import { Component, Input, Output, EventEmitter, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen) {
      <div class="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-black bg-opacity-50 p-4" (click)="onBackdropClick()">
        <div class="w-full max-w-md transform rounded-lg bg-white shadow-xl transition-all" (click)="$event.stopPropagation()">
          <!-- Header -->
          <div class="flex items-center justify-between border-b border-slate-200 px-6 py-4">
            <h2 class="text-lg font-semibold text-slate-900">{{ title }}</h2>
            <button
              type="button"
              (click)="close()"
              class="text-slate-400 hover:text-slate-600 transition-colors"
              aria-label="Close modal"
            >
              <svg class="h-6 w-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Body -->
          <div class="px-6 py-4">
            @if (content) {
              <ng-container *ngTemplateOutlet="content"></ng-container>
            } @else if (body) {
              <p class="text-sm text-slate-600">{{ body }}</p>
            }
          </div>

          <!-- Footer -->
          @if (showFooter && (actions || footer)) {
            <div class="flex gap-3 border-t border-slate-200 px-6 py-3 sm:justify-end">
              @if (actions) {
                @for (action of actions; track action.label) {
                  <button
                    type="button"
                    (click)="handleAction(action)"
                    [ngClass]="getActionClasses(action)"
                    class="rounded-lg px-4 py-2 text-sm font-medium transition-colors"
                  >
                    {{ action.label }}
                  </button>
                }
              } @else if (footer) {
                <ng-container *ngTemplateOutlet="footer"></ng-container>
              }
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class ModalComponent {
  @Input() isOpen = false;
  @Input() title = 'Modal';
  @Input() body: string | null = null;
  @Input() content: TemplateRef<any> | null = null;
  @Input() footer: TemplateRef<any> | null = null;
  @Input() showFooter = true;
  @Input() closeOnBackdrop = true;
  @Input() actions: ModalAction[] = [];

  @Output() closed = new EventEmitter<void>();
  @Output() actionClicked = new EventEmitter<ModalAction>();

  close(): void {
    this.isOpen = false;
    this.closed.emit();
  }

  onBackdropClick(): void {
    if (this.closeOnBackdrop) {
      this.close();
    }
  }

  handleAction(action: ModalAction): void {
    this.actionClicked.emit(action);
    if (action.closeOnClick !== false) {
      this.close();
    }
  }

  getActionClasses(action: ModalAction): string {
    const baseClasses = 'rounded-lg px-4 py-2 text-sm font-medium transition-colors';

    switch (action.variant) {
      case 'primary':
        return `${baseClasses} bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800`;
      case 'danger':
        return `${baseClasses} bg-danger-600 text-white hover:bg-danger-700 active:bg-danger-800`;
      case 'secondary':
      default:
        return `${baseClasses} border border-slate-300 bg-white text-slate-900 hover:bg-slate-50 active:bg-slate-100`;
    }
  }
}

export interface ModalAction {
  label: string;
  variant?: 'primary' | 'danger' | 'secondary';
  closeOnClick?: boolean;
  [key: string]: any;
}
