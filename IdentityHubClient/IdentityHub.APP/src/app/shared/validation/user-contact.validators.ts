import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Mirrors backend UserContactValidation rules (E.164-style). */
export const PHONE_NUMBER_MAX_LENGTH = 32;
export const PHONE_NUMBER_PATTERN = /^\+[1-9]\d{7,14}$/;
export const MIN_DATE_OF_BIRTH = '1900-01-01';

export function optionalPhoneNumberValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = (control.value ?? '') as string;
    if (!raw.trim()) {
      return null;
    }

    const value = raw.trim();
    if (value.length > PHONE_NUMBER_MAX_LENGTH || !PHONE_NUMBER_PATTERN.test(value)) {
      return { phoneNumberInvalid: true };
    }

    return null;
  };
}

export function optionalDateOfBirthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const raw = (control.value ?? '') as string;
    if (!raw.trim()) {
      return null;
    }

    if (!/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
      return { dateOfBirthInvalid: true };
    }

    const today = new Date();
    const todayIso = [
      today.getUTCFullYear(),
      String(today.getUTCMonth() + 1).padStart(2, '0'),
      String(today.getUTCDate()).padStart(2, '0')
    ].join('-');

    if (raw < MIN_DATE_OF_BIRTH) {
      return { dateOfBirthTooEarly: true };
    }

    if (raw > todayIso) {
      return { dateOfBirthInFuture: true };
    }

    return null;
  };
}

/** Empty string / whitespace → null for API payloads. */
export function normalizeOptionalText(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed.length > 0 ? trimmed : null;
}

/** Empty date input → null; otherwise yyyy-MM-dd. */
export function normalizeOptionalDate(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed.length > 0 ? trimmed : null;
}
