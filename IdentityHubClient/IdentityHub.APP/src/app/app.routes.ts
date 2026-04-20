import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { RegisterComponent } from './pages/register/register.component';
import { ShellComponent } from './layout/shell.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { UsersComponent } from './pages/users/users.component';
import { UserDetailComponent } from './pages/users/user-detail/user-detail.component';
import { UserEditComponent } from './pages/users/user-edit/user-edit.component';
import { UserDeleteComponent } from './pages/users/user-delete/user-delete.component';
import { RoleClaimsComponent } from './pages/role-claims/role-claims.component';
import { RoleClaimsDetailComponent } from './pages/role-claims/role-claims-detail/role-claims-detail.component';
import { RoleClaimsEditComponent } from './pages/role-claims/role-claims-edit/role-claims-edit.component';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [guestGuard] },
  {
    path: 'app',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'home', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'role-claims/:roleId/edit', component: RoleClaimsEditComponent },
      { path: 'role-claims/:roleId', component: RoleClaimsDetailComponent },
      { path: 'role-claims', component: RoleClaimsComponent },
      { path: 'users/:id/edit', component: UserEditComponent },
      { path: 'users/:id/delete', component: UserDeleteComponent },
      { path: 'users/:id', component: UserDetailComponent },
      { path: 'users', component: UsersComponent }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
