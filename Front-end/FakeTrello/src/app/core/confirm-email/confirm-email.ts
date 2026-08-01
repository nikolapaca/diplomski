import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../service/auth-service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [
    CommonModule
  ],
  templateUrl: './confirm-email.html',
  styleUrl: './confirm-email.css',
})
export class ConfirmEmail implements OnInit {

  token: string | null = null;

  message = '';
  isConfirmed = false;
  isLoading = false;


  constructor(
    private route: ActivatedRoute,
    private authService: AuthService
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

        },


        error: (error) => {

          console.log("CONFIRM ERROR:", error);

          this.isLoading = false;
          this.message =
            'Confirmation link is invalid or expired.';

        }

      });

  }

}