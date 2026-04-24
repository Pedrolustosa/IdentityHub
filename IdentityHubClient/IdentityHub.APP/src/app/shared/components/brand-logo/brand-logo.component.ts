import { NgClass } from '@angular/common';
import { Component, Input, booleanAttribute } from '@angular/core';

@Component({
  selector: 'app-brand-logo',
  standalone: true,
  imports: [NgClass],
  templateUrl: './brand-logo.component.html',
  styleUrl: './brand-logo.component.css'
})
export class BrandLogoComponent {
  /** Marketing/auth cards vs app sidebar */
  @Input() variant: 'auth' | 'sidebar' = 'auth';

  /** Sidebar collapsed rail: keep mark readable within narrow width */
  @Input({ transform: booleanAttribute }) collapsed = false;

  /** Full horizontal lockup (`logo_e_texto.png`) vs mark only (`logo.png`) */
  @Input({ transform: booleanAttribute }) wordmark = true;

  get imageSrc(): string {
    if (this.variant === 'sidebar') {
      return '/logo_e_texto.png';
    }
    return this.wordmark ? '/logo_e_texto.png' : '/logo.png';
  }
}
