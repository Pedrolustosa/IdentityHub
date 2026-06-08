import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UiLoadError, uiLoadErrorShowsRetry } from '../../http/ui-load-error';

@Component({
  selector: 'app-load-error-banner',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './load-error-banner.component.html',
  styleUrl: './load-error-banner.component.css'
})
export class LoadErrorBannerComponent {
  @Input({ required: true }) error!: UiLoadError;
  @Output() readonly retry = new EventEmitter<void>();

  /** Optional link when `error.kind === 'not_found'` (e.g. back to list). */
  @Input() notFoundBackLink: string | any[] | null = null;
  @Input() notFoundBackLabel = 'Go Back';

  showRetry(): boolean {
    return uiLoadErrorShowsRetry(this.error);
  }

  emitRetry(): void {
    this.retry.emit();
  }
}
