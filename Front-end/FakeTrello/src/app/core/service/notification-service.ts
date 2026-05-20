import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environment';
@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  private hubConnection!: signalR.HubConnection;

  public startConnection(): void {

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.hub}/notificationHub`, {
        accessTokenFactory: () => localStorage.getItem('access-token') || ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('SignalR connected.');
      })
      .catch(err => {
        console.error('SignalR connection error:', err);
      });
  }

  public addNotificationListener(callback: (notification: any) => void): void {

    this.hubConnection.on('ReceiveNotification', (notification) => {
      callback(notification);
    });

  }
}