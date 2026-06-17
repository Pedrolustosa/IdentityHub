import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent, LoadErrorBannerComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  isLoading = false;
  requestError: UiLoadError | null = null;
  successMessage = '';
  showPassword = false;
  showConfirmPassword = false;
  showTermsModal = false;

  readonly registerForm: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly toastr: ToastrService
  ) {
    this.registerForm = this.formBuilder.nonNullable.group({
      fullName: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(7), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)]],
      confirmPassword: ['', [Validators.required]],
      agreeToTerms: [false, [Validators.requiredTrue]]
    }, {
      validators: [this.passwordsMatchValidator()]
    });
  }

  get fullNameControl() {
    return this.registerForm.get('fullName');
  }

  get emailControl() {
    return this.registerForm.get('email');
  }

  get passwordControl() {
    return this.registerForm.get('password');
  }

  get confirmPasswordControl() {
    return this.registerForm.get('confirmPassword');
  }

  get strengthPercentage(): number {
    const password = this.passwordControl?.value ?? '';
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
    const password = this.passwordControl?.value ?? '';
    return password.length >= 7 && password.length <= 12;
  }

  get hasMaxLength(): boolean {
    const password = this.passwordControl?.value ?? '';
    return password.length <= 12;
  }

  get hasUppercase(): boolean {
    const password = this.passwordControl?.value ?? '';
    return /[A-Z]/.test(password);
  }

  get hasNumbers(): boolean {
    const password = this.passwordControl?.value ?? '';
    return /\d{2,}/.test(password);
  }

  get hasSpecialChar(): boolean {
    const password = this.passwordControl?.value ?? '';
    return /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPasswordVisibility(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  openTermsModal(): void {
    this.showTermsModal = true;
  }

  closeTermsModal(): void {
    this.showTermsModal = false;
  }

  private passwordsMatchValidator(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const password = group.get('password')?.value;
      const confirmPassword = group.get('confirmPassword')?.value;
      if (!password || !confirmPassword) {
        return null;
      }

      return password === confirmPassword ? null : { passwordMismatch: true };
    };
  }

  submit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.requestError = null;
    this.successMessage = '';
    this.isLoading = true;

    const formValue = this.registerForm.getRawValue();
    this.authService
      .register({
        fullName: formValue.fullName.trim(),
        email: formValue.email.trim().toLowerCase(),
        password: formValue.password
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          const registeredEmail = formValue.email.trim().toLowerCase();
          this.successMessage =
            'Account created successfully! Check your email to confirm your account. You can close this window or click the button below to sign in.';
          this.toastr.success('We sent a confirmation link to your email.', 'Registration');
          this.registerForm.reset({
            fullName: '',
            email: '',
            password: '',
            confirmPassword: '',
            agreeToTerms: false
          });
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err, { authForm401AsInvalid: true });
          this.requestError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Registration');
        }
      });
  }
}
