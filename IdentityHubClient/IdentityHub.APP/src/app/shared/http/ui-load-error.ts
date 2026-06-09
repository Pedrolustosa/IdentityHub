import { HttpErrorResponse } from '@angular/common/http';

export type MapHttpToUiLoadErrorOptions = {
  /**
   * When true, HTTP 401 maps to `unknown` with a message from the response body instead of `unauthorized`.
   * Use on sign-in and sign-up forms where 401 means invalid credentials, not an expired session.
   */
  authForm401AsInvalid?: boolean;
};

type ApiErrorPayload = {
  error?: string;
  detail?: string;
  title?: string;
  message?: string;
};

function readApiPayload(err: HttpErrorResponse): ApiErrorPayload | null {
  if (typeof err.error === 'object' && err.error !== null) {
    return err.error as ApiErrorPayload;
  }

  return null;
}

function httpErrorCode(err: HttpErrorResponse): string | undefined {
  const payload = readApiPayload(err);
  const code = payload?.error;

  if (typeof code === 'string' && code.trim()) {
    return code.trim();
  }

  return undefined;
}

function extractValidationDetails(detail: string | undefined): string[] | undefined {
  if (!detail?.trim()) {
    return undefined;
  }

  const parts = detail
    .split('|')
    .map((part) => part.replace(/\s+/g, ' ').trim())
    .filter((part) => part.length > 0);

  if (!parts.length) {
    return undefined;
  }

  const distinct = Array.from(new Set(parts));
  return distinct.length > 1 ? distinct : undefined;
}

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
  | { kind: 'unknown'; message?: string; code?: string; details?: string[] };

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
    const code = httpErrorCode(err);

    return {
      kind: 'unknown',
      code,
      message: detail ?? (typeof err.error === 'string' ? err.error : err.message),
      details: code === 'Validation.Failed' ? extractValidationDetails(detail) : undefined
    };
  }

  return { kind: 'unknown', message: err instanceof Error ? err.message : undefined };
}

export function uiLoadErrorShowsRetry(error: UiLoadError): boolean {
  return error.kind === 'network' || error.kind === 'server' || error.kind === 'unknown';
}

export function toastMessageForUiLoadError(error: UiLoadError): string {
  switch (error.kind) {
    case 'forbidden':
      return 'You do not have permission to perform this action. Check the page details for more information.';
    case 'unauthorized':
      return 'Your session has expired. Please sign in again.';
    case 'not_found':
      return 'The requested resource was not found. Check the page details for more information.';
    case 'network':
      return 'Cannot reach the server. Please check your connection and try again.';
    case 'server':
      return `A server error occurred (${error.status}). Please try again or contact support.`;
    default:
      if (error.message?.trim()) {
        return error.message.trim();
      }
      return 'We could not complete your request. Check the page details for more information.';
  }
}
