import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { Card } from '../../../core/model/card.model';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import {MatMenuModule} from '@angular/material/menu';
import { CardService } from '../../../core/service/card.service';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Board } from '../../../core/model/board.model';
import { MatDialog } from '@angular/material/dialog';
import { AssignCardDialog } from '../../../core/dialog/assign-card-dialog/assign-card-dialog';
import { UserService } from '../../../core/service/user-service';
import { CardDetailsDialogComponent } from '../../../core/dialog/card-details-dialog.component/card-details-dialog.component';
import { DueDateStatus, getDueDateStatus } from '../../../core/util/due-date.util';
import { ConfirmDialog } from '../../../core/dialog/confirm-dialog/confirm-dialog';
import { environment } from '../../../../environment';

@Component({
  selector: 'app-card-overview',
  imports: [MatCardModule, CommonModule, MatIcon, MatButtonModule, MatMenuModule, MatFormFieldModule, ReactiveFormsModule,MatInputModule],
  templateUrl: './card-overview.html',
  styleUrl: './card-overview.css'
})
export class CardOverview implements OnInit {
  @Input() public card!: Card;
  @Input() public board!: Board;
  @Input() public highlightedCardId: number | null = null;

  @Output() public cardDeleted = new EventEmitter<number>();
  @Output() public cardUpdated = new EventEmitter<void>();
  @Output() public highlightCleared = new EventEmitter<void>();

  public showUpdateCardForm = false;
  public updateCardForm!: FormGroup;
  public errorMessage = '';
  public loggedInUsername = '';
  public readonly assetsBase = environment.api.replace(/\/api$/, '');

  public get coverImageUrl(): string | null {
    if (!this.card.coverImageUrl) {
      return null;
    }
    return `${this.assetsBase}${this.card.coverImageUrl}`;
  }

  public constructor(private cardService: CardService, private userService: UserService, private matDialog: MatDialog, private cdr: ChangeDetectorRef) {
  }

  public ngOnInit(): void 
  {
    this.loggedInUsername = this.userService.getUsername();
  }

  public get dueDateStatus(): DueDateStatus | null {
    return getDueDateStatus(this.card.dueDate);
  }

  public onCardClick(event: MouseEvent): void {
    if (this.highlightedCardId === this.card.id) {
    this.highlightCleared.emit();
    }

    this.openCardDetails(event);
  }

  public deleteCard() : void {
    this.cardService.deleteCard(this.card.id).subscribe({
      next: () => {
        this.cardDeleted.emit(this.card.cardListId);
      }})
  }

  public confirmDeleteCard(): void {
    const dialogRef = this.matDialog.open(ConfirmDialog, {
      width: '420px',
      data: {
        title: 'Delete card?',
        message: `Are you sure you want to delete "${this.card.name}"? This action cannot be undone.`,
        confirmText: 'Delete card'
      }
    });
    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.deleteCard();
      }
    });
  }

  public openAssingCardDialog() : void {
    const dialogRef = this.matDialog.open(AssignCardDialog, {
      width: '1800px',
      data: {
        card: this.card,
        board: this.board
      }
    });
    dialogRef.afterClosed().subscribe((updatedCard: Card) => {
    if (updatedCard) {
      this.card = updatedCard;
      this.cardUpdated.emit();
    }
  });
  }

  public toggleUpdateCardForm() : void {
    this.showUpdateCardForm = true;
    this.updateCardForm = new FormGroup({
      name: new FormControl('', Validators.required),
      description: new FormControl('', Validators.required)
    })
    this.updateCardForm.patchValue({
      name: this.card.name,
      description: this.card.description
    })
  }

  public closeUpdateCardForm() : void {
    this.showUpdateCardForm = false;
    this.updateCardForm.reset();
  }

  public updateCard() : void {
    const updatedCard: Card = {
      ...this.card,
      name: this.updateCardForm.value.name,
      description: this.updateCardForm.value.description
    }

    this.cardService.updateCard(updatedCard).subscribe({
          next: (response) => {
            this.card.name = updatedCard.name;
            this.card.description = updatedCard.description;
            this.closeUpdateCardForm();

            this.cardUpdated.emit();
            this.cdr.markForCheck();
          },
          error: (error) => {
    
          this.errorMessage = 'Something went wrong. Please try again.';
    
          if (error.error && typeof error.error === 'string') {
            this.errorMessage = error.error;
          } else if (error.error && error.error.errors) {
            const firstError = Object.values(error.error.errors)[0] as string[];
            if (firstError && firstError.length > 0) {
              this.errorMessage = firstError[0];
            }
          }
          this.cdr.markForCheck();
        }
      });
  }

  public leaveCard(): void {
  this.cardService.unassignCard(this.card, this.loggedInUsername).subscribe({
    next: (_) => {
      this.card.assignedUserUsernames =
        (this.card.assignedUserUsernames || []).filter(u => u !== this.loggedInUsername);

      this.cardUpdated.emit();
      this.cdr.markForCheck();
    },
    error: (err) => {
      console.error('Došlo je do greške prilikom napuštanja kartice:', err);
    }
  });
}

  public openCardDetails(event?: MouseEvent): void {
  if (event) event.stopPropagation();

  const dialogRef = this.matDialog.open(CardDetailsDialogComponent, {
    data: { card: this.card, board: this.board },
    width: '960px',
    maxWidth: '95vw',
    height: '640px',
    maxHeight: '90vh',
    panelClass: 'card-details-panel'
  });

  const inst = dialogRef.componentInstance;
    inst.changed.subscribe((patch: Partial<Card>) => {
      this.card = { ...this.card, ...patch }; 
      this.cardUpdated.emit();
      this.cdr.markForCheck();                
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result === null) {
        this.cardDeleted.emit(this.card.cardListId);
        return;
      }
      if (result) {
        this.card = result;
        this.cardUpdated.emit();
        this.cdr.markForCheck();
      }
    });
}
}