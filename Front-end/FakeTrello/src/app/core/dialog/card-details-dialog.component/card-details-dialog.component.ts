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
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

import { Card } from '../../../core/model/card.model';
import { Board } from '../../../core/model/board.model';
import { CardService } from '../../../core/service/card.service';
import { AssignCardDialog } from '../../../core/dialog/assign-card-dialog/assign-card-dialog';
import { AiService } from '../../../core/service/ai.service';
import { UserService } from '../../../core/service/user-service';
import { BoardActivityService } from '../../../core/service/board-activity-service';
import { BoardActivity } from '../../../core/model/board-activity.model';
import { ACTIVITY_ICONS } from '../../../feature/board/board-activity/board-activity';

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
    MatProgressSpinnerModule,
    MatSnackBarModule
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

  aiGenerating = false;
  aiGeneratedTasks: string = '';

  assignedUserUsernames: string[] = [];

  isPinned = false;
  loggedInUsername = '';

  showHistory = false;
  loadingHistory = false;
  history: BoardActivity[] = [];
  private historyLoaded = false;

  readonly changed = new EventEmitter<Partial<Card>>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: CardDetailsData,
    private ref: MatDialogRef<CardDetailsDialogComponent, Card | null>,
    private cardService: CardService,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private aiService: AiService,
    private snackBar: MatSnackBar,
    private userService: UserService,
    private boardActivityService: BoardActivityService
  ) {
    this.titleCtrl.setValue(data.card.name ?? '');
    this.descriptionCtrl.setValue(data.card.description ?? '');
    this.assignedUserUsernames = this.data.card.assignedUserUsernames ?? [];
    this.isPinned = this.data.card.isPinned ?? false;
    this.loggedInUsername = this.userService.getUsername();
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

  suggestTasks(): void {
    const description = this.descriptionCtrl.value.trim();

    if (description.length < 20) {
      this.snackBar.open(
        'Opis je prekratak za smisleno generiranje zadataka (min. 20 znakova).',
        'Zatvori',
        { duration: 3000 }
      );
      return;
    }

    this.aiGenerating = true;
    this.errorMessage = '';
    this.aiGeneratedTasks = '';

    this.aiService.extractTasks(description).subscribe({
      next: (tasks) => {
        this.aiGeneratedTasks = tasks;
        this.descriptionCtrl.patchValue(this.aiGeneratedTasks);
        this.aiGenerating = false;
        this.snackBar.open('Zadaci uspješno generirani!', 'OK', { duration: 3000 });
      },
      error: (err) => {
        this.aiGenerating = false;
        this.errorMessage = err.error?.message || 'Greška u komunikaciji s AI servisom. Provjerite Ollamu/Backend.';
        this.snackBar.open(this.errorMessage, 'Zatvori', { duration: 5000 });
        console.error(err);
      }
    });
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
      isPinned: this.isPinned
    };

    this.saving = true;

    this.cardService.updateCard(updated).subscribe({
      next: (res) => {
        this.saving = false;

        const merged = {
          ...(res ?? updated),
          assignedUserUsernames: this.assignedUserUsernames
        } as Card;

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

  close(): void {
    this.ref.close(null);
  }
}