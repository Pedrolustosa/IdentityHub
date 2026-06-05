import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { SessionTokensService } from '../services/session-tokens.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(SessionTokensService);
  const token = tokens.getAccessToken();

  if (!token) {
    return next(req);
  }

  const authReq = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`
    }
  });

  return next(authReq);
};
