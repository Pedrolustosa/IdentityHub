import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';

export interface DateRange {
  startDate: string | null;
  endDate: string | null;
}

@Component({
  selector: 'app-date-range-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="flex flex-col gap-4 rounded-lg border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-end sm:gap-3">
      <div class="flex-1">
        <label for="start-date" class="block text-xs font-medium text-slate-700 mb-1">
          From
        </label>
        <input
          id="start-date"
          type="date"
          formControlName="startDate"
          (change)="onRangeChange()"
          class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        />
      </div>

      <div class="flex-1">
        <label for="end-date" class="block text-xs font-medium text-slate-700 mb-1">
          To
        </label>
        <input
          id="end-date"
          type="date"
          formControlName="endDate"
          (change)="onRangeChange()"
          class="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        />
      </div>

      <!-- Preset buttons -->
      @if (showPresets) {
        <div class="flex flex-wrap gap-2 sm:flex-col">
          @for (preset of presets; track preset.label) {
            <button
              type="button"
              (click)="applyPreset(preset)"
              class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50 transition-colors"
            >
              {{ preset.label }}
            </button>
          }
        </div>
      }

      <!-- Actions -->
      <div class="flex gap-2">
        @if (hasActiveRange()) {
          <button
            type="button"
            (click)="reset()"
            class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
          >
            Reset
          </button>
        }
        <button
          type="button"
          (click)="apply()"
          class="rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 transition-colors"
        >
          Apply
        </button>
      </div>
    </div>
  `
})
export class DateRangePickerComponent implements OnInit {
  @Input() showPresets = true;
  @Output() rangeChange = new EventEmitter<DateRange>();

  form: FormGroup;

  presets = [
    {
      label: 'Today',
      getValue: () => {
        const today = new Date();
        return {
          startDate: this.formatDate(today),
          endDate: this.formatDate(today)
        };
      }
    },
    {
      label: 'Yesterday',
      getValue: () => {
        const yesterday = new Date();
        yesterday.setDate(yesterday.getDate() - 1);
        return {
          startDate: this.formatDate(yesterday),
          endDate: this.formatDate(yesterday)
        };
      }
    },
    {
      label: 'Last 7 days',
      getValue: () => {
        const now = new Date();
        const start = new Date();
        start.setDate(start.getDate() - 7);
        return {
          startDate: this.formatDate(start),
          endDate: this.formatDate(now)
        };
      }
    },
    {
      label: 'Last 30 days',
      getValue: () => {
        const now = new Date();
        const start = new Date();
        start.setDate(start.getDate() - 30);
        return {
          startDate: this.formatDate(start),
          endDate: this.formatDate(now)
        };
      }
    },
    {
      label: 'This month',
      getValue: () => {
        const now = new Date();
        const start = new Date(now.getFullYear(), now.getMonth(), 1);
        return {
          startDate: this.formatDate(start),
          endDate: this.formatDate(now)
        };
      }
    }
  ];

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      startDate: [''],
      endDate: ['']
    });
  }

  ngOnInit(): void {
    // Form initialized
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onRangeChange(): void {
    this.rangeChange.emit(this.form.value);
  }

  applyPreset(preset: any): void {
    const range = preset.getValue();
    this.form.patchValue(range);
    this.rangeChange.emit(range);
  }

  apply(): void {
    this.rangeChange.emit(this.form.value);
  }

  reset(): void {
    this.form.reset();
    this.rangeChange.emit({ startDate: null, endDate: null });
  }

  hasActiveRange(): boolean {
    const { startDate, endDate } = this.form.value;
    return !!startDate || !!endDate;
  }
}
