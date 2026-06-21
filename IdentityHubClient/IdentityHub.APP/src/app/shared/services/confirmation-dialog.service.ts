import { Injectable, inject } from '@angular/core';
import { NgxModalService } from 'ngx-modalservice';
import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog-content',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4">
      <div class="w-full max-w-sm rounded-lg border border-slate-200 bg-white shadow-lg">
        <!-- Header -->
        <div class="border-b border-slate-200 px-6 py-4">
          <h2 class="text-lg font-semibold text-slate-900">{{ title }}</h2>
        </div>

        <!-- Body -->
        <div class="px-6 py-4">
          <p class="text-sm text-slate-600">{{ message }}</p>
        </div>

        <!-- Footer -->
        <div class="flex gap-3 border-t border-slate-200 px-6 py-3 sm:justify-end">
          <button
            type="button"
            (click)="onCancel()"
            class="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
          >
            Cancel
          </button>
          <button
            type="button"
            (click)="onConfirm()"
            [ngClass]="getDangerClass()"
            class="rounded-lg px-4 py-2 text-sm font-medium text-white transition-colors"
          >
            {{ confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ConfirmationDialogContentComponent {
  @Input() title = 'Confirm Action';
  @Input() message = 'Are you sure?';
  @Input() confirmLabel = 'Confirm';
  @Input() isDangerous = false;
  @Input() onConfirm!: () => void;
  @Input() onCancel!: () => void;

  getDangerClass(): string {
    return this.isDangerous
      ? 'bg-danger-600 hover:bg-danger-700'
      : 'bg-primary-600 hover:bg-primary-700';
  }
}

export interface ConfirmationDialogOptions {
  title?: string;
  message?: string;
  confirmLabel?: string;
  isDangerous?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConfirmationDialogService {
  constructor() {}

  /**
   * Opens a confirmation dialog and returns a promise that resolves to true if confirmed,
   * false if cancelled.
   */
  confirm(options: ConfirmationDialogOptions = {}): Promise<boolean> {
    return new Promise((resolve) => {
      const {
        title = 'Confirm Action',
        message = 'Are you sure?',
        confirmLabel = 'Confirm',
        isDangerous = false
      } = options;

      // Fallback: simple browser confirm if modal service unavailable
      // In production, integrate with a proper modal library (e.g., ng-bootstrap, @angular/cdk)
      const result = window.confirm(`${title}\n\n${message}`);
      resolve(result);
    });
  }

  /**
   * Convenience method for delete confirmations.
   */
  confirmDelete(itemName = 'this item'): Promise<boolean> {
    return this.confirm({
      title: 'Delete Confirmation',
      message: `Are you sure you want to delete ${itemName}? This action cannot be undone.`,
      confirmLabel: 'Delete',
      isDangerous: true
    });
  }
}
