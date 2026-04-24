import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { RoleListItem, RolesService } from '../../roles.service';

export interface RoleWithPermissionMeta {
  role: RoleListItem;
  permissionCount: number | null;
}

@Component({
  selector: 'app-role-claims',
  standalone: true,
  imports: [CommonModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './role-claims.component.html',
  styleUrl: './role-claims.component.css'
})
export class RoleClaimsComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  rows: RoleWithPermissionMeta[] = [];
  readonly canAssignRolePermissions: boolean;

  constructor(
    private readonly rolesService: RolesService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.canAssignRolePermissions = this.authService.canAssignRolePermissions();
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.loadError = null;

    this.rolesService
      .getRoles()
      .pipe(
        switchMap((roles) => {
          if (!roles.length) {
            return of<RoleWithPermissionMeta[]>([]);
          }
          return forkJoin(
            roles.map((role) =>
              this.rolesService.getRolePermissions(role.id).pipe(
                map((permissions) => ({
                  role,
                  permissionCount: permissions.length
                })),
                catchError(() => of<RoleWithPermissionMeta>({ role, permissionCount: null }))
              )
            )
          );
        }),
        finalize(() => (this.isLoading = false))
      )
      .subscribe({
        next: (rows) => {
          this.rows = rows;
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Role claims');
        }
      });
  }
}
