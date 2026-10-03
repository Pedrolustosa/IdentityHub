import { HttpBackend, HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../services/auth.service';
import { SessionTokensService } from '../services/session-tokens.service';

/**
 * Restores an in-memory access token from the HttpOnly refresh cookie after a full page load.
 */
export function initializeAuthSession(
  tokens: SessionTokensService,
  httpBackend: HttpBackend
): () => Promise<void> {
  return async () => {
    if (typeof window === 'undefined') {
      return;
    }

    tokens.clearLegacyAccessTokens();

    if (tokens.getAccessToken()) {
      return;
    }

    try {
      const rawClient = new HttpClient(httpBackend);
      const response = await firstValueFrom(
        rawClient.post<AuthResponse>(`${environment.apiUrl}/auth/refresh`, {}, { withCredentials: true })
      );

      if (response?.token) {
        tokens.setAccessTokenInMemory(response.token);
      }
    } catch {
      // No valid refresh cookie — remain signed out.
    }
  };
}
