import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './layouts/main-layout/main-layout.component';
import { DashboardComponent } from './features/dashboard/pages/dashboard/dashboard.component';
import { ProfileComponent } from './features/profile/pages/profile/profile.component';
import { MyAccessComponent } from './features/my-access/pages/my-access/my-access.component';
import { UsersComponent } from './features/users/pages/users/users.component';
import { UserCreateComponent } from './features/users/pages/users/user-create/user-create.component';
import { UserDetailComponent } from './features/users/pages/users/user-detail/user-detail.component';
import { UserEditComponent } from './features/users/pages/users/user-edit/user-edit.component';
import { RoleClaimsComponent } from './features/role-claims/pages/role-claims/role-claims.component';
import { RoleClaimsDetailComponent } from './features/role-claims/pages/role-claims/role-claims-detail/role-claims-detail.component';
import { RoleClaimsEditComponent } from './features/role-claims/pages/role-claims/role-claims-edit/role-claims-edit.component';
import { AuditLogsComponent } from './features/audit-logs/pages/audit-logs/audit-logs.component';
import { SecurityAlertsComponent } from './features/security-alerts/pages/security-alerts/security-alerts.component';
import { authGuard } from './core/guards/auth.guard';
import { permissionGuard } from './core/guards/permission.guard';
import { authLayoutChildRoutes } from './features/auth/auth.routes';

export const routes: Routes = [
  {
    path: '',
    component: AuthLayoutComponent,
    children: authLayoutChildRoutes
  },
  {
    path: 'app',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        component: DashboardComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Dashboard.View', title: 'Dashboard', breadcrumbs: [{ label: 'Dashboard' }] }
      },
      { path: 'change-password', redirectTo: 'profile', pathMatch: 'full' },
      {
        path: 'profile',
        component: ProfileComponent,
        data: { title: 'Profile', breadcrumbs: [{ label: 'Profile' }] }
      },
      {
        path: 'my-access',
        component: MyAccessComponent,
        data: { title: 'My Access', breadcrumbs: [{ label: 'My Access' }] }
      },
      { path: 'home', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'audit-logs',
        component: AuditLogsComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Audit.View', title: 'Audit Logs', breadcrumbs: [{ label: 'Audit Logs' }] }
      },
      {
        path: 'security-alerts',
        component: SecurityAlertsComponent,
        canActivate: [permissionGuard],
        data: { permissions: ['SecurityEvents.View', 'Audit.View'], title: 'Security Alerts', breadcrumbs: [{ label: 'Security Alerts' }] }
      },
      {
        path: 'roles/:roleId/permissions/edit',
        component: RoleClaimsEditComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Roles.Permissions.Update',
          title: 'Edit permissions',
          breadcrumbs: [
            { label: 'Role Permissions', link: '/app/roles' },
            { label: 'Permissions' },
            { label: 'Edit' }
          ]
        }
      },
      {
        path: 'roles/:roleId/permissions',
        component: RoleClaimsDetailComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Roles.Permissions.View',
          title: 'Role permissions',
          breadcrumbs: [
            { label: 'Role Permissions', link: '/app/roles' },
            { label: 'Permissions' }
          ]
        }
      },
      {
        path: 'roles',
        component: RoleClaimsComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Roles.View', title: 'Role Permissions', breadcrumbs: [{ label: 'Role Permissions' }] }
      },
      { path: 'role-claims', redirectTo: 'roles', pathMatch: 'full' },
      { path: 'role-claims/:roleId', redirectTo: 'roles/:roleId/permissions', pathMatch: 'full' },
      { path: 'role-claims/:roleId/edit', redirectTo: 'roles/:roleId/permissions/edit', pathMatch: 'full' },
      {
        path: 'users/create',
        component: UserCreateComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Users.Create',
          title: 'Create user',
          breadcrumbs: [
            { label: 'Users', link: '/app/users' },
            { label: 'Create' }
          ]
        }
      },
      {
        path: 'users/:id/edit',
        component: UserEditComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Users.Update',
          title: 'Edit user',
          breadcrumbs: [
            { label: 'Users', link: '/app/users' },
            { label: 'Detail' },
            { label: 'Edit' }
          ]
        }
      },
      {
        path: 'users/:id',
        component: UserDetailComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Users.View',
          title: 'User detail',
          breadcrumbs: [
            { label: 'Users', link: '/app/users' },
            { label: 'Detail' }
          ]
        }
      },
      {
        path: 'users',
        component: UsersComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Users.View', title: 'Users', breadcrumbs: [{ label: 'Users' }] }
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
