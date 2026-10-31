import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, AuthUser, HOME_ROUTE_BY_ROLE, LoginRequest, RegisterRequest } from './auth.models';

const STORAGE_KEY = 'classly.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly session = signal<AuthResponse | null>(readStoredSession());

  readonly token = computed(() => this.session()?.token ?? null);
  readonly user = computed(() => this.session()?.user ?? null);
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly homeRoute = computed(() => {
    const user = this.user();
    return user ? HOME_ROUTE_BY_ROLE[user.role] : '/login';
  });

  login(request: LoginRequest): Observable<AuthUser> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/login`, request)
      .pipe(tap(response => this.store(response)), map(response => response.user));
  }

  register(request: RegisterRequest): Observable<AuthUser> {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/auth/register`, request)
      .pipe(tap(response => this.store(response)), map(response => response.user));
  }

  refreshProfile(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${environment.apiUrl}/me`).pipe(tap(user => this.updateUser(user)));
  }

  updateUser(user: AuthUser): void {
    const current = this.session();
    if (current) {
      this.store({ ...current, user });
    }
  }

  logout(): void {
    this.session.set(null);
    writeStoredSession(null);
    this.router.navigateByUrl('/login');
  }

  private store(response: AuthResponse): void {
    this.session.set(response);
    writeStoredSession(response);
  }
}

function readStoredSession(): AuthResponse | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthResponse) : null;
  } catch {
    return null;
  }
}

function writeStoredSession(session: AuthResponse | null): void {
  try {
    if (session) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } else {
      localStorage.removeItem(STORAGE_KEY);
    }
  } catch {
    return;
  }
}
