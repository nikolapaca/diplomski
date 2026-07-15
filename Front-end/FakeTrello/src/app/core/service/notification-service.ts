import { Injectable, NgZone, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { Observable } from 'rxjs';
import { environment } from '../../../environment';
import { Notification } from '../model/notification.model';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private hubConnection!: signalR.HubConnection;

  public notifications = signal<Notification[]>([]);
  public unreadCount = signal<number>(0);
  private connectionStarted = false;

  public constructor(private http: HttpClient, private ngZone: NgZone) {}

  public getNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${environment.api}/notifications`);
  }

  public markAsRead(notificationId: number): Observable<void> {
    return this.http.put<void>(`${environment.api}/notifications/${notificationId}/read`, {});
  }

  public markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${environment.api}/notifications/read-all`, {});
  }

  public getUnreadNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${environment.api}/notifications/unread`);
  }

  public loadNotifications(): void {
    this.getNotifications().subscribe({
      next: (notifications) => {
        this.notifications.set(notifications);
        this.unreadCount.set(notifications.filter(n => !n.isRead).length);
      },
      error: (err) => {
        console.error('Failed to load notifications:', err);
      }
    });
  }

  public loadUnreadNotifications(): void {
    this.getUnreadNotifications().subscribe({
      next: (notifications) => {
        this.unreadCount.set(notifications.length);
      },
      error: (err) => {
        console.error('Failed to load unread notifications:', err);
      }
    });
  }

  public startConnection(): void {
    if (this.connectionStarted) {
      return;
    }

    this.connectionStarted = true;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hub}/notificationHub`, {
        accessTokenFactory: () => localStorage.getItem('access-token') || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      this.ngZone.run(() => {
        this.notifications.update(current => [notification, ...current]);
        this.unreadCount.update(count => count + 1);
      });
    });

    this.hubConnection
      .start()
      .then(() => console.log('SignalR connected.'))
      .catch(err => {
        this.connectionStarted = false;
        console.error('SignalR connection error:', err);
      });
  }

  public stopConnection(): void {
    if (!this.connectionStarted || !this.hubConnection) {
      return;
    }

    this.hubConnection.stop().then(() => {
      this.connectionStarted = false;
    });
  }
}