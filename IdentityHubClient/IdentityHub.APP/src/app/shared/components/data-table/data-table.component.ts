import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TableColumn {
  key: string;
  label: string;
  sortable?: boolean;
  width?: string;
}

export interface TableRow {
  [key: string]: any;
  id?: string | number;
}

export interface PaginationState {
  pageIndex: number;
  pageSize: number;
  total: number;
}

@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="overflow-x-auto rounded-lg border border-slate-200 shadow-sm">
      <table class="w-full border-collapse">
        <!-- Header -->
        <thead>
          <tr class="border-b border-slate-200 bg-slate-50">
            @for (column of columns; track column.key) {
              <th
                class="px-6 py-3 text-left text-xs font-semibold text-slate-900"
                [style.width]="column.width"
              >
                @if (column.sortable) {
                  <button
                    type="button"
                    (click)="toggleSort(column.key)"
                    class="flex items-center gap-2 hover:text-primary-600"
                  >
                    {{ column.label }}
                    @if (sortBy === column.key) {
                      <span>{{ sortDirection === 'asc' ? '↑' : '↓' }}</span>
                    }
                  </button>
                } @else {
                  {{ column.label }}
                }
              </th>
            }
          </tr>
        </thead>

        <!-- Body -->
        <tbody>
          @if (rows && rows.length > 0) {
            @for (row of rows; track row.id || $index; let rowIndex = $index) {
              <tr
                class="border-b border-slate-100 hover:bg-slate-50 transition-colors"
                [class.bg-slate-50]="rowIndex % 2 === 0"
              >
                @for (column of columns; track column.key) {
                  <td class="px-6 py-4 text-sm text-slate-900">
                    {{ formatValue(row[column.key], column.key) }}
                  </td>
                }
              </tr>
            }
          } @else {
            <tr>
              <td [attr.colspan]="columns.length" class="px-6 py-8 text-center text-sm text-muted">
                No data available
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    @if (showPagination && pagination) {
      <div class="flex items-center justify-between border-t border-slate-200 bg-white px-6 py-4">
        <div class="text-sm text-muted">
          Showing {{ (pagination.pageIndex) * pagination.pageSize + 1 }} to
          {{ Math.min((pagination.pageIndex + 1) * pagination.pageSize, pagination.total) }} of
          {{ pagination.total }} results
        </div>
        <div class="flex gap-2">
          <button
            type="button"
            (click)="previousPage()"
            [disabled]="pagination.pageIndex === 0"
            class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            ← Previous
          </button>
          <span class="px-3 py-2 text-sm text-slate-900">
            Page {{ pagination.pageIndex + 1 }} of {{ Math.ceil(pagination.total / pagination.pageSize) }}
          </span>
          <button
            type="button"
            (click)="nextPage()"
            [disabled]="(pagination.pageIndex + 1) * pagination.pageSize >= pagination.total"
            class="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            Next →
          </button>
        </div>
      </div>
    }
  `
})
export class DataTableComponent implements OnInit {
  @Input() columns: TableColumn[] = [];
  @Input() rows: TableRow[] = [];
  @Input() pagination: PaginationState | null = null;
  @Input() showPagination = true;
  @Input() sortable = true;

  @Output() sortChange = new EventEmitter<{ sortBy: string; sortDirection: 'asc' | 'desc' }>();
  @Output() pageChange = new EventEmitter<number>();

  sortBy: string = '';
  sortDirection: 'asc' | 'desc' = 'asc';
  Math = Math;

  ngOnInit(): void {
    if (this.columns.length > 0) {
      this.sortBy = this.columns[0].key;
    }
  }

  toggleSort(columnKey: string): void {
    if (!this.sortable) return;

    if (this.sortBy === columnKey) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortBy = columnKey;
      this.sortDirection = 'asc';
    }

    this.sortChange.emit({
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    });
  }

  formatValue(value: any, key: string): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'boolean') return value ? 'Yes' : 'No';
    if (typeof value === 'object') return JSON.stringify(value);
    return String(value);
  }

  previousPage(): void {
    if (this.pagination && this.pagination.pageIndex > 0) {
      this.pageChange.emit(this.pagination.pageIndex - 1);
    }
  }

  nextPage(): void {
    if (this.pagination) {
      const maxPage = Math.ceil(this.pagination.total / this.pagination.pageSize) - 1;
      if (this.pagination.pageIndex < maxPage) {
        this.pageChange.emit(this.pagination.pageIndex + 1);
      }
    }
  }
}
