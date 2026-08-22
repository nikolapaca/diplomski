import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../service/auth-service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './confirm-email.html',
  styleUrl: './confirm-email.css',
})
export class ConfirmEmail implements OnInit, OnDestroy {

  token: string | null = null;

  message = '';
  isConfirmed = false;
  isLoading = false;


  public redirectSeconds = 3;
  private redirectTimer: any;

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) {}


  ngOnInit(): void {

    this.token = this.route.snapshot.queryParamMap.get('token');

    console.log("TOKEN FROM URL:", this.token);

    if (!this.token) {
      this.message = 'Invalid confirmation link.';
    }
  }


  confirmEmail(): void {

    console.log("CONFIRM BUTTON CLICKED");

    if (!this.token) {
      console.log("TOKEN DOES NOT EXIST");
      return;
    }


    this.isLoading = true;


    this.authService.confirmEmail(this.token)
      .subscribe({

        next: (response) => {

          console.log("EMAIL CONFIRMED:", response);

          this.isLoading = false;
          this.isConfirmed = true;
          this.message = 
            'Your email has been successfully confirmed!';
          this.startRedirectCountdown();

        },


        error: (error) => {

          console.log("CONFIRM ERROR:", error);

          this.isLoading = false;
          this.message =
            'Confirmation link is invalid or expired.';

        }

      });

  }

  private startRedirectCountdown(): void {
    this.redirectTimer = setInterval(() => {
      this.redirectSeconds--;
      if (this.redirectSeconds <= 0) {
        clearInterval(this.redirectTimer);
        this.router.navigate(['/']);
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
  }

}