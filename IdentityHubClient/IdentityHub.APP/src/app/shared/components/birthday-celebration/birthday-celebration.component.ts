import { Component, computed, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BirthdayGreetingService } from '../../../core/services/birthday-greeting.service';

const CONFETTI_COLORS = [
  '#0ea5e9',
  '#38bdf8',
  '#f59e0b',
  '#fbbf24',
  '#f97316',
  '#22c55e',
  '#ec4899',
  '#a855f7',
  '#ef4444',
  '#14b8a6'
];

@Component({
  selector: 'app-birthday-celebration',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './birthday-celebration.component.html',
  styleUrl: './birthday-celebration.component.css'
})
export class BirthdayCelebrationComponent {
  private readonly birthday = inject(BirthdayGreetingService);

  readonly state = this.birthday.greeting;
  readonly visible = computed(() => this.state().visible);
  readonly displayName = computed(() => this.state().displayName);
  readonly age = computed(() => this.state().age);
  readonly ageLabel = computed(() => {
    const years = this.age();
    if (years == null) {
      return null;
    }
    return `${years}${this.ordinalSuffix(years)}`;
  });

  dontShowAgain = false;

  /** Full-screen confetti rain while the greeting is open. */
  readonly confetti = Array.from({ length: 64 }, (_, i) => {
    const shapeRoll = i % 3;
    const size = 7 + (i % 6) * 2;
    return {
      id: i,
      left: `${((i * 13) + (i % 7) * 3) % 100}%`,
      delay: `${(i % 16) * 0.12}s`,
      duration: `${2.8 + (i % 7) * 0.35}s`,
      color: CONFETTI_COLORS[i % CONFETTI_COLORS.length],
      width: shapeRoll === 1 ? size : size,
      height: shapeRoll === 1 ? size : Math.max(4, Math.round(size * 0.45)),
      shape: shapeRoll === 0 ? 'bday-piece--rect' : shapeRoll === 1 ? 'bday-piece--dot' : 'bday-piece--tri',
      drift: `${(i % 2 === 0 ? 1 : -1) * (18 + (i % 5) * 8)}px`,
      rotate: (i * 29) % 360
    };
  });

  dismiss(): void {
    this.birthday.dismiss({ dontShowAgain: this.dontShowAgain });
    this.dontShowAgain = false;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.visible()) {
      this.dismiss();
    }
  }

  private ordinalSuffix(n: number): string {
    const mod100 = n % 100;
    if (mod100 >= 11 && mod100 <= 13) {
      return 'th';
    }
    switch (n % 10) {
      case 1:
        return 'st';
      case 2:
        return 'nd';
      case 3:
        return 'rd';
      default:
        return 'th';
    }
  }
}
