import { Component, OnDestroy, OnInit } from '@angular/core';
import { BoardDetails } from "../feature/board/board-details/board-details";
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { Board } from '../core/model/board.model';
import { BoardService } from '../core/service/board.service';
import { CommonModule } from '@angular/common';
import { Observable, Subscription } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { CreateBoardDialog } from '../core/dialog/create-board-dialog/create-board-dialog';

@Component({
  selector: 'app-homepage',
  imports: [BoardDetails, MatDividerModule, MatIconModule, CommonModule],
  templateUrl: './homepage.html',
  styleUrls: ['./homepage.css']
})
export class Homepage implements OnDestroy, OnInit {
  public boards$!: Observable<Board[]>;
  private boardDeletedSubscription: Subscription | undefined;
  public errorMessage: string = '';

  public constructor (
    private boardService: BoardService,
    public dialog: MatDialog
  ) {
    this.boards$ = this.boardService.boards$;
  }

  public ngOnInit(): void {
    this.boardService.refreshState();
  }

  public ngOnDestroy(): void {
    if(this.boardDeletedSubscription){
      this.boardDeletedSubscription.unsubscribe();
    }
  }
}