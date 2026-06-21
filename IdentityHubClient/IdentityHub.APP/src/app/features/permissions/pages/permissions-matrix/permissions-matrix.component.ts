import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RolesService } from '../../../role-claims/roles.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-permissions-matrix',
  standalone: true,
  imports: [CommonModule, LoadErrorBannerComponent],
  template: `
    <section class="space-y-5">
      <header class="rounded-2xl border border-slate-200/80 bg-white px-5 py-4 shadow-sm">
        <h1 class="text-2xl font-semibold tracking-tight text-slate-900">Permissions matrix</h1>
        <p class="text-slate-600">Quick view of role versus permission coverage.</p>
      </header>

      @if (isLoading) {
        <div class="rounded-xl border bg-white p-6 shadow-sm">
          <div class="h-8 w-64 animate-pulse rounded bg-slate-100"></div>
        </div>
      } @else if (loadError) {
        <div class="rounded-xl border bg-white p-4 shadow-sm">
          <app-load-error-banner [error]="loadError" (retry)="load()" />
        </div>
      } @else {
        <div class="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
          <table class="min-w-[760px] w-full text-sm">
            <thead>
              <tr class="bg-slate-50 text-slate-500 uppercase tracking-wide text-xs">
                <th class="text-left px-3 py-3 font-semibold">Permission</th>
                @for (role of roleNames; track role) {
                  <th class="text-center px-3 py-3 font-semibold">{{ role }}</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (perm of permissionCatalog; track perm) {
                <tr class="border-t border-slate-100">
                  <td class="px-3 py-2 font-mono text-xs text-slate-800">{{ perm }}</td>
                  @for (role of roleNames; track role) {
                    <td class="px-3 py-2 text-center">
                      <span class="inline-flex h-5 w-5 items-center justify-center rounded-full text-xs font-semibold"
                        [ngClass]="hasPermission(role, perm) ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-400'">
                        {{ hasPermission(role, perm) ? '✓' : '-' }}
                      </span>
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class PermissionsMatrixComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  roleNames: string[] = [];
  permissionCatalog: string[] = [];
  rolePermissions: Record<string, Set<string>> = {};

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

    this.rolesService.getRoles().pipe(
      switchMap((roles) => {
        this.roleNames = roles.map((r) => r.name ?? 'Unnamed');
        const permissionCalls$ = forkJoin(
          roles.map((role) =>
            this.rolesService.getRolePermissions(role.id).pipe(catchError(() => of<string[]>([])))
          )
        );

        return forkJoin({
          catalog: this.rolesService.getPermissionCatalog(),
          byRole: permissionCalls$,
          roles: of(roles)
        });
      }),
      finalize(() => (this.isLoading = false))
    ).subscribe({
      next: ({ catalog, byRole, roles }) => {
        this.permissionCatalog = [...catalog].sort((a, b) => a.localeCompare(b));
        this.rolePermissions = {};
        roles.forEach((role, index) => {
          const key = role.name ?? 'Unnamed';
          this.rolePermissions[key] = new Set(byRole[index] ?? []);
        });
      },
      error: (err: unknown) => {
        const mapped = mapHttpToUiLoadError(err);
        this.loadError = mapped;
        this.toastr.error(toastMessageForUiLoadError(mapped), 'Permissions matrix');
      }
    });
  }

  hasPermission(roleName: string, permission: string): boolean {
    return this.rolePermissions[roleName]?.has(permission) ?? false;
  }
}
