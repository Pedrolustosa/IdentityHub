import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { guestGuard } from '../../core/guards/guest.guard';

export const authLayoutChildRoutes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/register/register.component').then((m) => m.RegisterComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      ),
    canActivate: [guestGuard]
  },
  {
    path: 'resend-confirmation',
    loadComponent: () =>
      import('./pages/resend-confirmation/resend-confirmation.component').then(
        (m) => m.ResendConfirmationComponent
      ),
    canActivate: [guestGuard]
  },
  {
    path: 'confirm-email',
    loadComponent: () =>
      import('./pages/confirm-email/confirm-email.component').then(
        (m) => m.ConfirmEmailComponent
      )
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./pages/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      )
  }
];
