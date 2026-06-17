import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { LoadErrorBannerComponent } from '../../../../../shared/components/load-error-banner/load-error-banner.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { UsersService } from '../../../users.service';

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, LoadErrorBannerComponent],
  templateUrl: './user-create.component.html',
  styleUrl: './user-create.component.css'
})
export class UserCreateComponent {
  isSubmitting = false;
  submitError: UiLoadError | null = null;

  readonly createForm = new FormBuilder().nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', [Validators.maxLength(120)]]
  });

  constructor(
    private readonly usersService: UsersService,
    private readonly router: Router,
    private readonly toastr: ToastrService
  ) {}

  get emailControl() {
    return this.createForm.controls.email;
  }

  submit(): void {
    if (this.isSubmitting) {
      return;
    }

    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submitError = null;

    const { email, fullName } = this.createForm.getRawValue();

    this.usersService
      .inviteUser({
        email: email.trim().toLowerCase(),
        fullName: fullName.trim() || null
      })
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: () => {
          this.toastr.success('Invitation sent. The user can now set a password from the email link.', 'Users');
          void this.router.navigate(['/app/users']);
        },
        error: (err: unknown) => {
          const mapped = mapHttpToUiLoadError(err);
          this.submitError = mapped;
          this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
        }
      });
  }
}
