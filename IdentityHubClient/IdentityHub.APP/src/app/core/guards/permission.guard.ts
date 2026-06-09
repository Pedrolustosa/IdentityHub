import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

type PermissionRouteData = {
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
};

export const permissionGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }

  const data = (route.data ?? {}) as PermissionRouteData;
  const single = typeof data.permission === 'string' && data.permission.trim() ? [data.permission.trim()] : [];
  const many = (data.permissions ?? []).map((entry) => entry.trim()).filter((entry) => entry.length > 0);
  const required = [...new Set([...single, ...many])];

  if (required.length === 0) {
    return true;
  }

  const hasRequiredPermissions = data.requireAll
    ? required.every((permission) => authService.hasPermission(permission))
    : required.some((permission) => authService.hasPermission(permission));

  if (hasRequiredPermissions) {
    return true;
  }

  return router.createUrlTree(['/app/profile']);
};
