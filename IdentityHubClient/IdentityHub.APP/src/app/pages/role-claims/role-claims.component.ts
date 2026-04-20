import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, finalize, map, switchMap } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RoleListItem, RolesService } from '../../services/roles.service';

export interface RoleWithPermissionMeta {
  role: RoleListItem;
  permissionCount: number | null;
}

@Component({
  selector: 'app-role-claims',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './role-claims.component.html',
  styleUrl: './role-claims.component.css'
})
export class RoleClaimsComponent implements OnInit {
  isLoading = true;
  hasError = false;
  rows: RoleWithPermissionMeta[] = [];

  constructor(
    private readonly rolesService: RolesService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.hasError = false;

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
        error: () => {
          this.hasError = true;
          this.toastr.error('Failed to load roles.', 'Role claims');
        }
      });
  }
}
