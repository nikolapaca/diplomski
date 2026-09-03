import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from "@angular/material/input";
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Router, RouterModule } from '@angular/router';
import { CredentialsDTO} from '../../../core/model/login-credentials.model';
import { AuthService } from '../../../core/service/auth-service';
import { TokenService} from '../../../core/service/token-service';
 
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule,
    MatFormFieldModule, 
    MatInputModule,     
    CommonModule,
    MatButtonModule,
    MatSnackBarModule,
    RouterModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  public loginForm: FormGroup;
  public constructor(private router: Router, private snackBar: MatSnackBar, private authService: AuthService, private tokenService: TokenService) {
    this.loginForm = new FormGroup({
      username: new FormControl('', Validators.required),
      password: new FormControl('', [Validators.required, Validators.minLength(6)])
    });
  }

  public OnSubmit() : void {
    const credentials: CredentialsDTO = {
      username: this.loginForm.value.username,
      password: this.loginForm.value.password
    }
    this.authService.loginUser(credentials).subscribe({next: (response) => {
      this.tokenService.saveAccessToken(response.accessToken);
      this.router.navigate(['/home']);
    },
    error: (error) => {
      this.tokenService.clear();
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
    }
    });
  }  
}