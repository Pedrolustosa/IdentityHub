import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  isLoading = false;
  errorMessage = '';
  successMessage = '';

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
      password: ['', [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[A-Z])(?=.*\d).+$/)]],
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
    if (password.length >= 8) score += 25;
    if (/[A-Z]/.test(password)) score += 25;
    if (/\d/.test(password)) score += 25;
    if (/[^A-Za-z0-9]/.test(password)) score += 25;
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
    return password.length >= 8;
  }

  get hasUppercase(): boolean {
    const password = this.passwordControl?.value ?? '';
    return /[A-Z]/.test(password);
  }

  get hasNumber(): boolean {
    const password = this.passwordControl?.value ?? '';
    return /\d/.test(password);
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

    this.errorMessage = '';
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
            'Account created. Check your inbox to confirm your email before signing in. Redirecting to sign in…';
          this.toastr.success('We sent a confirmation link to your email.', 'Account created');
          this.registerForm.reset({
            fullName: '',
            email: '',
            password: '',
            confirmPassword: '',
            agreeToTerms: false
          });
          setTimeout(
            () =>
              void this.router.navigate(['/login'], {
                queryParams: { email: registeredEmail }
              }),
            2200
          );
        },
        error: (error) => {
          this.errorMessage = error?.error ?? 'Registration failed. Please try again.';
          this.toastr.error(this.errorMessage, 'Registration failed');
        }
      });
  }
}
