import { Component, Input, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';

export interface FilterField {
  name: string;
  label: string;
  type: 'text' | 'select' | 'date' | 'checkbox';
  placeholder?: string;
  options?: Array<{ label: string; value: any }>;
  value?: any;
}

@Component({
  selector: 'app-filter-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  template: `
    <div class="bg-white px-6 py-4 shadow-sm sm:px-8">
      <form [formGroup]="filterForm" class="space-y-4">
        <!-- Search input -->
        <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div class="flex-1">
            <input
              type="search"
              formControlName="search"
              [placeholder]="searchPlaceholder"
              (input)="onSearch()"
              class="w-full rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>

          <!-- Filter fields -->
          @if (filterFields && filterFields.length > 0) {
            <div class="flex flex-wrap gap-3">
              @for (field of filterFields; track field.name) {
                @switch (field.type) {
                  @case ('text') {
                    <input
                      type="text"
                      [formControlName]="field.name"
                      [placeholder]="field.placeholder || field.label"
                      (change)="onFilterChange()"
                      class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder-slate-400 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
                    />
                  }
                  @case ('select') {
                    <select
                      [formControlName]="field.name"
                      (change)="onFilterChange()"
                      class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
                    >
                      <option value="">{{ field.label }}</option>
                      @for (option of field.options; track option.value) {
                        <option [value]="option.value">{{ option.label }}</option>
                      }
                    </select>
                  }
                  @case ('date') {
                    <input
                      type="date"
                      [formControlName]="field.name"
                      (change)="onFilterChange()"
                      class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
                    />
                  }
                  @case ('checkbox') {
                    <label class="flex items-center gap-2 text-sm">
                      <input
                        type="checkbox"
                        [formControlName]="field.name"
                        (change)="onFilterChange()"
                        class="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                      />
                      <span class="text-slate-700">{{ field.label }}</span>
                    </label>
                  }
                }
              }
            </div>
          }

          <!-- Actions -->
          <div class="flex gap-2">
            @if (showReset && hasActiveFilters()) {
              <button
                type="button"
                (click)="reset()"
                class="inline-flex items-center rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 transition-colors"
              >
                🔄 Reset
              </button>
            }
            <button
              type="button"
              (click)="apply()"
              class="inline-flex items-center rounded-lg bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 transition-colors"
            >
              🔍 Search
            </button>
          </div>
        </div>
      </form>
    </div>
  `
})
export class FilterBarComponent implements OnInit {
  @Input() filterFields: FilterField[] = [];
  @Input() searchPlaceholder = 'Search...';
  @Input() showReset = true;
  @Output() filterChange = new EventEmitter<any>();
  @Output() search = new EventEmitter<string>();

  filterForm: FormGroup;

  constructor(private fb: FormBuilder) {
    this.filterForm = this.fb.group({
      search: ['']
    });
  }

  ngOnInit(): void {
    // Add dynamic form controls
    if (this.filterFields) {
      this.filterFields.forEach(field => {
        this.filterForm.addControl(field.name, this.fb.control(field.value || ''));
      });
    }
  }

  onSearch(): void {
    const searchValue = this.filterForm.get('search')?.value;
    this.search.emit(searchValue);
  }

  onFilterChange(): void {
    this.filterChange.emit(this.filterForm.value);
  }

  apply(): void {
    this.filterChange.emit(this.filterForm.value);
  }

  reset(): void {
    this.filterForm.reset();
    this.filterChange.emit(this.filterForm.value);
  }

  hasActiveFilters(): boolean {
    const value = this.filterForm.value;
    return Object.values(value).some(v => v !== null && v !== '' && v !== undefined);
  }
}
