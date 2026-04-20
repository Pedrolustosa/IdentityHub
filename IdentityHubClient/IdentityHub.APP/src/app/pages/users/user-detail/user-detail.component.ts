import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { UserListItem, UsersService } from '../../../services/users.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './user-detail.component.html',
  styleUrl: './user-detail.component.css'
})
export class UserDetailComponent implements OnInit {
  isLoading = true;
  hasError = false;
  user: UserListItem | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly usersService: UsersService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigate(['/app/users']);
      return;
    }

    this.usersService
      .getUserById(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (u) => {
          this.user = u;
        },
        error: (err) => {
          this.hasError = true;
          if (err?.status === 404) {
            this.toastr.warning('User not found.', 'Users');
          } else {
            this.toastr.error('Could not load user.', 'Users');
          }
        }
      });
  }
}
