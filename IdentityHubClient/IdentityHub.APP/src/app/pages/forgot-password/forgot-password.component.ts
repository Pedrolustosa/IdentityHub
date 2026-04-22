import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../services/auth.service';
import { BrandLogoComponent } from '../../components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BrandLogoComponent],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  submitted = false;

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

    this.authService
      .forgotPassword({ email: this.form.controls.email.value.trim().toLowerCase() })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (message) => {
          this.submitted = true;
          const msg =
            message?.trim() ||
            'If an account exists for this email, you will receive password reset instructions.';
          this.toastr.success(msg, 'Password reset');
        },
        error: () => {
          this.toastr.error('Could not submit request. Try again later.', 'Password reset');
        }
      });
  }
}
