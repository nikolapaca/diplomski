import { Component, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { AuthService } from '../service/auth-service';
import { MatDialog } from '@angular/material/dialog';
import { CreateBoardDialog } from '../dialog/create-board-dialog/create-board-dialog';
import { FormControl, ReactiveFormsModule, ɵInternalFormsSharedModule } from "@angular/forms";
import { debounceTime, distinctUntilChanged, filter } from 'rxjs';
import { BoardService } from '../service/board.service';
import { Board } from '../model/board.model';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { NotificationService } from '../service/notification-service';

@Component({
  selector: 'app-navbar',
  imports: [ɵInternalFormsSharedModule, ReactiveFormsModule, 
    MatToolbarModule, MatButtonModule, MatIconModule, RouterModule],
  templateUrl: './navbar.html',
  styleUrls: ['./navbar.css']
})
export class Navbar implements OnInit {
  public isLoggedIn: boolean = false;
  public searchControl = new FormControl('');
  public errorMessage = '';
  public hideSearch = false;


  public constructor(
    private router: Router,
    private authService: AuthService,
    private matDialog: MatDialog,
    private boardService: BoardService,
    private notificationService: NotificationService
  ) {}

  public ngOnInit(): void {
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        const urlSegments = event.url.split('/');
        this.hideSearch = urlSegments[1] === 'boards' && urlSegments.length === 4;
      });
    this.authService.isLoggedIn$.subscribe(status => {
      this.isLoggedIn = status;
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
    
    this.notificationService.startConnection();

    this.notificationService.addNotificationListener((notification) => {
      console.log('NOTIFICATION RECEIVED:', notification);
    });

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
    localStorage.removeItem("access-token");
    this.isLoggedIn = false;
    this.router.navigate(['']);
  }

  public returnToHome() : void {
    this.router.navigate(['home'])
  }
}