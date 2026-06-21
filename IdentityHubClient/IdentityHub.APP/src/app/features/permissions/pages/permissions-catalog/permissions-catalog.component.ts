import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RolesService } from '../../../role-claims/roles.service';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';

@Component({
  selector: 'app-permissions-catalog',
  standalone: true,
  imports: [CommonModule, UxStateComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Permissions catalog</h1>
        <p class="text-slate-600">Reference list for available permissions and impact review.</p>
      </header>

      <app-ux-state
        [state]="isLoading ? 'loading' : loadError ? 'error' : permissions.length === 0 ? 'empty' : 'loaded'"
        [error]="loadError"
        title="No permissions in catalog"
        description="No permissions were returned by the backend catalog endpoint."
        (retry)="load()"
      >
        <div class="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
          @for (permission of permissions; track permission) {
            <article class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm space-y-2">
              <p class="text-xs uppercase tracking-wide text-slate-500">Code</p>
              <p class="font-mono text-sm text-slate-900 break-all">{{ permission }}</p>
              <p class="text-xs text-slate-500">Module: {{ moduleOf(permission) }}</p>
            </article>
          }
        </div>
      </app-ux-state>
    </section>
  `
})
export class PermissionsCatalogComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  permissions: string[] = [];

  constructor(
    private readonly rolesService: RolesService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.rolesService.getPermissionCatalog()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (permissions) => {
          this.permissions = [...permissions].sort((a, b) => a.localeCompare(b));
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Permissions catalog');
        }
      });
  }

  moduleOf(permission: string): string {
    const [module] = permission.split('.');
    return module || 'General';
  }
}
