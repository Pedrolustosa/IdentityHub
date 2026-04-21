import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  missingParams = false;
  emailFromQuery = '';
  tokenFromQuery = '';

  readonly form = this.formBuilder.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
    const token = this.route.snapshot.queryParamMap.get('token')?.trim() ?? '';
    if (!email || !token) {
      this.missingParams = true;
      return;
    }
    this.emailFromQuery = email;
    this.tokenFromQuery = token;
  }

  get newPasswordControl() {
    return this.form.get('newPassword');
  }

  get confirmPasswordControl() {
    return this.form.get('confirmPassword');
  }

  passwordsMatch(): boolean {
    return this.form.controls.newPassword.value === this.form.controls.confirmPassword.value;
  }

  submit(): void {
    if (this.missingParams) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.passwordsMatch()) {
      this.toastr.error('Passwords do not match.', 'Reset password');
      return;
    }

    this.isLoading = true;
    this.authService
      .resetPassword({
        email: this.emailFromQuery.toLowerCase(),
        token: this.tokenFromQuery,
        newPassword: this.form.controls.newPassword.value
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Password updated. You can sign in with your new password.', 'Reset password');
          void this.router.navigate(['/login']);
        },
        error: () => {
          this.toastr.error('Reset failed. The link may be invalid or expired.', 'Reset password');
        }
      });
  }
}
