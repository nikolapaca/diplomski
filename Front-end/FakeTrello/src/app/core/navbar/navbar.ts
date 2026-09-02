import { Component, ElementRef, HostListener, OnInit, ChangeDetectorRef } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { AuthService } from '../service/auth-service';
import { MatDialog } from '@angular/material/dialog';
import { CreateBoardDialog } from '../dialog/create-board-dialog/create-board-dialog';
import { FormControl, ReactiveFormsModule, ɵInternalFormsSharedModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs';
import { BoardService } from '../service/board.service';
import { Board } from '../model/board.model';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../service/notification-service';
import { Notification } from '../model/notification.model';
import { CommonModule } from '@angular/common';
import { MatMenuModule } from '@angular/material/menu';
import { UserService } from '../service/user-service';

@Component({
  selector: 'app-navbar',
  imports: [
    CommonModule,
    ɵInternalFormsSharedModule,
    ReactiveFormsModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    RouterModule
  ],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class Navbar implements OnInit {
  public isLoggedIn: boolean = false;
  public searchControl = new FormControl('');
  public errorMessage = '';
  public hideSearch = false;
  public isInsideBoard = false;
  public showNotifications = false;
  public loggedInUsername = '';

  public constructor(
    private router: Router,
    private authService: AuthService,
    private matDialog: MatDialog,
    private boardService: BoardService,
    public notificationService: NotificationService,
    private elementRef: ElementRef,
    private userService: UserService,
    private cdr: ChangeDetectorRef
  ) {}

  public ngOnInit(): void {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        const urlSegments = event.url.split('/');
        this.isInsideBoard = urlSegments[1] === 'boards' && urlSegments.length === 4;
        this.hideSearch = this.isInsideBoard;
      });

    this.authService.isLoggedIn$.subscribe(status => {
      this.isLoggedIn = status;

      if (this.isLoggedIn) {
        this.loggedInUsername = this.userService.getUsername();
        this.notificationService.loadNotifications();
        this.notificationService.startConnection();
      }
      this.cdr.markForCheck();
    });

    this.searchControl.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged()
      )
      .subscribe(searchTerm => {
        const lowercaseSearchTerms = searchTerm?.toLowerCase().trim();
        this.boardService.search(lowercaseSearchTerms || '');
      });
  }

  public toggleNotifications(): void {
    const isOpening = !this.showNotifications;
    this.showNotifications = !this.showNotifications;

    if (!isOpening) {
      this.clearBadge();
    }
  }

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    if (!this.showNotifications) {
      return;
    }

    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.showNotifications = false;
      this.clearBadge();
    }
  }

  private clearBadge(): void {
    this.notificationService.unreadCount.set(0);
  }

  public markAllNotificationsAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notificationService.notifications.update(current =>
          current.map(n => ({ ...n, isRead: true }))
        );
        this.notificationService.unreadCount.set(0);
      },
      error: (err) => {
        console.error('Failed to mark all notifications as read:', err);
      }
    });
  }

  public iconFor(type: string | number): string {
    switch (type) {
      case 'ADDED_TO_BOARD':
      case 0:
        return 'dashboard_customize';
      case 'REMOVED_FROM_BOARD':
      case 1:
        return 'person_remove';
      case 'ASSIGNED_TO_CARD':
      case 2:
        return 'person_add';
      case 'UNASSIGNED_FROM_CARD':
      case 3:
        return 'person_off';
      case 'BOARD_DELETED':
      case 4:
        return 'delete_forever';
      case 'BOARD_ARCHIVED':
      case 5:
        return 'archive';
      default:
        return 'notifications';
    }
  }

  public openNotification(notification: Notification): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe({
        next: () => {
          this.notificationService.notifications.update(current =>
            current.map(n =>
              n.id === notification.id ? { ...n, isRead: true } : n
            )
          );

          this.notificationService.unreadCount.update(count =>
            count > 0 ? count - 1 : 0
          );
        }
      });
    }

    if (!notification.boardOwnerUsername || !notification.boardName) {
      return;
    }

    const type = notification.type;

    if (notification.type === 'ADDED_TO_BOARD' || notification.type === 0) {
      this.showNotifications = false;
      this.clearBadge();

      this.router.navigate([
        '/boards',
        notification.boardOwnerUsername,
        notification.boardName
      ]);

      return;
    }

    if (type === 'ASSIGNED_TO_CARD' || type === 2) {
      this.showNotifications = false;
      this.clearBadge();

      this.router.navigate(
        [`/boards/${notification.boardOwnerUsername}/${notification.boardName}`],
        {
          queryParams: {
            cardId: notification.cardId
          }
        }
      );
      return;
    }
  }

  public createBoard(): void {
    const dialogRef = this.matDialog.open(CreateBoardDialog, {
      width: '1800px'
    });

    dialogRef.componentInstance.boardSubmitted.subscribe((board: Board) => {
      this.boardService.createBoard(board).subscribe({
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

  public signOut(): void {
    localStorage.removeItem('access-token');
    this.isLoggedIn = false;
    this.showNotifications = false;
    this.notificationService.notifications.set([]);
    this.notificationService.unreadCount.set(0);
    this.router.navigate(['']);
  }

  public returnToHome(): void {
    this.router.navigate(['home']);
  }
}