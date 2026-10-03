/** Resolves an in-app return URL and rejects open redirects. */
export function resolveSafeReturnUrl(raw: unknown, fallback = '/app/dashboard'): string {
  if (typeof raw !== 'string') {
    return fallback;
  }

  const value = raw.trim();
  if (!value.startsWith('/') || value.startsWith('//') || value.includes('://')) {
    return fallback;
  }

  return value;
}
