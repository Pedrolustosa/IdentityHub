import { HttpErrorResponse } from '@angular/common/http';

export type MapHttpToUiLoadErrorOptions = {
  /**
   * When true, HTTP 401 maps to `unknown` with a message from the response body instead of `unauthorized`.
   * Use on sign-in and sign-up forms where 401 means invalid credentials, not an expired session.
   */
  authForm401AsInvalid?: boolean;
};

function httpErrorDetail(err: HttpErrorResponse): string | undefined {
  if (typeof err.error === 'string' && err.error.trim()) {
    return err.error.trim();
  }
  if (typeof err.error === 'object' && err.error !== null) {
    const o = err.error as { detail?: string; title?: string; message?: string };
    const fromApi = o.detail ?? o.message ?? o.title;
    if (typeof fromApi === 'string' && fromApi.trim()) {
      return fromApi.trim();
    }
  }
  return undefined;
}

export type UiLoadError =
  | { kind: 'forbidden' }
  | { kind: 'unauthorized' }
  | { kind: 'not_found' }
  | { kind: 'server'; status: number }
  | { kind: 'network' }
  | { kind: 'unknown'; message?: string };

export function mapHttpToUiLoadError(err: unknown, options?: MapHttpToUiLoadErrorOptions): UiLoadError {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 403) {
      return { kind: 'forbidden' };
    }
    if (err.status === 401) {
      if (options?.authForm401AsInvalid) {
        return { kind: 'unknown', message: httpErrorDetail(err) ?? err.message };
      }
      return { kind: 'unauthorized' };
    }
    if (err.status === 404) {
      return { kind: 'not_found' };
    }
    if (err.status === 0) {
      return { kind: 'network' };
    }
    if (err.status >= 500) {
      return { kind: 'server', status: err.status };
    }
    const detail = httpErrorDetail(err);
    return { kind: 'unknown', message: detail ?? (typeof err.error === 'string' ? err.error : err.message) };
  }
  return { kind: 'unknown', message: err instanceof Error ? err.message : undefined };
}

export function uiLoadErrorShowsRetry(error: UiLoadError): boolean {
  return error.kind === 'network' || error.kind === 'server' || error.kind === 'unknown';
}

export function toastMessageForUiLoadError(error: UiLoadError): string {
  switch (error.kind) {
    case 'forbidden':
      return 'Access denied. See on-screen message.';
    case 'unauthorized':
      return 'Session expired. Sign in again.';
    case 'not_found':
      return 'Not found. See on-screen message.';
    case 'network':
      return 'Cannot reach the server. Check your connection.';
    case 'server':
      return `Server error (${error.status}). Try again or contact support.`;
    default:
      return 'Request failed. See on-screen message.';
  }
}
