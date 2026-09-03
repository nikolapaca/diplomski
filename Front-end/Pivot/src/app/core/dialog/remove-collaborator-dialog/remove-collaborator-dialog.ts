import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { User } from '../../model/user.model';
import { debounceTime, distinctUntilChanged, Subscription } from 'rxjs';
import { UserService } from '../../service/user-service';
import { BoardService } from '../../service/board.service';
import { Board } from '../../model/board.model';

@Component({
  selector: 'app-remove-collaborator-dialog',
  imports: [ReactiveFormsModule,
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatFormFieldModule,
    MatDialogModule,
    MatInputModule,
    MatListModule],
  templateUrl: './remove-collaborator-dialog.html',
  styleUrl: './remove-collaborator-dialog.css'
})
export class RemoveCollaboratorDialog implements OnInit, OnDestroy{
  public searchControl = new FormControl('');
  public users: User[] = [];
  private searchSubscription: Subscription | undefined;
  
  public constructor(
    public dialogRef: MatDialogRef<RemoveCollaboratorDialog>,
    private userService: UserService,
    private boardService: BoardService,
    private cdRef: ChangeDetectorRef,
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

  public ngOnDestroy(): void {
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
  }

  public removeCollaborator(user: User): void {
    this.boardService.removeCollaborator(this.data, user.username).subscribe((_) => {
      this.fetchUsers(this.searchControl.value || '')
    })
  }

  private fetchUsers(searchTerm: string): void {
    this.userService.getUsersAssignedToThisBoard(searchTerm, this.data.name, this.data.ownerUsername).subscribe({
      next: (users) => {
        this.users = users;
        this.cdRef.detectChanges();
      }
    });
  }
}
