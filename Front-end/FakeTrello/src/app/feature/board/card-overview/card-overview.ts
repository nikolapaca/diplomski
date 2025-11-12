import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
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

@Component({
  selector: 'app-card-overview',
  imports: [MatCardModule, CommonModule, MatIcon, MatButtonModule, MatMenuModule, MatFormFieldModule, ReactiveFormsModule,MatInputModule],
  templateUrl: './card-overview.html',
  styleUrl: './card-overview.css'
})
export class CardOverview implements OnInit {
  @Input() public card!: Card;
  @Input() public board!: Board;
  @Output() public cardDeleted = new EventEmitter<number>();
  @Output() public cardUpdated = new EventEmitter<void>();
  public showUpdateCardForm = false;
  public updateCardForm!: FormGroup;
  public errorMessage = '';
  public loggedInUsername = '';

  public constructor(private cardService: CardService, private userService: UserService, private matDialog: MatDialog) {
  }

  public ngOnInit(): void 
  {
    this.loggedInUsername = this.userService.getUsername();
  }

  public deleteCard() : void {
    this.cardService.deleteCard(this.card.id).subscribe({
      next: () => {
        this.cardDeleted.emit(this.card.cardListId);
      }})
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
      id: this.card.id,
      name: this.updateCardForm.value.name,
      description: this.updateCardForm.value.description,
      cardListId: this.card.cardListId
    }

    this.cardService.updateCard(updatedCard).subscribe({
          next: (response) => {
            this.card.name = updatedCard.name;
            this.card.description = updatedCard.description;
            this.closeUpdateCardForm();

            this.cardUpdated.emit();
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
        }
      });
  }

  public leaveCard() : void {
    this.cardService.unassignCard(this.card).subscribe({
      next: (_) => {
      },
      error: (err) => {
        console.error('Došlo je do greške prilikom dodeljivanja kartice:', err);
      }
    });
  }
}
