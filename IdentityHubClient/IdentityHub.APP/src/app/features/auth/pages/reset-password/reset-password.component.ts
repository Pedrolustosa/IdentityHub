import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent, LoadErrorBannerComponent],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.css'
})
export class ResetPasswordComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  missingParams = false;
  requestError: UiLoadError | null = null;
  emailFromQuery = '';
  tokenFromQuery = '';
  showPassword = false;
  showConfirmPassword = false;

  readonly form = this.formBuilder.nonNullable.group({
    newPassword: ['', [Validators.required, Validators.minLength(7)]],
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

  get strengthPercentage(): number {
    const password = this.newPasswordControl?.value ?? '';
    let score = 0;
    if (this.hasMinLength) score += 25;
    if (this.hasUppercase) score += 25;
    if (this.hasNumbers) score += 25;
    if (this.hasSpecialChar) score += 25;
    return score;
  }

  get strengthLabel(): string {
    const score = this.strengthPercentage;
    if (score <= 25) return 'Weak';
    if (score <= 50) return 'Fair';
    if (score <= 75) return 'Good';
    return 'Strong';
  }

  get hasMinLength(): boolean {
    const password = this.newPasswordControl?.value ?? '';
    return password.length >= 7 && password.length <= 12;
  }

  get hasUppercase(): boolean {
    const password = this.newPasswordControl?.value ?? '';
    return /[A-Z]/.test(password);
  }

  get hasNumbers(): boolean {
    const password = this.newPasswordControl?.value ?? '';
    return /\d{2,}/.test(password);
  }

  get hasSpecialChar(): boolean {
    const password = this.newPasswordControl?.value ?? '';
    return /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
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
      this.toastr.error('Passwords do not match.', 'Password Reset');
      return;
    }

    this.isLoading = true;
    this.requestError = null;
    this.authService
      .resetPassword({
        email: this.emailFromQuery.toLowerCase(),
        token: this.tokenFromQuery,
        newPassword: this.form.controls.newPassword.value
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Password updated successfully. You can now sign in with your new password.', 'Password Reset');
          void this.router.navigate(['/login'], { queryParams: { email: this.emailFromQuery } });
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.requestError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Password Reset');
        }
      });
  }
}
