import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../../core/services/auth.service';
import { LoadErrorBannerComponent } from '../../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { RoleListItem, RolesService } from '../../../roles.service';

@Component({
  selector: 'app-role-claims-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './role-claims-detail.component.html',
  styleUrl: './role-claims-detail.component.css'
})
export class RoleClaimsDetailComponent implements OnInit {
  isLoading = true;
  loadError: UiLoadError | null = null;
  roleId = '';
  role: RoleListItem | null = null;
  permissions: string[] = [];
  permSearch = '';
  readonly canAssignRolePermissions: boolean;

  get groupedPermissions(): Record<string, string[]> {
    const term = this.permSearch.trim().toLowerCase();
    const list = term ? this.permissions.filter((p) => p.toLowerCase().includes(term)) : this.permissions;
    return list.reduce<Record<string, string[]>>((groups, p) => {
      const domain = p.split('.')[0] ?? 'Other';
      (groups[domain] ??= []).push(p);
      return groups;
    }, {});
  }

  get domainGroups(): string[] {
    return Object.keys(this.groupedPermissions).sort((a, b) => a.localeCompare(b));
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly rolesService: RolesService,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {
    this.canAssignRolePermissions = this.authService.canAssignRolePermissions();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('roleId');
    if (!id) {
      void this.router.navigate(['/app/roles']);
      return;
    }
    this.roleId = id;
    this.loadRoleData();
  }

  loadRoleData(): void {
    this.isLoading = true;
    this.loadError = null;
    this.role = null;
    this.permissions = [];

    forkJoin({
      role: this.rolesService.getRoleById(this.roleId),
      permissions: this.rolesService.getRolePermissions(this.roleId)
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ role, permissions }) => {
          this.role = role;
          this.permissions = permissions ?? [];
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.loadError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Role Permissions');
        }
      });
  }
}
