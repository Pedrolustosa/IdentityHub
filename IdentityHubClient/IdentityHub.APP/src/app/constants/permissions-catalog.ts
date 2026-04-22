/**
 * Known permission claim values (type "permission" on roles).
 * Keep in sync with IdentityHub.Application.Authorization.Permissions on the server.
 */
export const PERMISSION_CATALOG: readonly string[] = [
  'Users.View',
  'Users.Create',
  'Users.Update',
  'Users.Delete',
  'Roles.View',
  'Roles.Manage'
];
