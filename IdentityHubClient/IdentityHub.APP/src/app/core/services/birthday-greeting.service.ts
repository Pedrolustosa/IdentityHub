import { isPlatformBrowser } from '@angular/common';
import { Inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { AuthService, MeResponse } from './auth.service';
import {
  isBirthdayOnDate,
  isDateOfBirthToday,
  parseDateOnly,
  turningAgeFromDateOfBirth
} from '../../shared/utils/birthday.util';
import { catchError, map, of, take } from 'rxjs';

export interface BirthdayGreetingState {
  visible: boolean;
  displayName: string;
  age: number | null;
  userKey: string;
}

@Injectable({ providedIn: 'root' })
export class BirthdayGreetingService {
  private readonly shownKeyPrefix = 'ih.birthday.shown';
  private readonly muteKeyPrefix = 'ih.birthday.mute';

  /** True when the signed-in user's birthday is today (navbar icon). */
  readonly isBirthdayToday = signal(false);

  readonly greeting = signal<BirthdayGreetingState>({
    visible: false,
    displayName: '',
    age: null,
    userKey: ''
  });

  constructor(
    private readonly auth: AuthService,
    @Inject(PLATFORM_ID) private readonly platformId: object
  ) {}

  /** Call once when the authenticated shell loads (after login or session restore). */
  evaluateOnAppEntry(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.auth
      .getMe()
      .pipe(
        take(1),
        catchError(() => of(null))
      )
      .subscribe((me) => {
        if (!me) {
          this.isBirthdayToday.set(false);
          return;
        }

        const birthdayToday = isDateOfBirthToday(me.dateOfBirth);
        this.isBirthdayToday.set(birthdayToday);

        if (!birthdayToday) {
          return;
        }

        const state = this.buildGreeting(me, { respectPrefs: true });
        if (!state) {
          return;
        }
        // Slight delay so the dashboard paints before the celebration appears.
        window.setTimeout(() => this.greeting.set(state), 450);
      });
  }

  /** Re-open congratulations from the navbar (ignores mute / shown-today). */
  openCelebration(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.auth
      .getMe()
      .pipe(
        take(1),
        map((me) => this.buildGreeting(me, { respectPrefs: false })),
        catchError(() => of(null))
      )
      .subscribe((state) => {
        if (state) {
          this.isBirthdayToday.set(true);
          this.greeting.set(state);
        }
      });
  }

  dismiss(options?: { dontShowAgain?: boolean }): void {
    const current = this.greeting();
    if (current.userKey) {
      this.markShownToday(current.userKey);
      if (options?.dontShowAgain) {
        this.mutePermanently(current.userKey);
      }
    }
    this.greeting.update((g) => ({ ...g, visible: false }));
  }

  private buildGreeting(
    me: MeResponse,
    options: { respectPrefs: boolean }
  ): BirthdayGreetingState | null {
    if (!me.dateOfBirth) {
      return null;
    }

    const dob = parseDateOnly(me.dateOfBirth);
    if (!dob) {
      return null;
    }

    const today = new Date();
    if (!isBirthdayOnDate(dob, today)) {
      return null;
    }

    const userKey = (me.email || me.id || 'anon').toLowerCase();
    if (options.respectPrefs && (this.isMuted(userKey) || this.wasShownToday(userKey, today))) {
      return null;
    }

    const full = (me.fullName || me.email || 'there').trim();
    const displayName = full.split(/\s+/)[0] || full;
    const age = turningAgeFromDateOfBirth(me.dateOfBirth, today);

    return {
      visible: true,
      displayName,
      age,
      userKey
    };
  }

  private mutePermanently(userKey: string): void {
    try {
      localStorage.setItem(this.muteKey(userKey), '1');
    } catch {
      /* ignore */
    }
  }

  private isMuted(userKey: string): boolean {
    try {
      return localStorage.getItem(this.muteKey(userKey)) === '1';
    } catch {
      return false;
    }
  }

  private muteKey(userKey: string): string {
    return `${this.muteKeyPrefix}.${userKey}`;
  }

  private markShownToday(userKey: string): void {
    if (!userKey) {
      return;
    }
    try {
      localStorage.setItem(this.shownKey(userKey, new Date()), '1');
    } catch {
      /* ignore */
    }
  }

  private wasShownToday(userKey: string, today: Date): boolean {
    try {
      return localStorage.getItem(this.shownKey(userKey, today)) === '1';
    } catch {
      return false;
    }
  }

  private shownKey(userKey: string, today: Date): string {
    const y = today.getFullYear();
    const m = String(today.getMonth() + 1).padStart(2, '0');
    const d = String(today.getDate()).padStart(2, '0');
    return `${this.shownKeyPrefix}.${userKey}.${y}-${m}-${d}`;
  }
}
