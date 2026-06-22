/**
 * Known permission claim values (type "permission" on roles).
 * Keep in sync with IdentityHub.Domain.Constants.AppPermissions on the server.
 * This list defines every row shown on the role-permissions edit screen (checked or not).
 */
export const PERMISSION_CATALOG: readonly string[] = [
  'Users.View',
  'Users.Create',
  'Users.Update',
  'Users.Delete',
  'Users.Roles.Update',
  'Roles.View',
  'Roles.Create',
  'Roles.Update',
  'Roles.Delete',
  'Roles.Permissions.View',
  'Roles.Permissions.Update',
  'Dashboard.View',
  'Sessions.View',
  'Sessions.Revoke',
  'Activity.View',
  'Audit.View',
  'SecurityEvents.View',
  'SecurityEvents.Manage',
  'SecuritySettings.View',
  'SecuritySettings.Update',
  'Permissions.Catalog.View',
  'Permissions.Matrix.View',
  'UserInvites.View',
  'UserInvites.Create',
  'UserInvites.Cancel',
  'UserInvites.Resend'
];
