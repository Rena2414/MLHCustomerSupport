import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { UserListItem } from '../../../core/api/api.models';
import { UsersService } from '../../../core/api/users.service';
import { UserRole } from '../../../core/auth/auth.models';
import { SubscriptionPipe } from '../../shared/label.pipes';

interface UsersState {
  loading: boolean;
  error: boolean;
  users: UserListItem[];
}

@Component({
  selector: 'app-users-list',
  imports: [DatePipe, RouterLink, SubscriptionPipe],
  templateUrl: './users-list.component.html',
  styleUrl: './users-list.component.css'
})
export class UsersListComponent {
  private readonly usersService = inject(UsersService);

  readonly roles: (UserRole | null)[] = [null, 'Student', 'Teacher', 'Parent', 'Admin'];
  readonly role = signal<UserRole | null>(null);
  readonly state = toSignal(
    toObservable(this.role).pipe(
      switchMap(role =>
        this.usersService.getAll(role).pipe(
          map(users => ({ loading: false, error: false, users })),
          catchError(() => of({ loading: false, error: true, users: [] })),
          startWith({ loading: true, error: false, users: [] } as UsersState)
        )
      )
    ),
    { initialValue: { loading: true, error: false, users: [] } as UsersState }
  );
}
