import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, finalize } from 'rxjs/operators';
import { of } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { UxStateComponent } from '../../../../../shared/components/ux-state/ux-state.component';
import { PhoneInputComponent } from '../../../../../shared/components/phone-input/phone-input.component';
import { mapHttpToUiLoadError, toastMessageForUiLoadError, UiLoadError } from '../../../../../shared/http/ui-load-error';
import { UsersService } from '../../../users.service';
import { RoleListItem, RolesService } from '../../../../role-claims/roles.service';
import {
  normalizeOptionalDate,
  normalizeOptionalText,
  optionalDateOfBirthValidator
} from '../../../../../shared/validation/user-contact.validators';

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, UxStateComponent, PhoneInputComponent],
  templateUrl: './user-create.component.html',
  styleUrl: './user-create.component.css'
})
export class UserCreateComponent implements OnInit {
  isSubmitting = false;
  submitError: UiLoadError | null = null;
  availableRoles: RoleListItem[] = [];
  selectedRoles: string[] = [];
  sendInvite = true;
  createdUserId: string | null = null;
  generatedPassword: string | null = null;

  readonly createForm = new FormBuilder().nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', [Validators.maxLength(120)]],
    phoneNumber: [''],
    dateOfBirth: ['', [optionalDateOfBirthValidator()]],
    isActive: [true]
  });

  constructor(
    private readonly usersService: UsersService,
    private readonly rolesService: RolesService,
    private readonly router: Router,
    private readonly toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.rolesService.getRoles()
      .pipe(catchError(() => of<RoleListItem[]>([])))
      .subscribe((roles) => {
        this.availableRoles = roles.filter((r) => r.name);
      });
  }

  get emailControl() {
    return this.createForm.controls.email;
  }

  toggleRole(name: string, checked: boolean): void {
    if (checked) {
      if (!this.selectedRoles.includes(name)) {
        this.selectedRoles = [...this.selectedRoles, name];
      }
    } else {
      this.selectedRoles = this.selectedRoles.filter((r) => r !== name);
    }
  }

  isRoleSelected(name: string): boolean {
    return this.selectedRoles.includes(name);
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

    const { email, fullName, phoneNumber, dateOfBirth, isActive } = this.createForm.getRawValue();
    const contact = {
      fullName: normalizeOptionalText(fullName),
      phoneNumber: normalizeOptionalText(phoneNumber),
      dateOfBirth: normalizeOptionalDate(dateOfBirth)
    };

    if (this.sendInvite) {
      this.generatedPassword = null;
      this.usersService
        .inviteUser({
          email: email.trim().toLowerCase(),
          ...contact,
          isActive,
          roles: this.selectedRoles
        })
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.createdUserId = 'invited';
            this.toastr.success('Invitation sent.', 'Users');
          },
          error: (err: unknown) => {
            const mapped = mapHttpToUiLoadError(err);
            this.submitError = mapped;
            this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
          }
        });
    } else {
      const tempPassword = this.generateTempPassword();
      this.generatedPassword = tempPassword;

      this.usersService
        .createUser({
          email: email.trim().toLowerCase(),
          password: tempPassword,
          ...contact
        })
        .pipe(finalize(() => (this.isSubmitting = false)))
        .subscribe({
          next: () => {
            this.createdUserId = 'created';
            this.toastr.success('User created without invite.', 'Users');
          },
          error: (err: unknown) => {
            const mapped = mapHttpToUiLoadError(err);
            this.submitError = mapped;
            this.toastr.error(toastMessageForUiLoadError(mapped), 'Users');
          }
        });
    }
  }

  resetForAnother(): void {
    this.createdUserId = null;
    this.generatedPassword = null;
    this.sendInvite = true;
    this.selectedRoles = [];
    this.createForm.reset({
      email: '',
      fullName: '',
      phoneNumber: '',
      dateOfBirth: '',
      isActive: true
    });
  }

  goToList(): void {
    void this.router.navigate(['/app/users']);
  }

  copyGeneratedPassword(): void {
    if (!this.generatedPassword || typeof navigator === 'undefined') {
      return;
    }

    navigator.clipboard.writeText(this.generatedPassword).then(() => {
      this.toastr.success('Temporary password copied.', 'Users');
    }).catch(() => {
      this.toastr.warning('Could not copy password. Please copy manually.', 'Users');
    });
  }

  private generateTempPassword(): string {
    const chars = 'ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$';
    return Array.from({ length: 12 }, () => chars[Math.floor(Math.random() * chars.length)]).join('');
  }
}
