import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Router } from '@angular/router';
import { UserService } from '../../../core/service/user-service';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './edit-profile-component.html',
  styleUrl: './edit-profile-component.css'
})
export class EditProfileComponent {
  public passwordForm = new FormGroup({
    currentPassword: new FormControl('', Validators.required),
    newPassword: new FormControl('', [Validators.required, Validators.minLength(6)]),
    confirmNewPassword: new FormControl('', Validators.required)
  });

  public changingPassword = false;

  public passwordErrorMessage = '';
  public passwordSuccessMessage = '';

  public hideCurrentPassword = true;
  public hideNewPassword = true;
  public hideConfirmPassword = true;

  public constructor(
    private userService: UserService,
    private router: Router,
    private cdRef: ChangeDetectorRef
  ) {}

  public passwordsDoNotMatch(): boolean {
    const newPassword = this.passwordForm.get('newPassword')?.value;
    const confirmNewPassword = this.passwordForm.get('confirmNewPassword')?.value;

    return !!confirmNewPassword && newPassword !== confirmNewPassword;
  }

  public changePassword(): void {
    if (this.passwordForm.invalid || this.passwordsDoNotMatch()) {
      this.passwordForm.markAllAsTouched();

      if (this.passwordsDoNotMatch()) {
        this.passwordErrorMessage = 'New passwords do not match.';
      }

      return;
    }

    const dto = {
      currentPassword: this.passwordForm.value.currentPassword || '',
      newPassword: this.passwordForm.value.newPassword || '',
      confirmNewPassword: this.passwordForm.value.confirmNewPassword || ''
    };

    this.changingPassword = true;
    this.passwordErrorMessage = '';
    this.passwordSuccessMessage = '';

    this.userService.changePassword(dto).subscribe({
      next: () => {
        this.changingPassword = false;
        this.passwordSuccessMessage = 'Password changed successfully.';
        this.passwordForm.reset();

        this.hideCurrentPassword = true;
        this.hideNewPassword = true;
        this.hideConfirmPassword = true;

        this.cdRef.detectChanges();
      },
      error: (err) => {
        this.changingPassword = false;
        this.passwordErrorMessage = err.error || 'Failed to change password.';
        console.error(err);
        this.cdRef.detectChanges();
      }
    });
  }

  public goBack(): void {
    this.router.navigate(['/home']);
  }
}