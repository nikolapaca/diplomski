import { Routes } from '@angular/router';
import { Register } from './feature/user/register/register';
import { Login } from './feature/user/login/login';
import { Homepage } from './homepage/homepage';
import { BoardOverview } from './feature/board/board-overview/board-overview';
import { AuthGuard } from './core/guard/auth.guard';
import { GuestGuard } from './core/guard/guest.guard';
import { NotFound } from './core/not-found/not-found';
import { ProfileComponent } from './feature/user/profile-component/profile-component';
import { EditProfileComponent } from './feature/user/edit-profile-component/edit-profile-component';
import { ConfirmEmail } from './core/confirm-email/confirm-email';
import { ForgotPassword } from './core/forgot-password/forgot-password';
import { ResetPassword } from './core/reset-password/reset-password';


export const routes: Routes = [
    { path: 'register', component: Register, canActivate: [GuestGuard] },
    { path: '', component: Login, canActivate: [GuestGuard] },
    { path: 'home', component: Homepage, canActivate: [AuthGuard] },
    { path: 'boards/:username/:boardName', component: BoardOverview, canActivate: [AuthGuard] },
    { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
    { path: 'edit-profile', component: EditProfileComponent, canActivate: [AuthGuard] },
    { path: 'not-found', component: NotFound },
    { path: 'confirm-email', component: ConfirmEmail},
    { path: 'forgot-password', component: ForgotPassword, canActivate: [GuestGuard] },
    { path: 'reset-password', component: ResetPassword, canActivate: [GuestGuard] },
    { path: '**', component: NotFound }
];
