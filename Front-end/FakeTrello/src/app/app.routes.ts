import { Routes } from '@angular/router';
import { Register } from './feature/user/register/register';
import { Login } from './feature/user/login/login';
import { Homepage } from './homepage/homepage';
import { BoardOverview } from './feature/board/board-overview/board-overview';
import { AuthGuard } from './core/guard/auth.guard';
import { GuestGuard } from './core/guard/guest.guard';
import { NotFound } from './core/not-found/not-found';
import { ProfileComponent } from './feature/user/profile-component/profile-component';


export const routes: Routes = [
    { path: 'register', component: Register, canActivate: [GuestGuard] },
    { path: '', component: Login, canActivate: [GuestGuard] },
    { path: 'home', component: Homepage, canActivate: [AuthGuard] },
    { path: 'boards/:username/:boardName', component: BoardOverview, canActivate: [AuthGuard] },
    { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
    { path: '**', component: NotFound }
];
