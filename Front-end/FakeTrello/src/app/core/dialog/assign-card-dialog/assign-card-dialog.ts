import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, EventEmitter, Inject, signal } from '@angular/core';
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
  imports: [
    ReactiveFormsModule,
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatDialogModule,
    MatInputModule,
    MatListModule
  ],
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
  public changed = new EventEmitter<Partial<Card>>();

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

  public closeDialog(): void {
    this.dialogRef.close(this.card);
  }

  private setAssignedMessage(): void {
    if (!this.card || !this.card.assignedUserUsernames || this.card.assignedUserUsernames.length === 0) {
      this.assignedMessage = 'Unassigned';
    } else {
      this.assignedMessage = `Assigned to: ${this.card.assignedUserUsernames.join(', ')}`;
    }

    this.cdRef.detectChanges();
  }

  public assignToCollaborator(user: User): void {
    this.cardService.assignCard(this.data.card, user.username).subscribe({
      next: (_) => {
        if (this.card) {
          this.card.assignedUserUsernames = [
            ...(this.card.assignedUserUsernames || []),
            user.username
          ];

          this.changed.emit({
            assignedUserUsernames: this.card.assignedUserUsernames
          });
        }

        this.setAssignedMessage();
        this.fetchUsers(this.searchControl.value || '');
        this.cdRef.detectChanges();
      },
      error: (err) => {
        console.error('Došlo je do greške prilikom dodeljivanja kartice:', err);
      }
    });
  }

  public unassignFromCollaborator(username: string): void {
    this.cardService.unassignCard(this.data.card, username).subscribe({
      next: (_) => {
        if (this.card) {
          this.card.assignedUserUsernames =
            (this.card.assignedUserUsernames || []).filter(u => u !== username);

          this.changed.emit({
            assignedUserUsernames: this.card.assignedUserUsernames
          });
        }

        this.setAssignedMessage();
        this.fetchUsers(this.searchControl.value || '');
        this.cdRef.detectChanges();
      },
      error: (err) => {
        console.error('Došlo je do greške prilikom skidanja korisnika sa kartice:', err);
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
      this.userService.getAssignableUsersToThisBoard(
        searchTerm,
        this.data.board.name,
        this.data.board.ownerUsername,
        this.card.id
      ).subscribe({
        next: (users) => {
          this.users.set(users);
        }
      });
    }
  }
}