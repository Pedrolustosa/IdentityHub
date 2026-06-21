import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

type StatusType = 'active' | 'inactive' | 'open' | 'closed' | 'success' | 'error' | 'warning' | 'pending';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [ngClass]="badgeClasses()">{{ label }}</span>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `]
})
export class StatusBadgeComponent {
  @Input({ required: true }) status!: StatusType;
  @Input() label = this.formatLabel(this.status);

  private formatLabel(status: StatusType): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
  }

  badgeClasses(): string {
    const baseClasses = 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold transition-colors';

    const statusClasses: Record<StatusType, string> = {
      active: 'bg-success-100 text-success-800',
      inactive: 'bg-surface-100 text-surface-700',
      open: 'bg-primary-100 text-primary-800',
      closed: 'bg-surface-100 text-surface-700',
      success: 'bg-success-100 text-success-800',
      error: 'bg-danger-100 text-danger-800',
      warning: 'bg-warning-100 text-warning-800',
      pending: 'bg-muted-lighter text-muted'
    };

    return `${baseClasses} ${statusClasses[this.status]}`;
  }
}
