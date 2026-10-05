import { Routes } from '@angular/router';
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component';
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
    loadComponent: () =>
      import('./layouts/main-layout/main-layout.component').then((m) => m.MainLayoutComponent),
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          ),
        canActivate: [permissionGuard],
        data: { permission: 'Dashboard.View', title: 'Dashboard', breadcrumbs: [{ label: 'Dashboard' }] }
      },
      { path: 'change-password', redirectTo: 'profile', pathMatch: 'full' },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/pages/profile/profile.component').then(
            (m) => m.ProfileComponent
          ),
        data: { title: 'Profile', breadcrumbs: [{ label: 'Profile' }] }
      },
      {
        path: 'my-sessions',
        loadComponent: () =>
          import('./features/my-sessions/pages/my-sessions/my-sessions.component').then(
            (m) => m.MySessionsComponent
          ),
        data: { title: 'My Sessions', breadcrumbs: [{ label: 'My Sessions' }] }
      },
      {
        path: 'my-access',
        loadComponent: () =>
          import('./features/my-access/pages/my-access/my-access.component').then(
            (m) => m.MyAccessComponent
          ),
        data: { title: 'My Access', breadcrumbs: [{ label: 'My Access' }] }
      },
      {
        path: 'profile/access',
        redirectTo: 'my-access',
        pathMatch: 'full'
      },
      {
        path: 'profile/sessions',
        redirectTo: 'my-sessions',
        pathMatch: 'full'
      },
      {
        path: 'access-denied',
        loadComponent: () =>
          import('./features/access-denied/pages/access-denied/access-denied.component').then(
            (m) => m.AccessDeniedComponent
          ),
        data: { title: 'Access denied', breadcrumbs: [{ label: 'Access denied' }] }
      },
      { path: 'home', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'audit-logs',
        loadComponent: () =>
          import('./features/audit-logs/pages/audit-logs/audit-logs.component').then(
            (m) => m.AuditLogsComponent
          ),
        canActivate: [permissionGuard],
        data: { permission: 'Audit.View', title: 'Audit Logs', breadcrumbs: [{ label: 'Audit Logs' }] }
      },
      {
        path: 'audit-logs/:id',
        loadComponent: () =>
          import('./features/audit-logs/pages/audit-logs/audit-log-detail/audit-log-detail.component').then(
            (m) => m.AuditLogDetailComponent
          ),
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
        loadComponent: () =>
          import('./features/security-alerts/pages/security-alerts/security-alerts.component').then(
            (m) => m.SecurityAlertsComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'SecurityEvents.View',
          title: 'Security Alerts',
          breadcrumbs: [{ label: 'Security Alerts' }]
        }
      },
      {
        path: 'security-alerts/:id',
        loadComponent: () =>
          import(
            './features/security-alerts/pages/security-alerts/security-alert-detail/security-alert-detail.component'
          ).then((m) => m.SecurityAlertDetailComponent),
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
        loadComponent: () =>
          import('./features/permissions/pages/permissions-matrix/permissions-matrix.component').then(
            (m) => m.PermissionsMatrixComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'Permissions.Matrix.View',
          title: 'Permissions matrix',
          breadcrumbs: [{ label: 'Permissions matrix' }]
        }
      },
      {
        path: 'permissions/catalog',
        loadComponent: () =>
          import('./features/permissions/pages/permissions-catalog/permissions-catalog.component').then(
            (m) => m.PermissionsCatalogComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'Permissions.Catalog.View',
          title: 'Permissions catalog',
          breadcrumbs: [{ label: 'Permissions catalog' }]
        }
      },
      {
        path: 'sessions',
        loadComponent: () =>
          import('./features/sessions/pages/sessions/sessions.component').then(
            (m) => m.SessionsComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'Sessions.View',
          title: 'System sessions',
          breadcrumbs: [{ label: 'System sessions' }]
        }
      },
      {
        path: 'user-invites',
        loadComponent: () =>
          import('./features/user-invites/pages/user-invites/user-invites.component').then(
            (m) => m.UserInvitesComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'UserInvites.View',
          title: 'User invites',
          breadcrumbs: [{ label: 'User invites' }]
        }
      },
      {
        path: 'security-settings',
        loadComponent: () =>
          import('./features/security-settings/pages/security-settings/security-settings.component').then(
            (m) => m.SecuritySettingsComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'SecuritySettings.View',
          title: 'Security settings',
          breadcrumbs: [{ label: 'Security settings' }]
        }
      },
      {
        path: 'activity',
        loadComponent: () =>
          import('./features/activity/pages/activity/activity.component').then(
            (m) => m.ActivityComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'Activity.View',
          title: 'Recent activity',
          breadcrumbs: [{ label: 'Recent activity' }]
        }
      },
      {
        path: 'roles/:roleId/permissions/edit',
        loadComponent: () =>
          import('./features/role-claims/pages/role-claims/role-claims-edit/role-claims-edit.component').then(
            (m) => m.RoleClaimsEditComponent
          ),
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
        loadComponent: () =>
          import(
            './features/role-claims/pages/role-claims/role-claims-detail/role-claims-detail.component'
          ).then((m) => m.RoleClaimsDetailComponent),
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
        loadComponent: () =>
          import('./features/role-claims/pages/role-claims/role-claims.component').then(
            (m) => m.RoleClaimsComponent
          ),
        canActivate: [permissionGuard],
        data: {
          permission: 'Roles.View',
          title: 'Role Permissions',
          breadcrumbs: [{ label: 'Role Permissions' }]
        }
      },
      { path: 'role-claims', redirectTo: 'roles', pathMatch: 'full' },
      { path: 'role-claims/:roleId', redirectTo: 'roles/:roleId/permissions', pathMatch: 'full' },
      { path: 'role-claims/:roleId/edit', redirectTo: 'roles/:roleId/permissions/edit', pathMatch: 'full' },
      {
        path: 'users/create',
        loadComponent: () =>
          import('./features/users/pages/users/user-create/user-create.component').then(
            (m) => m.UserCreateComponent
          ),
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
        loadComponent: () =>
          import('./features/users/pages/users/user-edit/user-edit.component').then(
            (m) => m.UserEditComponent
          ),
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
        loadComponent: () =>
          import('./features/users/pages/users/user-detail/user-detail.component').then(
            (m) => m.UserDetailComponent
          ),
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
        loadComponent: () =>
          import('./features/users/pages/users/users.component').then((m) => m.UsersComponent),
        canActivate: [permissionGuard],
        data: { permission: 'Users.View', title: 'Users', breadcrumbs: [{ label: 'Users' }] }
      }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
