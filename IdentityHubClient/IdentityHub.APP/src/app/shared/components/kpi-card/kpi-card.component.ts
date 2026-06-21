import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface KpiMetric {
  label: string;
  value: number | string;
  unit?: string;
  trend?: {
    value: number;
    direction: 'up' | 'down' | 'neutral';
    period?: string;
  };
  icon?: string;
  color?: 'primary' | 'success' | 'warning' | 'danger';
}

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [ngClass]="getCardClasses()">
      <!-- Header -->
      <div class="flex items-start justify-between">
        <div>
          <p class="text-sm font-medium text-muted">{{ metric.label }}</p>
          <h3 class="mt-2 text-3xl font-bold text-slate-900">
            {{ metric.value }}
            @if (metric.unit) {
              <span class="text-lg text-muted">{{ metric.unit }}</span>
            }
          </h3>
        </div>
        @if (metric.icon) {
          <div class="text-3xl">{{ metric.icon }}</div>
        }
      </div>

      <!-- Trend -->
      @if (metric.trend) {
        <div class="mt-4 flex items-center gap-2">
          <span [ngClass]="getTrendClasses()">
            @if (metric.trend.direction === 'up') {
              ↑
            } @else if (metric.trend.direction === 'down') {
              ↓
            } @else {
              →
            }
            {{ Math.abs(metric.trend.value) }}%
          </span>
          @if (metric.trend.period) {
            <span class="text-xs text-muted">{{ metric.trend.period }}</span>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
  `]
})
export class KpiCardComponent {
  @Input({ required: true }) metric!: KpiMetric;

  Math = Math;

  getCardClasses(): string {
    const baseClasses = 'rounded-lg border border-slate-200 bg-white p-6 shadow-sm';
    const colorMap: Record<string, string> = {
      primary: 'border-l-4 border-l-primary-500',
      success: 'border-l-4 border-l-success-500',
      warning: 'border-l-4 border-l-warning-500',
      danger: 'border-l-4 border-l-danger-500'
    };

    const colorClass = this.metric.color ? colorMap[this.metric.color] : '';
    return `${baseClasses} ${colorClass}`;
  }

  getTrendClasses(): string {
    const baseClasses = 'inline-flex items-center text-sm font-semibold';

    switch (this.metric.trend?.direction) {
      case 'up':
        return `${baseClasses} text-success-600`;
      case 'down':
        return `${baseClasses} text-danger-600`;
      default:
        return `${baseClasses} text-muted`;
    }
  }
}
