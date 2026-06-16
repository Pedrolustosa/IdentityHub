import { HttpBackend, HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, firstValueFrom, from, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../services/auth.service';
import { SessionTokensService } from '../services/session-tokens.service';

const reattemptHeader = 'X-Auth-Reattempt';

/** Avoids two parallel POST /refresh calls (server revokes old refresh token). */
let refreshAccessPromise: Promise<string> | null = null;

function sharedRefreshAccessToken(
  rawClient: HttpClient,
  tokens: SessionTokensService
): Promise<string> {
  refreshAccessPromise ??= firstValueFrom(
    rawClient.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true })
  )
    .then((res) => {
      tokens.updateAccessToken(res.token);
      return res.token;
    })
    .finally(() => {
      refreshAccessPromise = null;
    });

  return refreshAccessPromise;
}

export const authRefreshInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(SessionTokensService);
  const router = inject(Router);
  const httpBackend = inject(HttpBackend);
  const rawClient = new HttpClient(httpBackend);

  return next(req).pipe(
    catchError((err: unknown) => {
      if (!(err instanceof HttpErrorResponse) || err.status !== 401) {
        return throwError(() => err);
      }
      if (req.headers.has(reattemptHeader)) {
        return throwError(() => err);
      }

      return from(sharedRefreshAccessToken(rawClient, tokens)).pipe(
        switchMap((access) =>
          next(
            req.clone({
              headers: req.headers.set(reattemptHeader, '1'),
              setHeaders: { Authorization: `Bearer ${access}` }
            })
          )
        ),
        catchError((refreshErr: unknown) => {
          tokens.clearAll();
          void router.navigate(['/login']);
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
