import {
  getCountries,
  getCountryCallingCode,
  type CountryCode
} from 'libphonenumber-js';

export interface PhoneCountry {
  /** ISO 3166-1 alpha-2 (libphonenumber CountryCode). */
  readonly iso2: CountryCode;
  readonly name: string;
  /** International dialing code without +. */
  readonly dialCode: string;
}

export interface PhoneCountryFlagAsset {
  readonly src: string;
  readonly srcset: string;
  readonly width: number;
  readonly height: number;
}

export const DEFAULT_PHONE_COUNTRY_ISO2: CountryCode = 'BR';

const regionNames =
  typeof Intl !== 'undefined'
    ? new Intl.DisplayNames(['en'], { type: 'region' })
    : null;

function countryDisplayName(iso2: CountryCode): string {
  return regionNames?.of(iso2) ?? iso2;
}

/**
 * Full international country list from libphonenumber metadata
 * (all territories with a calling code). Brazil stays first; the rest are A–Z by name.
 */
export const PHONE_COUNTRIES: readonly PhoneCountry[] = getCountries()
  .map((iso2) => ({
    iso2,
    name: countryDisplayName(iso2),
    dialCode: getCountryCallingCode(iso2)
  }))
  .sort((a, b) => {
    if (a.iso2 === DEFAULT_PHONE_COUNTRY_ISO2) return -1;
    if (b.iso2 === DEFAULT_PHONE_COUNTRY_ISO2) return 1;
    return a.name.localeCompare(b.name, 'en');
  });

/**
 * Flag images via the free FlagCDN HTTP API
 * (documented at https://www.bandeirasnacionais.com/baixar/api).
 * Pattern: https://flagcdn.com/{width}x{height}/{iso2}.png
 */
export function phoneCountryFlagAsset(
  iso2: string,
  size: 'sm' | 'md' = 'sm'
): PhoneCountryFlagAsset {
  const code = iso2.toLowerCase();
  const width = size === 'md' ? 24 : 20;
  const height = size === 'md' ? 18 : 15;
  const retina2x = size === 'md' ? '48x36' : '40x30';
  const retina3x = size === 'md' ? '72x54' : '60x45';

  return {
    src: `https://flagcdn.com/${width}x${height}/${code}.png`,
    srcset: `https://flagcdn.com/${retina2x}/${code}.png 2x, https://flagcdn.com/${retina3x}/${code}.png 3x`,
    width,
    height
  };
}

export function phoneCountryFlagUrl(iso2: string): string {
  return phoneCountryFlagAsset(iso2).src;
}

export function findPhoneCountryByIso2(iso2: string): PhoneCountry | undefined {
  const code = iso2.toUpperCase() as CountryCode;
  return PHONE_COUNTRIES.find((country) => country.iso2 === code);
}

export function getDefaultPhoneCountry(): PhoneCountry {
  return findPhoneCountryByIso2(DEFAULT_PHONE_COUNTRY_ISO2) ?? PHONE_COUNTRIES[0];
}
