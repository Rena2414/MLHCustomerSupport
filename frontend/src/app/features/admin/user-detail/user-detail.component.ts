import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';
import { UserDetails } from '../../../core/api/api.models';
import { UsersService } from '../../../core/api/users.service';
import { HumanizePipe, SubscriptionPipe } from '../../shared/label.pipes';

interface DetailState {
  loading: boolean;
  user: UserDetails | null;
}

@Component({
  selector: 'app-user-detail',
  imports: [DatePipe, RouterLink, HumanizePipe, SubscriptionPipe],
  templateUrl: './user-detail.component.html',
  styleUrl: './user-detail.component.css'
})
export class UserDetailComponent {
  private readonly usersService = inject(UsersService);

  readonly id = input.required<string>();
  readonly state = toSignal(
    toObservable(this.id).pipe(
      switchMap(id =>
        this.usersService.getById(id).pipe(
          map(user => ({ loading: false, user })),
          catchError(() => of({ loading: false, user: null })),
          startWith({ loading: true, user: null } as DetailState)
        )
      )
    ),
    { initialValue: { loading: true, user: null } as DetailState }
  );
}
