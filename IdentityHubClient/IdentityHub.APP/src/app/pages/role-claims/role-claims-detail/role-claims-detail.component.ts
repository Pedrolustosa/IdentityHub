import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { RoleListItem, RolesService } from '../../../services/roles.service';

@Component({
  selector: 'app-role-claims-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './role-claims-detail.component.html',
  styleUrl: './role-claims-detail.component.css'
})
export class RoleClaimsDetailComponent implements OnInit {
  isLoading = true;
  hasError = false;
  roleId = '';
  role: RoleListItem | null = null;
  permissions: string[] = [];

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly rolesService: RolesService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('roleId');
    if (!id) {
      void this.router.navigate(['/app/role-claims']);
      return;
    }
    this.roleId = id;

    forkJoin({
      role: this.rolesService.getRoleById(id),
      permissions: this.rolesService.getRolePermissions(id)
    })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: ({ role, permissions }) => {
          this.role = role;
          this.permissions = permissions ?? [];
        },
        error: () => {
          this.hasError = true;
          this.toastr.error('Could not load role or permissions.', 'Role claims');
        }
      });
  }
}
