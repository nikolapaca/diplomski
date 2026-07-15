import { ChangeDetectorRef, Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged, Subscription } from 'rxjs';
import { User } from '../../model/user.model';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { UserService } from '../../service/user-service';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { Board } from '../../model/board.model';
import { BoardService } from '../../service/board.service';

@Component({
  selector: 'app-add-collaborators-dialog',
  imports: [ReactiveFormsModule,
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatDialogModule,
    MatInputModule,
    MatListModule],
  templateUrl: './add-collaborators-dialog.html',
  styleUrl: './add-collaborators-dialog.css'
})
export class AddCollaboratorsDialog implements OnInit, OnDestroy{
  public searchControl = new FormControl('');
  public users: User[] = [];
  private searchSubscription: Subscription | undefined;
  
  public constructor(
    public dialogRef: MatDialogRef<AddCollaboratorsDialog>,
    private userService: UserService,
    private boardService: BoardService,
    @Inject(MAT_DIALOG_DATA) public data: Board 
  ) {}

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
  }

  public errorMessage: string | null = null;

  public addCollaborator(user: User): void {
    this.errorMessage = null;

    this.boardService.addCollaborator(this.data, user.username).subscribe({
      next: () => {
        this.fetchUsers(this.searchControl.value || '');
      },
      error: (err) => {
        this.errorMessage = typeof err.error === 'string' ? err.error : 'Failed to add collaborator.';
      }
    });
  }
  
  public ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  private fetchUsers(searchTerm: string): void {
    this.userService.getUsersNotAssignedToThisBoard(searchTerm, this.data.name, this.data.ownerUsername).subscribe({
      next: (users) => {
        this.users = users;
      }
    });
  }
}