import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { resolveSafeReturnUrl } from '../auth/safe-return-url';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  const returnUrl = resolveSafeReturnUrl(route.queryParamMap.get('returnUrl'));
  return router.parseUrl(returnUrl);
};
