import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { UserListItem, UsersService } from '../../../services/users.service';

@Component({
  selector: 'app-user-delete',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './user-delete.component.html',
  styleUrl: './user-delete.component.css'
})
export class UserDeleteComponent implements OnInit {
  isLoading = true;
  loadError = false;
  isDeleting = false;
  userId = '';
  user: UserListItem | null = null;
  confirmChecked = false;

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
    this.userId = id;

    this.usersService
      .getUserById(id)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (u) => {
          this.user = u;
        },
        error: () => {
          this.loadError = true;
          this.toastr.error('Could not load user.', 'Users');
        }
      });
  }

  cancel(): void {
    void this.router.navigate(['/app/users', this.userId]);
  }

  deleteUser(): void {
    if (!this.confirmChecked || !this.userId || this.isDeleting) {
      return;
    }

    this.isDeleting = true;
    this.usersService
      .deleteUser(this.userId)
      .pipe(finalize(() => (this.isDeleting = false)))
      .subscribe({
        next: () => {
          this.toastr.success('User deleted.', 'Users');
          void this.router.navigate(['/app/users']);
        },
        error: () => {
          this.toastr.error('Could not delete user.', 'Users');
        }
      });
  }
}
