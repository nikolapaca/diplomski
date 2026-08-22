import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService } from '../service/auth-service';
import { ResetPassword as ResetPasswordModel } from '../model/reset-password.model';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit, OnDestroy {

  public token: string | null = null;
  public form: FormGroup;
  public message = '';
  public isError = false;
  public isSuccess = false;
  public isLoading = false;
  public redirectSeconds = 3;
  private redirectTimer: any;

  public constructor(private route: ActivatedRoute, private authService: AuthService, private router: Router) {
    this.form = new FormGroup({
      newPassword: new FormControl('', [Validators.required, Validators.minLength(6)]),
      confirmPassword: new FormControl('', [Validators.required])
    });
  }

  public ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token');

    if (!this.token) {
      this.message = 'Invalid or missing reset link.';
      this.isError = true;
    }
  }

  public onSubmit(): void {
    if (!this.token) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.form.value.newPassword !== this.form.value.confirmPassword) {
      this.isError = true;
      this.message = 'Passwords do not match.';
      return;
    }

    const request: ResetPasswordModel = {
      token: this.token,
      newPassword: this.form.value.newPassword,
      confirmPassword: this.form.value.confirmPassword
    };

    this.isLoading = true;
    this.isError = false;
    this.message = '';

    this.authService.resetPassword(request).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        this.isSuccess = true;
        this.message = response?.message || 'Password has been reset. You can now log in with your new password.';
        this.redirectTimer = setInterval(() => {
          this.redirectSeconds--;
          if (this.redirectSeconds <= 0) {
            clearInterval(this.redirectTimer);
            this.router.navigate(['/']);
          }
        }, 1000);
      },
      error: (error) => {
        this.isLoading = false;
        this.isError = true;
        this.message = error.error?.message || 'Reset link is invalid or expired.';
      }
    });
  }

  ngOnDestroy(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
    }
  }
}