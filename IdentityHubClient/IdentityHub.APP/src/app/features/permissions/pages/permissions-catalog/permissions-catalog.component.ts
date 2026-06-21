import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RolesService } from '../../../role-claims/roles.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-permissions-catalog',
  standalone: true,
  imports: [CommonModule, LoadErrorBannerComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Permissions catalog</h1>
        <p class="text-slate-600">Reference list for available permissions and impact review.</p>
      </header>

      @if (isLoading) {
        <div class="rounded-xl border bg-white p-6 shadow-sm">
          <div class="h-8 w-56 animate-pulse rounded bg-slate-100"></div>
        </div>
      } @else if (loadError) {
        <div class="rounded-xl border bg-white p-4 shadow-sm">
          <app-load-error-banner [error]="loadError" (retry)="load()" />
        </div>
      } @else {
        <div class="grid grid-cols-1 gap-3 md:grid-cols-2 lg:grid-cols-3">
          @for (permission of permissions; track permission) {
            <article class="rounded-xl border border-slate-200 bg-white p-4 shadow-sm space-y-2">
              <p class="text-xs uppercase tracking-wide text-slate-500">Code</p>
              <p class="font-mono text-sm text-slate-900 break-all">{{ permission }}</p>
              <p class="text-xs text-slate-500">Module: {{ moduleOf(permission) }}</p>
            </article>
          }
        </div>
      }
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
