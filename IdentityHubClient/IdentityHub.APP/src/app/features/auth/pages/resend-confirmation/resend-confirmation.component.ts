import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../../core/services/auth.service';
import { BrandLogoComponent } from '../../../../shared/components/brand-logo/brand-logo.component';
import { UxStateComponent } from '../../../../shared/components/ux-state/ux-state.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../shared/http/ui-load-error';
import { normalizeToastMessage } from '../../../../shared/ui/toast-copy';

@Component({
  selector: 'app-resend-confirmation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent, UxStateComponent],
  templateUrl: './resend-confirmation.component.html',
  styleUrl: './resend-confirmation.component.css'
})
export class ResendConfirmationComponent implements OnInit, OnDestroy {
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
    private readonly route: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly toastr: ToastrService
  ) {}

  get emailControl() {
    return this.form.get('email');
  }

  ngOnInit(): void {
    this.route.queryParams.pipe(take(1)).subscribe((params) => {
      const email = params['email'];
      if (typeof email === 'string' && email.trim()) {
        this.form.patchValue({ email: email.trim() });
      }
    });
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
      .resendConfirmation({ email: this.form.controls.email.value.trim().toLowerCase() })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (body) => {
          this.submitted = true;
          this.cooldownSeconds = 60;
          this.startCooldown();

          const msg = normalizeToastMessage(
            body,
            'If an account exists and is not yet confirmed, we sent a new confirmation email.'
          );
          this.toastr.success(msg, 'Email Confirmation');
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.requestError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Email Confirmation');
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
