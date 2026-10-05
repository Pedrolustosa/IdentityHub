import { CommonModule, isPlatformBrowser } from '@angular/common';
import {
  Component,
  ElementRef,
  HostListener,
  Inject,
  Input,
  OnDestroy,
  PLATFORM_ID,
  ViewChild,
  forwardRef
} from '@angular/core';
import {
  ControlValueAccessor,
  FormsModule,
  NG_VALIDATORS,
  NG_VALUE_ACCESSOR,
  ValidationErrors,
  Validator
} from '@angular/forms';
import {
  DEFAULT_PHONE_COUNTRY_ISO2,
  PHONE_COUNTRIES,
  PhoneCountry,
  PhoneCountryFlagAsset,
  phoneCountryFlagAsset
} from '../../phone/phone-countries';
import {
  digitsOnly,
  formatNationalNumber,
  isCompleteNationalNumber,
  maxNationalDigits,
  nationalPlaceholder,
  parseStoredPhoneNumber,
  toE164
} from '../../phone/phone-number.utils';

@Component({
  selector: 'app-phone-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './phone-input.component.html',
  styleUrl: './phone-input.component.css',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    },
    {
      provide: NG_VALIDATORS,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    }
  ]
})
export class PhoneInputComponent implements ControlValueAccessor, Validator, OnDestroy {
  @Input() inputId = 'phone-input';
  @Input() placeholder = '';
  @Input() defaultCountryIso2 = DEFAULT_PHONE_COUNTRY_ISO2;

  @ViewChild('searchInput') private searchInput?: ElementRef<HTMLInputElement>;
  @ViewChild('nationalInput') private nationalInput?: ElementRef<HTMLInputElement>;
  @ViewChild('countryTrigger') private countryTrigger?: ElementRef<HTMLButtonElement>;

  readonly countries = PHONE_COUNTRIES;

  selectedCountry: PhoneCountry = parseStoredPhoneNumber('', this.defaultCountryIso2).country;
  nationalDisplay = '';
  countryMenuOpen = false;
  countryFilter = '';
  disabled = false;
  menuStyle: Record<string, string> = {};

  private nationalDigits = '';
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;
  private readonly isBrowser: boolean;
  private readonly onCapturedScroll = (): void => this.onViewportChange();

  constructor(
    private readonly host: ElementRef<HTMLElement>,
    @Inject(PLATFORM_ID) platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
    if (this.isBrowser) {
      document.addEventListener('scroll', this.onCapturedScroll, true);
    }
  }

  ngOnDestroy(): void {
    if (this.isBrowser) {
      document.removeEventListener('scroll', this.onCapturedScroll, true);
    }
  }

  get flagAsset(): PhoneCountryFlagAsset {
    return phoneCountryFlagAsset(this.selectedCountry.iso2);
  }

  get dialCodeLabel(): string {
    return `+${this.selectedCountry.dialCode}`;
  }

  get inputPlaceholder(): string {
    return this.placeholder || nationalPlaceholder(this.selectedCountry);
  }

  get filteredCountries(): PhoneCountry[] {
    const query = this.countryFilter.trim().toLowerCase();
    if (!query) {
      return [...this.countries];
    }

    return this.countries.filter((country) => {
      const haystack =
        `${country.name} ${country.iso2} +${country.dialCode} ${country.dialCode}`.toLowerCase();
      return haystack.includes(query);
    });
  }

  get filteredCountryOptions(): { country: PhoneCountry; flag: PhoneCountryFlagAsset }[] {
    return this.filteredCountries.map((country) => ({
      country,
      flag: phoneCountryFlagAsset(country.iso2)
    }));
  }

  get countryTriggerLabel(): string {
    return `${this.selectedCountry.name} ${this.dialCodeLabel}`;
  }

  get countryCountLabel(): string {
    return `${this.countries.length} countries`;
  }

  writeValue(value: string | null): void {
    const parsed = parseStoredPhoneNumber(value, this.defaultCountryIso2);
    this.selectedCountry = parsed.country;
    this.nationalDigits = parsed.nationalDigits;
    this.nationalDisplay = formatNationalNumber(this.selectedCountry, this.nationalDigits);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  validate(): ValidationErrors | null {
    if (!this.nationalDigits) {
      return null;
    }

    if (!isCompleteNationalNumber(this.selectedCountry, this.nationalDigits)) {
      return { phoneNumberIncomplete: true };
    }

    const e164 = toE164(this.selectedCountry.dialCode, this.nationalDigits);
    if (e164.length > 32) {
      return { phoneNumberInvalid: true };
    }

    return null;
  }

  toggleCountryMenu(): void {
    if (this.disabled) {
      return;
    }

    this.countryMenuOpen = !this.countryMenuOpen;
    if (this.countryMenuOpen) {
      this.countryFilter = '';
      this.repositionMenu();
      queueMicrotask(() => this.searchInput?.nativeElement.focus());
    }
  }

  selectCountry(country: PhoneCountry): void {
    this.selectedCountry = country;
    this.countryMenuOpen = false;
    this.countryFilter = '';

    const maxDigits = maxNationalDigits(country);
    if (this.nationalDigits.length > maxDigits) {
      this.nationalDigits = this.nationalDigits.slice(0, maxDigits);
    }

    this.nationalDisplay = formatNationalNumber(this.selectedCountry, this.nationalDigits);
    this.emitValue();
    queueMicrotask(() => this.nationalInput?.nativeElement.focus());
  }

  onNationalInput(raw: string): void {
    let nextDigits = digitsOnly(raw);
    nextDigits = this.stripLeadingCountryDialCode(nextDigits);
    nextDigits = nextDigits.slice(0, maxNationalDigits(this.selectedCountry));
    this.nationalDigits = nextDigits;
    this.nationalDisplay = formatNationalNumber(this.selectedCountry, nextDigits);
    this.emitValue();
  }

  onBlur(): void {
    this.onTouched();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.countryMenuOpen) {
      return;
    }

    const target = event.target as Node | null;
    if (target && !this.host.nativeElement.contains(target)) {
      this.countryMenuOpen = false;
    }
  }

  @HostListener('window:resize')
  onViewportChange(): void {
    if (this.countryMenuOpen) {
      this.repositionMenu();
    }
  }

  private repositionMenu(): void {
    const trigger = this.countryTrigger?.nativeElement ?? this.host.nativeElement;
    const rect = trigger.getBoundingClientRect();
    const menuWidth = Math.min(Math.max(rect.width, 300), 360);
    const left = Math.min(rect.left, window.innerWidth - menuWidth - 8);
    const spaceBelow = window.innerHeight - rect.bottom - 8;
    const preferBelow = spaceBelow >= 220;
    const maxHeight = Math.min(360, preferBelow ? spaceBelow : rect.top - 8);

    this.menuStyle = {
      position: 'fixed',
      left: `${Math.max(8, left)}px`,
      width: `${menuWidth}px`,
      maxHeight: `${Math.max(180, maxHeight)}px`,
      zIndex: '80',
      ...(preferBelow
        ? { top: `${rect.bottom + 6}px`, bottom: 'auto' }
        : { bottom: `${window.innerHeight - rect.top + 6}px`, top: 'auto' })
    };
  }

  private stripLeadingCountryDialCode(digits: string): string {
    const dial = this.selectedCountry.dialCode;
    if (!digits.startsWith(dial)) {
      return digits;
    }

    const withoutDial = digits.slice(dial.length);
    if (withoutDial.length >= 4 && withoutDial.length <= maxNationalDigits(this.selectedCountry)) {
      return withoutDial;
    }

    return digits;
  }

  private emitValue(): void {
    this.onChange(toE164(this.selectedCountry.dialCode, this.nationalDigits));
  }
}
