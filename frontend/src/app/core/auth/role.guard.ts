import { inject } from '@angular/core';
import { CanActivateFn, RedirectFunction, Router } from '@angular/router';
import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

export function roleGuard(...roles: UserRole[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const user = auth.user();
    if (!user) {
      return router.parseUrl('/login');
    }
    return roles.includes(user.role) ? true : router.parseUrl(auth.homeRoute());
  };
}

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isLoggedIn() ? inject(Router).parseUrl(auth.homeRoute()) : true;
};

export const redirectByRole: RedirectFunction = () => inject(AuthService).homeRoute();
