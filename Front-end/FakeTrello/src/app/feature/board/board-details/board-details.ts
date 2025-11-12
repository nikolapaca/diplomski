import { Component, Input, OnInit } from '@angular/core';
import {MatCardModule} from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Board } from '../../../core/model/board.model';
import { Router } from '@angular/router';
import { MatIcon } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { BoardService } from '../../../core/service/board.service';
import { MatButtonModule, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { UpdateBoardDialog } from '../../../core/dialog/update-board-dialog/update-board-dialog';
import { BoardUpdate } from '../../../core/model/board-update.model';
import { AuthService } from '../../../core/service/auth-service';

@Component({
  selector: 'app-board-card',
  imports: [MatCardModule, MatTooltipModule, MatIcon, MatIconButton, MatMenuModule, MatButtonModule],
  templateUrl: './board-details.html',
  styleUrl: './board-details.css'
})
export class BoardDetails implements OnInit {
  @Input() board!: Board;
  public loggedInUsername = '';

  public constructor(private router: Router, private boardService: BoardService, private matDialog: MatDialog, private authService: AuthService) { }

  public navigateToBoard(): void {
    this.router.navigate(['/boards', this.board.ownerUsername, this.board.name], {
      state: { board: this.board }
    });
  }

  public ngOnInit(): void {
    this.loggedInUsername = this.authService.getLoggedInUser() || '';
  }

  public deleteBoard() : void {
    this.boardService.deleteBoard(this.board.name, this.board.ownerUsername).subscribe();
  }

  public leaveBoard() : void {
    this.boardService.removeCollaborator(this.board, this.loggedInUsername).subscribe({
      next: (_) => {
        this.router.navigate(['home'])
        this.boardService.refreshState();
      }
    })
  }

  public updateBoard() : void {
    const boardUpdate: BoardUpdate = {
      oldBoardName: this.board.name,
      newBoardName: '',
      oldBoardDescription: this.board.description,
      newBoardDescription: '',
      ownerUsername: this.board.ownerUsername
    }
    const dialogRef = this.matDialog.open(UpdateBoardDialog, {
          width: '1800px',
          data: boardUpdate
        });

    dialogRef.componentInstance.boardSubmitted.subscribe((board: BoardUpdate) => {
      this.boardService.updateBoard(board).subscribe({
        next: () => {
          this.boardService.refreshState();
          dialogRef.close();
        },
        error: (error) => {
          let errorMessage = 'Something went wrong. Please try again.';
          if (error.error && typeof error.error === 'string') {
            errorMessage = error.error;
          } else if (error.error && error.error.errors) {
            const firstError = Object.values(error.error.errors)[0] as string[];
            if (firstError && firstError.length > 0) {
              errorMessage = firstError[0];
            }
          }
          dialogRef.componentInstance.setBackendError(errorMessage);
        }
      });
    });
  }
}
