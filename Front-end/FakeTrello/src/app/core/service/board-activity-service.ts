import { Injectable, NgZone, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { environment } from '../../../environment';
import { BoardActivity } from '../model/board-activity.model';

@Injectable({
  providedIn: 'root'
})
export class BoardActivityService {
  private hubConnection!: signalR.HubConnection;
  private connectionStarted = false;
  private joinedBoard: { ownerUsername: string; boardName: string } | null = null;

  public activities = signal<BoardActivity[]>([]);

  public constructor(private http: HttpClient, private ngZone: NgZone) {}

  public getActivitiesForBoard(ownerUsername: string, boardName: string): Observable<BoardActivity[]> {
    return this.http.get<BoardActivity[]>(`${environment.api}/activities/${ownerUsername}/${boardName}`);
  }

  public loadActivities(ownerUsername: string, boardName: string): void {
    this.getActivitiesForBoard(ownerUsername, boardName).subscribe({
      next: (activities) => {
        this.activities.set(activities);
      },
      error: (err) => {
        console.error('Failed to load board activities:', err);
      }
    });
  }

  public connectToBoard(ownerUsername: string, boardName: string): void {
    if (this.joinedBoard && this.joinedBoard.ownerUsername === ownerUsername && this.joinedBoard.boardName === boardName && this.connectionStarted) {
      return;
    }

    if (!this.connectionStarted) {
      this.hubConnection = new signalR.HubConnectionBuilder()
        .withUrl(`${environment.hub}/notificationHub`, {
          accessTokenFactory: () => localStorage.getItem('access-token') || ''
        })
        .withAutomaticReconnect()
        .build();

      this.hubConnection.on('ReceiveBoardActivity', (activity: BoardActivity) => {
        this.ngZone.run(() => {
          this.activities.update(current => [activity, ...current]);
        });
      });

      this.connectionStarted = true;

      this.hubConnection
        .start()
        .then(() => this.joinBoardGroup(ownerUsername, boardName))
        .catch(err => {
          this.connectionStarted = false;
          console.error('SignalR connection error:', err);
        });
    } else {
      const previousBoard = this.joinedBoard;
      const leavePrevious = previousBoard !== null
        ? this.hubConnection.invoke('LeaveBoardGroup', previousBoard.ownerUsername, previousBoard.boardName).catch(() => {})
        : Promise.resolve();

      leavePrevious.finally(() => this.joinBoardGroup(ownerUsername, boardName));
    }
  }

  private joinBoardGroup(ownerUsername: string, boardName: string): void {
    this.joinedBoard = { ownerUsername, boardName };
    this.hubConnection.invoke('JoinBoardGroup', ownerUsername, boardName).catch(err =>
      console.error('Failed to join board group:', err)
    );
  }

  public disconnectFromBoard(): void {
    if (!this.connectionStarted || !this.hubConnection) {
      return;
    }

    const board = this.joinedBoard;

    const leave = board !== null
      ? this.hubConnection.invoke('LeaveBoardGroup', board.ownerUsername, board.boardName).catch(() => {})
      : Promise.resolve();

    leave.finally(() => {
      this.hubConnection.stop().then(() => {
        this.connectionStarted = false;
        this.joinedBoard = null;
      });
    });
  }
}