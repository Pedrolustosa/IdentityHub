const DEFAULT_TOAST_MAX_LENGTH = 140;

export function normalizeToastMessage(
  message: string | null | undefined,
  fallback: string,
  maxLength = DEFAULT_TOAST_MAX_LENGTH
): string {
  const compact = (message ?? '').replace(/\s+/g, ' ').trim();

  if (!compact) {
    return fallback;
  }

  if (compact.length <= maxLength) {
    return compact;
  }

  return `${compact.slice(0, Math.max(0, maxLength - 3)).trimEnd()}...`;
}
