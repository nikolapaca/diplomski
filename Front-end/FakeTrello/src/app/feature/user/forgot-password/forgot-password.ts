import { Component, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/service/auth-service';
import { ForgotPasswordRequest } from '../../../core/model/forgot-password-request.model';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule
  ],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {

  public form: FormGroup;
  public message = '';
  public isError = false;
  public isSubmitted = false;
  public isLoading = false;

  public constructor(private authService: AuthService, private cdr: ChangeDetectorRef) {
    this.form = new FormGroup({
      email: new FormControl('', [Validators.required, Validators.email])
    });
  }

  public onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: ForgotPasswordRequest = {
      email: this.form.value.email
    };

    this.isLoading = true;
    this.message = '';
    this.isError = false;

    this.authService.forgotPassword(request).subscribe({
      next: (response: any) => {
        this.isLoading = false;
        this.isSubmitted = true;
        this.message = response?.message || 'If an account with that email exists, a password reset link has been sent.';
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.isLoading = false;
        this.isError = true;
        this.message = error.error?.message || 'Something went wrong. Please try again.';
        this.cdr.markForCheck();
      }
    });
  }
}
