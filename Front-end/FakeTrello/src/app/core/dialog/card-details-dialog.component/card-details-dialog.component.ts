import { ChangeDetectorRef, Component, EventEmitter, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef, MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDividerModule } from '@angular/material/divider';
import { ReactiveFormsModule, FormControl, Validators, FormGroup } from '@angular/forms';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { Card } from '../../../core/model/card.model';
import { Board } from '../../../core/model/board.model';
import { CardService } from '../../../core/service/card.service';
import { AssignCardDialog } from '../../../core/dialog/assign-card-dialog/assign-card-dialog';
import { UserService } from '../../../core/service/user-service';
import { BoardActivityService } from '../../../core/service/board-activity-service';
import { BoardActivity } from '../../../core/model/board-activity.model';
import { ACTIVITY_ICONS } from '../../../feature/board/board-activity/board-activity';
import { CommentService } from '../../../core/service/comment-service';
import { Comment } from '../../../core/model/comment.model';
import { CardImageService } from '../../service/card-image-service';
import { CardImage } from '../../../core/model/card-image.model';
import { environment } from '../../../../environment';
import { DueDateStatus, getDueDateStatus, toDatetimeLocalValue, fromDatetimeLocalValue } from '../../../core/util/due-date.util';

export interface CardDetailsData {
  card: Card;
  board: Board;
}

@Component({
  selector: 'app-card-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDividerModule,
    ReactiveFormsModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './card-details-dialog.component.html',
  styleUrls: ['./card-details-dialog.component.css']
})
export class CardDetailsDialogComponent {
  titleCtrl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required] });
  descriptionCtrl = new FormControl<string>('', { nonNullable: true });

  form = new FormGroup({
    name: this.titleCtrl,
    description: this.descriptionCtrl
  });

  saving = false;
  errorMessage = '';

  assignedUserUsernames: string[] = [];

  isPinned = false;
  loggedInUsername = '';

  showDueDateEditor = false;
  dueDateCtrl = new FormControl<string>('', { nonNullable: true });
  savingDueDate = false;

  showHistory = false;
  loadingHistory = false;
  history: BoardActivity[] = [];
  private historyLoaded = false;

  comments: Comment[] = [];
  loadingComments = false;
  postingComment = false;
  newCommentCtrl = new FormControl<string>('', { nonNullable: true });

  images: CardImage[] = [];
  loadingImages = false;
  uploadingImage = false;
  imageErrorMessage = '';
  readonly assetsBase = environment.api.replace(/\/api$/, '');

  readonly changed = new EventEmitter<Partial<Card>>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: CardDetailsData,
    private ref: MatDialogRef<CardDetailsDialogComponent, Card | null>,
    private cardService: CardService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private userService: UserService,
    private boardActivityService: BoardActivityService,
    private commentService: CommentService,
    private cardImageService: CardImageService
  ) {
    this.titleCtrl.setValue(data.card.name ?? '');
    this.descriptionCtrl.setValue(data.card.description ?? '');
    this.assignedUserUsernames = this.data.card.assignedUserUsernames ?? [];
    this.isPinned = this.data.card.isPinned ?? false;
    this.dueDateCtrl.setValue(toDatetimeLocalValue(this.data.card.dueDate));
    this.loggedInUsername = this.userService.getUsername();
    this.loadComments();
    this.loadImages();
  }

  get dueDateStatus(): DueDateStatus | null {
    return getDueDateStatus(this.data.card.dueDate);
  }

  toggleDueDateEditor(): void {
    this.dueDateCtrl.setValue(toDatetimeLocalValue(this.data.card.dueDate));
    this.showDueDateEditor = !this.showDueDateEditor;
  }

  saveDueDate(): void {
    const isoDueDate = fromDatetimeLocalValue(this.dueDateCtrl.value);
    this.applyDueDate(isoDueDate);
  }

  clearDueDate(): void {
    this.dueDateCtrl.setValue('');
    this.applyDueDate(null);
  }

  private applyDueDate(isoDueDate: string | null): void {
    const updated: Card = {
      id: this.data.card.id,
      name: this.titleCtrl.value,
      description: this.descriptionCtrl.value,
      cardListId: this.data.card.cardListId,
      isPinned: this.isPinned,
      dueDate: isoDueDate
    };

    this.savingDueDate = true;

    this.cardService.updateCard(updated).subscribe({
      next: () => {
        this.savingDueDate = false;
        this.data.card.dueDate = isoDueDate;
        this.showDueDateEditor = false;
        this.cdr.detectChanges();
        this.changed.emit({ dueDate: isoDueDate });
      },
      error: () => {
        this.savingDueDate = false;
        this.errorMessage = 'Failed to update due date.';
      }
    });
  }

  get isOwner(): boolean {
    return !!this.data.board && this.loggedInUsername === this.data.board.ownerUsername;
  }

  togglePin(): void {
    this.cardService.togglePin(this.data.card.id).subscribe({
      next: () => {
        this.isPinned = !this.isPinned;
        this.data.card.isPinned = this.isPinned;
        this.cdr.detectChanges();
        this.changed.emit({ isPinned: this.isPinned });
      },
      error: () => {
        this.errorMessage = 'Failed to toggle pin.';
      }
    });
  }

  toggleHistory(): void {
    this.showHistory = !this.showHistory;

    if (this.showHistory && !this.historyLoaded) {
      this.loadingHistory = true;
      this.boardActivityService.getActivitiesForCard(this.data.card.id).subscribe({
        next: (activities) => {
          this.history = activities;
          this.historyLoaded = true;
          this.loadingHistory = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.loadingHistory = false;
          this.errorMessage = 'Failed to load card history.';
        }
      });
    }
  }

  iconFor(type: number): string {
    return ACTIVITY_ICONS[type] || 'history';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const updated: Card = {
      id: this.data.card.id,
      name: this.titleCtrl.value,
      description: this.descriptionCtrl.value,
      cardListId: this.data.card.cardListId,
      isPinned: this.isPinned,
      dueDate: this.data.card.dueDate
    };

    this.saving = true;

    this.cardService.updateCard(updated).subscribe({
      next: () => {
        this.saving = false;

        const merged: Card = {
          ...updated,
          assignedUserUsernames: this.assignedUserUsernames
        };

        this.ref.close(merged);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = 'Failed to save changes.';
        console.error(err);
      }
    });
  }

  delete(): void {
    if (!confirm('Delete this card?')) return;

    this.saving = true;

    this.cardService.deleteCard(this.data.card.id).subscribe({
      next: () => {
        this.saving = false;
        this.ref.close(null);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = 'Failed to delete card.';
        console.error(err);
      }
    });
  }

  assign(): void {
    const dialogRef = this.dialog.open(AssignCardDialog, {
      width: '720px',
      data: { card: this.data.card, board: this.data.board }
    });

    dialogRef.componentInstance.changed.subscribe((patch: Partial<Card>) => {
      if (patch.assignedUserUsernames) {
        this.assignedUserUsernames = patch.assignedUserUsernames;
        this.data.card.assignedUserUsernames = patch.assignedUserUsernames;

        this.cdr.detectChanges();

        this.changed.emit({
          assignedUserUsernames: patch.assignedUserUsernames
        });
      }
    });

    dialogRef.afterClosed().subscribe((updatedCard: Card) => {
      if (updatedCard) {
        this.assignedUserUsernames = updatedCard.assignedUserUsernames ?? [];
        this.data.card.assignedUserUsernames = this.assignedUserUsernames;

        this.cdr.detectChanges();

        this.changed.emit({
          assignedUserUsernames: this.assignedUserUsernames
        });
      }
    });
  }

  unassign(username: string): void {
    this.cardService.unassignCard(this.data.card, username).subscribe({
      next: () => {
        this.assignedUserUsernames = this.assignedUserUsernames
          .filter(u => u !== username);

        this.data.card.assignedUserUsernames = this.assignedUserUsernames;

        this.cdr.detectChanges();

        this.changed.emit({
          assignedUserUsernames: this.assignedUserUsernames
        });
      },
      error: () => {
        this.errorMessage = 'Failed to unassign user.';
      }
    });
  }

  loadComments(): void {
    this.loadingComments = true;
    this.commentService.getByCard(this.data.card.id).subscribe({
      next: (comments) => {
        this.comments = comments;
        this.loadingComments = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingComments = false;
      }
    });
  }

  addComment(): void {
    const text = this.newCommentCtrl.value.trim();
    if (!text) {
      return;
    }

    this.postingComment = true;

    this.commentService.addComment(this.data.card.id, text).subscribe({
      next: (comment) => {
        this.comments = [...this.comments, comment];
        this.newCommentCtrl.setValue('');
        this.postingComment = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.postingComment = false;
        this.errorMessage = 'Failed to add comment.';
      }
    });
  }

  deleteComment(commentId: number): void {
    this.commentService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.id !== commentId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to delete comment.';
      }
    });
  }

  canDeleteComment(comment: Comment): boolean {
    return comment.username === this.loggedInUsername || this.isOwner;
  }

  loadImages(): void {
    this.loadingImages = true;
    this.cardImageService.getByCard(this.data.card.id).subscribe({
      next: (images) => {
        this.images = images;
        this.loadingImages = false;
        this.emitCoverChange();
        this.cdr.detectChanges();
      },
      error: () => {
        this.loadingImages = false;
      }
    });
  }

  onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files && input.files.length > 0 ? input.files[0] : null;
    if (!file) {
      return;
    }

    this.uploadingImage = true;
    this.imageErrorMessage = '';

    this.cardImageService.upload(this.data.card.id, file).subscribe({
      next: (image) => {
        this.images = [...this.images, image];
        this.uploadingImage = false;
        input.value = '';
        this.emitCoverChange();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.uploadingImage = false;
        this.imageErrorMessage = err.error?.message || err.error || 'Failed to upload image.';
        input.value = '';
      }
    });
  }

  deleteImage(imageId: number): void {
    this.cardImageService.delete(imageId).subscribe({
      next: () => {
        this.images = this.images.filter(i => i.id !== imageId);
        this.emitCoverChange();
        this.cdr.detectChanges();
      },
      error: () => {
        this.imageErrorMessage = 'Failed to delete image.';
      }
    });
  }

  private emitCoverChange(): void {
    const coverImageUrl = this.images.length === 1 ? this.images[0].filePath : null;
    this.changed.emit({ coverImageUrl, attachmentCount: this.images.length });
  }

  canDeleteImage(image: CardImage): boolean {
    return image.uploadedByUsername === this.loggedInUsername || this.isOwner;
  }

  imageUrl(image: CardImage): string {
    return `${this.assetsBase}${image.filePath}`;
  }

  close(): void {
    this.ref.close(null);
  }
}