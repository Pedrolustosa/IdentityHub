import { Component, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { normalizeToastMessage } from '../../../../shared/ui/toast-copy';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent, UxStateComponent],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  submitted = false;
  requestError: UiLoadError | null = null;
  cooldownSeconds = 0;
  private cooldownInterval: any;

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]]
  });

  constructor(
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  get emailControl() {
    return this.form.get('email');
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.submitted = false;
    this.requestError = null;

    this.authService
      .forgotPassword({ email: this.form.controls.email.value.trim().toLowerCase() })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (message) => {
          this.submitted = true;
          this.cooldownSeconds = 60;
          this.startCooldown();

          const msg = normalizeToastMessage(
            message,
            'If an account exists for this email, you will receive password reset instructions.'
          );
          this.toastr.success(msg, 'Password Recovery');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.requestError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Password Recovery');
        }
      });
  }

  private startCooldown(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }

    this.cooldownInterval = setInterval(() => {
      this.cooldownSeconds--;
      if (this.cooldownSeconds <= 0) {
        clearInterval(this.cooldownInterval);
        this.cooldownInterval = null;
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }
}
