import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, Inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { debounceTime, distinctUntilChanged, Subscription } from 'rxjs';
import { Board } from '../../model/board.model';
import { UserService } from '../../service/user-service';
import { User } from '../../model/user.model';
import { Card } from '../../model/card.model';
import { CardService } from '../../service/card.service';

@Component({
  selector: 'app-assign-card-dialog',
  imports: [ReactiveFormsModule,
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatDialogModule,
    MatInputModule,
    MatListModule],
  templateUrl: './assign-card-dialog.html',
  styleUrl: './assign-card-dialog.css'
})
export class AssignCardDialog {
  public searchControl = new FormControl('');
  public users = signal<User[]>([]);
  public assignedMessage = '';
  public user!: User;
  public board: Board | undefined;
  public card: Card | undefined;
  private searchSubscription: Subscription | undefined;

  public constructor(
    public dialogRef: MatDialogRef<AssignCardDialog>,
    private userService: UserService,
    private cardService: CardService,
    private cdRef: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: { card: Card, board: Board }
  ) {
    this.card = data.card;
    this.board = data.board;
  }

  public ngOnInit(): void {
    this.fetchUsers('');
    console.log('kartica', this.card)
    this.searchSubscription = this.searchControl.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged()
      )
      .subscribe(searchValue => {
        this.fetchUsers(searchValue || '');
      });

    this.setAssignedMessage();
  }

  public closeDialog() : void {
    this.dialogRef.close();
  }

  private setAssignedMessage(): void {
    if (this.card && (this.card.assignedUserUsername === null || this.card.assignedUserUsername === '')) {
      this.assignedMessage = 'Unassigned';
    } else {
      console.log('karticaaaa', this.card)
      if (this.card) {
        this.cardService.getUserAssignedToCard(this.card).subscribe({
          next: (response) => {
            this.user = response;
            console.log('koji je user', this.user);
            if (this.user) {
              this.assignedMessage = `Assigned to: ${this.user.username}`;
            } else {
              this.assignedMessage = 'Unassigned';
            }
            this.cdRef.detectChanges();
          },
          error: (error) => {
            console.error('greskaa', error);
            this.assignedMessage = 'Unassigned';
          }
        });
      }
    }
  }

  /*public addCollaborator(user: User): void {
    this.boardService.addCollaborator(this.data.board, user.username).subscribe((_) => {
      this.fetchUsers(this.searchControl.value || '')
    })
  }*/

  public assignToCollaborator(user: User): void {
    this.cardService.assignCard(this.data.card, user.username).subscribe({
      next: (_) => {
        if (this.card) {
          this.card.assignedUserUsername = user.username;
        }
        this.fetchUsers(this.searchControl.value || '');
        this.dialogRef.close(this.card);
      },
      error: (err) => {
        console.error('Došlo je do greške prilikom dodeljivanja kartice:', err);
      }
    });
  }

  public unassignFromCollaborator(): void {
    this.cardService.unassignCard(this.data.card).subscribe({
      next: (_) => {
        if (this.card) {
          this.card.assignedUserUsername = '';
        }
        this.fetchUsers(this.searchControl.value || '');
        this.dialogRef.close(this.card);
      },
      error: (err) => {
        console.error('Došlo je do greške prilikom dodeljivanja kartice:', err);
      }
    });
  }

  public ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  private fetchUsers(searchTerm: string): void {
    if (this.card) {
      this.userService.getAssignableUsersToThisBoard(searchTerm, this.data.board.name, this.data.board.ownerUsername, this.card.id).subscribe({
        next: (users) => {
          this.users.set(users);
        }
      });
    }
  }
}
