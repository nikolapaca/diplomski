// src/app/pages/profile-page/profile-page.component.ts

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl, Validators, FormGroup } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { UserService } from '../../../core/service/user-service';
import { User } from '../../../core/model/user.model';

@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './profile-component.html',
  styleUrls: ['./profile-component.css']
})
export class ProfileComponent implements OnInit {
  
  // Kontrole za formu
  usernameCtrl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.minLength(3)] });
  emailCtrl = new FormControl<string>('', { nonNullable: true, validators: [Validators.required, Validators.email] });
  nameCtrl = new FormControl<string>('', { nonNullable: true });
  surnameCtrl = new FormControl<string>('', { nonNullable: true });
  passwordCtrl = new FormControl<string>('', { nonNullable: true, validators: [Validators.minLength(6)] });
  
  form = new FormGroup({
    username: this.usernameCtrl,
    email: this.emailCtrl,
    name: this.nameCtrl,
    surname: this.surnameCtrl,
    password: this.passwordCtrl
  });

  loading = false;
  saving = false;
  userId!: number; // ID trenutnog korisnika

  constructor(
    private userService: UserService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loading = true;
    this.userService.getProfile().subscribe({
      next: (profile: User) => {
        this.usernameCtrl.setValue(profile.username);
        this.emailCtrl.setValue(profile.email);
        this.nameCtrl.setValue(profile.name ?? '');
        this.surnameCtrl.setValue(profile.surname ?? '');
        this.loading = false;
      },
      error: (err) => {
        this.snackBar.open('Greška pri dohvaćanju profila.', 'Zatvori', { duration: 5000 });
        this.loading = false;
        console.error(err);
      }
    });
  }

  saveProfile(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.snackBar.open('Molimo ispravite greške u formi.', 'Zatvori', { duration: 3000 });
      return;
    }

    this.saving = true;
    
    const updatedProfile: User = {
      username: this.usernameCtrl.value,
      email: this.emailCtrl.value,
      name: this.nameCtrl.value,
      surname: this.surnameCtrl.value,
      password: this.passwordCtrl.value
    };

    /*this.userService.updateProfile(updatedProfile).subscribe({
      next: () => {
        this.saving = false;
        this.snackBar.open('Profil uspješno ažuriran!', 'OK', { duration: 3000, panelClass: ['snackbar-success'] });
        this.form.markAsPristine(); // Označi formu kao čistu nakon spremanja
      },
      error: (err) => {
        this.saving = false;
        const errorMessage = err.error?.message || 'Ažuriranje profila nije uspjelo.';
        this.snackBar.open(errorMessage, 'Zatvori', { duration: 5000 });
        console.error(err);
      }
    });*/
  }
}