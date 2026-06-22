export type NavigationGroup = 'overview' | 'administration' | 'security' | 'account';

export type NavigationItem = {
  label: string;
  route: string;
  icon: string;
  requiredAny: string[];
  badge?: 'securityAlerts';
  group: NavigationGroup;
};

export const NAVIGATION_ITEMS: NavigationItem[] = [
  { label: 'Dashboard', route: '/app/dashboard', icon: 'dashboard', requiredAny: ['Dashboard.View'], group: 'overview' },
  { label: 'Users', route: '/app/users', icon: 'users', requiredAny: ['Users.View'], group: 'administration' },
  {
    label: 'Roles & Permissions',
    route: '/app/roles',
    icon: 'roleClaims',
    requiredAny: ['Roles.View'],
    group: 'administration'
  },
  { label: 'My Sessions', route: '/app/profile', icon: 'sessions', requiredAny: [], group: 'security' },
  {
    label: 'Security Alerts',
    route: '/app/security-alerts',
    icon: 'securityAlerts',
    requiredAny: ['SecurityEvents.View'],
    badge: 'securityAlerts',
    group: 'security'
  },
  { label: 'Audit Logs', route: '/app/audit-logs', icon: 'auditLogs', requiredAny: ['Audit.View'], group: 'security' },
  { label: 'System Sessions', route: '/app/sessions', icon: 'sessions', requiredAny: ['Sessions.View'], group: 'security' },
  { label: 'Recent Activity', route: '/app/activity', icon: 'auditLogs', requiredAny: ['Activity.View'], group: 'security' },
  {
    label: 'Security Settings',
    route: '/app/security-settings',
    icon: 'securityAlerts',
    requiredAny: ['SecuritySettings.View'],
    group: 'security'
  },
  { label: 'My Access', route: '/app/my-access', icon: 'access', requiredAny: [], group: 'account' },
  {
    label: 'User Invites',
    route: '/app/user-invites',
    icon: 'users',
    requiredAny: ['UserInvites.View'],
    group: 'account'
  },
  {
    label: 'Permissions Matrix',
    route: '/app/permissions/matrix',
    icon: 'roleClaims',
    requiredAny: ['Permissions.Matrix.View'],
    group: 'account'
  },
  {
    label: 'Permissions Catalog',
    route: '/app/permissions/catalog',
    icon: 'roleClaims',
    requiredAny: ['Permissions.Catalog.View'],
    group: 'account'
  }
];

export const ACCESS_SCREEN_CATALOG = [
  { label: 'Dashboard', route: '/app/dashboard', requiredAny: ['Dashboard.View'] },
  { label: 'Users', route: '/app/users', requiredAny: ['Users.View'] },
  { label: 'Roles & Permissions', route: '/app/roles', requiredAny: ['Roles.View'] },
  { label: 'Audit Logs', route: '/app/audit-logs', requiredAny: ['Audit.View'] },
  { label: 'Security Alerts', route: '/app/security-alerts', requiredAny: ['SecurityEvents.View'] },
  { label: 'Permissions Matrix', route: '/app/permissions/matrix', requiredAny: ['Permissions.Matrix.View'] },
  { label: 'Permissions Catalog', route: '/app/permissions/catalog', requiredAny: ['Permissions.Catalog.View'] },
  { label: 'System Sessions', route: '/app/sessions', requiredAny: ['Sessions.View'] },
  { label: 'User Invites', route: '/app/user-invites', requiredAny: ['UserInvites.View'] },
  { label: 'Security Settings', route: '/app/security-settings', requiredAny: ['SecuritySettings.View'] },
  { label: 'Recent Activity', route: '/app/activity', requiredAny: ['Activity.View'] }
];
