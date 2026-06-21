import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface Permission {
  name: string;
  icon?: string;
  removable?: boolean;
}

@Component({
  selector: 'app-permission-chip',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="inline-flex flex-wrap gap-2">
      @for (permission of permissions; track permission.name) {
        <div class="inline-flex items-center gap-2 rounded-full bg-primary-100 px-3 py-1.5 text-sm font-medium text-primary-800">
          @if (permission.icon) {
            <span class="text-base">{{ permission.icon }}</span>
          }
          <span>{{ permission.name }}</span>
          @if (permission.removable !== false) {
            <button
              type="button"
              (click)="removePermission(permission)"
              class="ml-1 inline-flex items-center justify-center rounded-full hover:bg-primary-200 transition-colors"
              [attr.aria-label]="'Remove ' + permission.name"
            >
              <svg class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          }
        </div>
      }
    </div>
  `
})
export class PermissionChipComponent {
  @Input() permissions: Permission[] = [];
  @Input() removable = true;
  @Output() permissionRemoved = new EventEmitter<Permission>();

  removePermission(permission: Permission): void {
    this.permissionRemoved.emit(permission);
    this.permissions = this.permissions.filter(p => p.name !== permission.name);
  }
}
