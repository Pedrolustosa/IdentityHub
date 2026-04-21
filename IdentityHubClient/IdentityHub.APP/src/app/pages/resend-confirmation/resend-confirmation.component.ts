import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, take } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-resend-confirmation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './resend-confirmation.component.html',
  styleUrl: './resend-confirmation.component.css'
})
export class ResendConfirmationComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);

  isLoading = false;
  submitted = false;

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

    this.authService
      .resendConfirmation({ email: this.form.controls.email.value.trim().toLowerCase() })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (body) => {
          this.submitted = true;
          const msg =
            body?.trim() ||
            'If an account exists and is not yet confirmed, we sent a new confirmation email.';
          this.toastr.success(msg, 'Confirmation email');
        },
        error: () => {
          this.toastr.error('Could not send the email. Try again later.', 'Confirmation email');
        }
      });
  }
}
