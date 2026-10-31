import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UserRole } from '../auth/auth.models';
import { UserDetailsDto } from './api.dtos';
import { UserDetails, UserListItem } from './api.models';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/users`;

  getAll(role: UserRole | null): Observable<UserListItem[]> {
    const params = role ? new HttpParams().set('role', role) : undefined;
    return this.http.get<UserListItem[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<UserDetails> {
    return this.http
      .get<UserDetailsDto>(`${this.baseUrl}/${id}`)
      .pipe(map(({ user, ...related }) => ({ ...user, ...related })));
  }
}
