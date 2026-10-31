import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { describeApiError } from '../../../core/api/api-error';
import { HOME_ROUTE_BY_ROLE } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { PRODUCT_NAME } from '../../../core/brand';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: '../auth-page.css'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly productName = PRODUCT_NAME;
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly form = inject(NonNullableFormBuilder).group({
    login: ['', Validators.required],
    password: ['', Validators.required]
  });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    this.auth.login(this.form.getRawValue()).subscribe({
      next: user => this.router.navigateByUrl(HOME_ROUTE_BY_ROLE[user.role]),
      error: error => {
        this.submitting.set(false);
        this.error.set(describeApiError(error, 'Wrong login or password.'));
      }
    });
  }
}
