import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  readonly loginForm: FormGroup;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly toastr: ToastrService
  ) {
    this.loginForm = this.formBuilder.nonNullable.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [true]
    });
  }

  get emailControl() {
    return this.loginForm.get('email');
  }

  get passwordControl() {
    return this.loginForm.get('password');
  }

  submit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.errorMessage = '';
    this.successMessage = '';
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
          const storage = formValue.rememberMe ? localStorage : sessionStorage;
          const otherStorage = formValue.rememberMe ? sessionStorage : localStorage;

          otherStorage.removeItem('accessToken');
          otherStorage.removeItem('refreshToken');
          storage.setItem('accessToken', response.token);
          storage.setItem('refreshToken', response.refreshToken);

          this.successMessage = 'Login successful.';
          this.toastr.success('You are now signed in.', 'Success');
          void this.router.navigate(['/app/dashboard']);
        },
        error: (error) => {
          this.errorMessage = error?.error ?? 'Login failed. Please check your credentials.';
          this.toastr.error(this.errorMessage, 'Login failed');
        }
      });
  }
}
