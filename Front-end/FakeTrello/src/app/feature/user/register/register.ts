import { Component } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, ValidatorFn, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { UserService } from '../../../core/service/user-service';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router, RouterModule } from '@angular/router';
import { User } from '../../../core/model/user.model';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmPassword = control.get('confirmPassword');

  if (!password || !confirmPassword) {
    return null;
  }

  return password.value === confirmPassword.value ? null : { 'passwordMismatch': true };
};

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule,
    MatFormFieldModule, 
    MatInputModule,     
    CommonModule,
    MatButtonModule,
    MatSnackBarModule,
    RouterModule
  ],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  public registerForm: FormGroup;

  public constructor(private userService: UserService, private snackBar: MatSnackBar, private router: Router) {
    this.registerForm = new FormGroup({
      name: new FormControl('', Validators.required),
      surname: new FormControl('', Validators.required),
      email: new FormControl('', [Validators.required, Validators.email]),
      username: new FormControl('', Validators.required),
      password: new FormControl('', [Validators.required, Validators.minLength(6)]),
      confirmPassword: new FormControl('', [Validators.required, Validators.minLength(6)])
    }, { validators: passwordMatchValidator });
  }

  public OnSubmit() : void {

    const user: User = {
      name: this.registerForm.value.name,
      surname: this.registerForm.value.surname,
      email: this.registerForm.value.email,
      username: this.registerForm.value.username,
      password: this.registerForm.value.password,
    };

    this.userService.registerUser(user).subscribe(response => {
      this.snackBar.open('You were registered successfully!', 'Close', {
            duration: 3000,
            panelClass: ['success-snackbar']
      });
    this.router.navigate(['']);
    }, error => {

        let errorMessage = 'Something went wrong. Please try again.';

        if (error.error && typeof error.error === 'string') {
          errorMessage = error.error;
        } else if (error.error && error.error.errors) {
          const firstError = Object.values(error.error.errors)[0] as string[];
          if (firstError && firstError.length > 0) {
            errorMessage = firstError[0];
          }
        }
        
        this.snackBar.open(errorMessage, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
    });
  }
}
