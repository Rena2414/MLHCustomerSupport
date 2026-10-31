import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { describeApiError } from '../../../core/api/api-error';
import { HOME_ROUTE_BY_ROLE, RegisterRole } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { PRODUCT_NAME } from '../../../core/brand';

interface RoleOption {
  value: RegisterRole;
  label: string;
  hint: string;
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: '../auth-page.css'
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly productName = PRODUCT_NAME;
  readonly roles: RoleOption[] = [
    { value: 'student', label: 'Student', hint: 'I learn' },
    { value: 'teacher', label: 'Teacher', hint: 'I teach' },
    { value: 'parent', label: 'Parent', hint: 'I follow my child' }
  ];
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly form = inject(NonNullableFormBuilder).group({
    role: ['student' as RegisterRole, Validators.required],
    name: ['', Validators.required],
    age: [10, [Validators.required, Validators.min(5), Validators.max(120)]],
    login: ['', [Validators.required, Validators.minLength(3)]],
    password: ['', [Validators.required, Validators.minLength(4)]]
  });
  readonly selectedRole = toSignal(this.form.controls.role.valueChanges, { initialValue: this.form.controls.role.value });

  submit(): void {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();
    this.auth.register({ ...value, age: Number(value.age) }).subscribe({
      next: user => this.router.navigateByUrl(HOME_ROUTE_BY_ROLE[user.role]),
      error: error => {
        this.submitting.set(false);
        this.error.set(describeApiError(error, 'Could not create the account. Try another login.'));
      }
    });
  }
}
