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
import { AuditLogDetailComponent } from './features/audit-logs/pages/audit-logs/audit-log-detail/audit-log-detail.component';
import { SecurityAlertsComponent } from './features/security-alerts/pages/security-alerts/security-alerts.component';
import { SecurityAlertDetailComponent } from './features/security-alerts/pages/security-alerts/security-alert-detail/security-alert-detail.component';
import { PermissionsMatrixComponent } from './features/permissions/pages/permissions-matrix/permissions-matrix.component';
import { PermissionsCatalogComponent } from './features/permissions/pages/permissions-catalog/permissions-catalog.component';
import { SessionsComponent } from './features/sessions/pages/sessions/sessions.component';
import { UserInvitesComponent } from './features/user-invites/pages/user-invites/user-invites.component';
import { SecuritySettingsComponent } from './features/security-settings/pages/security-settings/security-settings.component';
import { ActivityComponent } from './features/activity/pages/activity/activity.component';
import { AccessDeniedComponent } from './features/access-denied/pages/access-denied/access-denied.component';
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
      {
        path: 'profile/access',
        component: MyAccessComponent,
        data: { title: 'My Access', breadcrumbs: [{ label: 'Profile', link: '/app/profile' }, { label: 'Access' }] }
      },
      {
        path: 'access-denied',
        component: AccessDeniedComponent,
        data: { title: 'Access denied', breadcrumbs: [{ label: 'Access denied' }] }
      },
      { path: 'home', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'audit-logs',
        component: AuditLogsComponent,
        canActivate: [permissionGuard],
        data: { permission: 'Audit.View', title: 'Audit Logs', breadcrumbs: [{ label: 'Audit Logs' }] }
      },
      {
        path: 'audit-logs/:id',
        component: AuditLogDetailComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Audit.View',
          title: 'Audit log detail',
          breadcrumbs: [
            { label: 'Audit Logs', link: '/app/audit-logs' },
            { label: 'Detail' }
          ]
        }
      },
      {
        path: 'security-alerts',
        component: SecurityAlertsComponent,
        canActivate: [permissionGuard],
        data: { permission: 'SecurityEvents.View', title: 'Security Alerts', breadcrumbs: [{ label: 'Security Alerts' }] }
      },
      {
        path: 'security-alerts/:id',
        component: SecurityAlertDetailComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'SecurityEvents.View',
          title: 'Security alert detail',
          breadcrumbs: [
            { label: 'Security Alerts', link: '/app/security-alerts' },
            { label: 'Detail' }
          ]
        }
      },
      {
        path: 'permissions/matrix',
        component: PermissionsMatrixComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Roles.Permissions.View',
          title: 'Permissions matrix',
          breadcrumbs: [{ label: 'Permissions matrix' }]
        }
      },
      {
        path: 'permissions/catalog',
        component: PermissionsCatalogComponent,
        canActivate: [permissionGuard],
        data: {
          permission: 'Roles.Permissions.View',
          title: 'Permissions catalog',
          breadcrumbs: [{ label: 'Permissions catalog' }]
        }
      },
      {
        path: 'sessions',
        component: SessionsComponent,
        canActivate: [permissionGuard],
        data: {
          permissions: ['Sessions.View', 'Users.View'],
          title: 'System sessions',
          breadcrumbs: [{ label: 'System sessions' }]
        }
      },
      {
        path: 'user-invites',
        component: UserInvitesComponent,
        canActivate: [permissionGuard],
        data: {
          permissions: ['Users.Invites.View', 'Users.View'],
          title: 'User invites',
          breadcrumbs: [{ label: 'User invites' }]
        }
      },
      {
        path: 'security-settings',
        component: SecuritySettingsComponent,
        canActivate: [permissionGuard],
        data: {
          permissions: ['SecuritySettings.View', 'SecurityEvents.Manage'],
          title: 'Security settings',
          breadcrumbs: [{ label: 'Security settings' }]
        }
      },
      {
        path: 'activity',
        component: ActivityComponent,
        canActivate: [permissionGuard],
        data: {
          permissions: ['Activity.View', 'Audit.View'],
          title: 'Recent activity',
          breadcrumbs: [{ label: 'Recent activity' }]
        }
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
