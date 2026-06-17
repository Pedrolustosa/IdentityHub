import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { SessionTokensService } from '../../../../core/services/session-tokens.service';
import { ToastrService } from 'ngx-toastr';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { LoadErrorBannerComponent } from '../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { normalizeToastMessage } from '../../../../shared/ui/toast-copy';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent, LoadErrorBannerComponent],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  isLoading = false;
  successMessage = '';
  requestError: UiLoadError | null = null;
  showPassword = false;
  capsLockActive = false;
  emailRequiresConfirmation = false;

  readonly loginForm: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly sessionTokens: SessionTokensService,
    private readonly router: Router,
    private readonly route: ActivatedRoute,
    private readonly toastr: ToastrService
  ) {
    this.loginForm = this.formBuilder.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(7)]],
      rememberMe: [true]
    });
  }

  ngOnInit(): void {
    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      const email = params['email'];
      if (typeof email === 'string' && email.trim()) {
        this.loginForm.patchValue({ email: email.trim() });
      }
    });
  }

  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onPasswordKeyDown(event: KeyboardEvent): void {
    this.capsLockActive = event.getModifierState('CapsLock');
  }

  submit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.successMessage = '';
    this.requestError = null;
    this.emailRequiresConfirmation = false;
    this.isLoading = true;

    const formValue = this.loginForm.getRawValue();
    this.authService
      .login({
        email: formValue.email.trim().toLowerCase(),
        password: formValue.password
      })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.sessionTokens.saveAccessToken(response.token, formValue.rememberMe);

          this.successMessage = 'Login successful.';
          this.toastr.success('You are now signed in.', 'Authentication');
          void this.router.navigate(['/app/dashboard']);
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err, { authForm401AsInvalid: true });
          this.requestError = mapped;

          // Check if error indicates unconfirmed email
          const errMsg = (err as any)?.error?.message || '';
          this.emailRequiresConfirmation = errMsg.toLowerCase().includes('email') && errMsg.toLowerCase().includes('confirm');

          // Use generic message for security
          const msg = normalizeToastMessage(
            toastMessageForUiLoadError(mapped),
            'Invalid email or password. Please check your credentials and try again.'
          );

          this.toastr.warning(msg, 'Authentication');
        }
      });
  }
}
