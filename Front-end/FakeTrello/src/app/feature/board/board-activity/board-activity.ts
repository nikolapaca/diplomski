import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Board } from '../../../core/model/board.model';
import { BoardActivity } from '../../../core/model/board-activity.model';
import { BoardActivityService } from '../../../core/service/board-activity-service';

export const ACTIVITY_ICONS: Record<number, string> = {
  0: 'add_circle',        // CARD_CREATED
  1: 'edit',               // CARD_UPDATED
  2: 'delete',             // CARD_DELETED
  3: 'swap_horiz',         // CARD_MOVED
  4: 'swap_vert',          // CARD_REORDERED
  5: 'person_add',         // USER_ASSIGNED_TO_CARD
  6: 'person_remove',      // USER_UNASSIGNED_FROM_CARD
  7: 'group_add',          // COLLABORATOR_ADDED
  8: 'group_remove',       // COLLABORATOR_REMOVED
  9: 'playlist_add',       // LIST_CREATED
  10: 'edit_note',         // LIST_UPDATED
  11: 'playlist_remove',   // LIST_DELETED
  12: 'dashboard_customize', // BOARD_UPDATED
  13: 'archive',           // BOARD_ARCHIVED
  14: 'push_pin',          // LIST_PINNED
  15: 'push_pin',          // LIST_UNPINNED
  16: 'push_pin',          // CARD_PINNED
  17: 'push_pin'           // CARD_UNPINNED
};

@Component({
  selector: 'app-board-activity',
  imports: [CommonModule, MatIconModule],
  templateUrl: './board-activity.html',
  styleUrl: './board-activity.css'
})
export class BoardActivityPanel implements OnChanges, OnDestroy {
  @Input() board: Board | undefined;

  public showActivity = false;

  public constructor(public boardActivityService: BoardActivityService) {}

  public ngOnChanges(changes: SimpleChanges): void {
    if (changes['board'] && this.board) {
      this.boardActivityService.loadActivities(this.board.ownerUsername, this.board.name);
      this.boardActivityService.connectToBoard(this.board.ownerUsername, this.board.name);
    }
  }

  public ngOnDestroy(): void {
    this.boardActivityService.disconnectFromBoard();
  }

  public toggleActivity(): void {
    this.showActivity = !this.showActivity;
  }

  public iconFor(type: number): string {
    return ACTIVITY_ICONS[type] || 'history';
  }
}