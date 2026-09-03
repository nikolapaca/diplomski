import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, Inject, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Board } from '../../model/board.model';
import { BoardMember } from '../../model/board-member.model';
import { BoardService } from '../../service/board.service';

@Component({
  selector: 'app-board-members-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './board-members-dialog.html',
  styleUrl: './board-members-dialog.css'
})
export class BoardMembersDialog implements OnInit {
  public members: BoardMember[] = [];
  public loading = true;
  public errorMessage = '';

  public constructor(
    public dialogRef: MatDialogRef<BoardMembersDialog>,
    private boardService: BoardService,
    private cdRef: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: Board
  ) {}

  public ngOnInit(): void {
    this.boardService.getMembers(this.data.name, this.data.ownerUsername).subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
        this.cdRef.detectChanges();
      },
      error: () => {
        this.errorMessage = 'Failed to load board members.';
        this.loading = false;
        this.cdRef.detectChanges();
      }
    });
  }

  public isOwner(member: BoardMember): boolean {
    return member.role === 0;
  }

  public close(): void {
    this.dialogRef.close();
  }
}
