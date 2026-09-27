import {
  AsYouType,
  getExampleNumber,
  parsePhoneNumberFromString,
  type CountryCode
} from 'libphonenumber-js';
import examples from 'libphonenumber-js/mobile/examples';
import {
  DEFAULT_PHONE_COUNTRY_ISO2,
  findPhoneCountryByIso2,
  getDefaultPhoneCountry,
  PHONE_COUNTRIES,
  PhoneCountry
} from './phone-countries';

export function digitsOnly(value: string | null | undefined): string {
  return (value ?? '').replace(/\D/g, '');
}

/** Format national digits with libphonenumber AsYouType for the selected country. */
export function formatNationalNumber(
  country: PhoneCountry,
  nationalDigits: string
): string {
  const digits = digitsOnly(nationalDigits);
  if (!digits) {
    return '';
  }

  return new AsYouType(country.iso2).input(digits);
}

/** Example national number used as input placeholder. */
export function nationalPlaceholder(country: PhoneCountry): string {
  try {
    const example = getExampleNumber(country.iso2, examples);
    if (example) {
      return example.formatNational();
    }
  } catch {
    // Some territories may not have examples in the mobile set.
  }

  return 'Phone number';
}

/** E.164-style value stored in the API: `+{dialCode}{nationalDigits}`. */
export function toE164(dialCode: string, nationalDigits: string): string {
  const national = digitsOnly(nationalDigits);
  if (!national) {
    return '';
  }

  return `+${digitsOnly(dialCode)}${national}`;
}

export interface ParsedPhoneNumber {
  country: PhoneCountry;
  nationalDigits: string;
  e164: string;
}

/**
 * Detect country + national number from a stored phone value.
 * Uses libphonenumber when possible; falls back to dial-code matching.
 */
export function parseStoredPhoneNumber(
  raw: string | null | undefined,
  preferredIso2: string = DEFAULT_PHONE_COUNTRY_ISO2
): ParsedPhoneNumber {
  const preferred =
    findPhoneCountryByIso2(preferredIso2) ?? getDefaultPhoneCountry();

  if (!raw?.trim()) {
    return { country: preferred, nationalDigits: '', e164: '' };
  }

  const trimmed = raw.trim();
  const parsed = parsePhoneNumberFromString(trimmed.startsWith('+') ? trimmed : `+${digitsOnly(trimmed)}`);

  if (parsed?.country) {
    const country = findPhoneCountryByIso2(parsed.country) ?? preferred;
    return {
      country,
      nationalDigits: parsed.nationalNumber,
      e164: parsed.number
    };
  }

  if (parsed?.countryCallingCode) {
    const byDial = PHONE_COUNTRIES.filter(
      (country) => country.dialCode === parsed.countryCallingCode
    );
    const country =
      byDial.find((item) => item.iso2 === preferred.iso2) ??
      byDial.find((item) => item.iso2 === 'US') ??
      byDial[0] ??
      preferred;

    return {
      country,
      nationalDigits: parsed.nationalNumber,
      e164: parsed.number || toE164(country.dialCode, parsed.nationalNumber)
    };
  }

  // Soft dial-code match for incomplete / legacy values.
  const allDigits = digitsOnly(trimmed);
  if (!allDigits) {
    return { country: preferred, nationalDigits: '', e164: '' };
  }

  const sortedByDialLength = [...PHONE_COUNTRIES].sort(
    (a, b) => b.dialCode.length - a.dialCode.length
  );

  for (const country of sortedByDialLength) {
    if (!allDigits.startsWith(country.dialCode)) {
      continue;
    }

    const national = allDigits.slice(country.dialCode.length);
    if (national.length > 0) {
      return {
        country,
        nationalDigits: national,
        e164: toE164(country.dialCode, national)
      };
    }
  }

  return {
    country: preferred,
    nationalDigits: allDigits,
    e164: toE164(preferred.dialCode, allDigits)
  };
}

export function isCompleteNationalNumber(
  country: PhoneCountry,
  nationalDigits: string
): boolean {
  const digits = digitsOnly(nationalDigits);
  if (!digits) {
    return false;
  }

  const phone = parsePhoneNumberFromString(toE164(country.dialCode, digits), country.iso2);
  return !!phone?.isPossible();
}

/** Upper bound while typing — E.164 national numbers stay within 15 digits total. */
export function maxNationalDigits(country: PhoneCountry): number {
  return Math.max(15 - country.dialCode.length, 4);
}

export function isCountryCode(value: string): value is CountryCode {
  return PHONE_COUNTRIES.some((country) => country.iso2 === value);
}
