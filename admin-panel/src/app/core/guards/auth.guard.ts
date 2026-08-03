import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../auth/session/session.service';

/** Requires a stored access token (profile may still be hydrating). */
export const authGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);

  if (session.hasToken()) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};

/** Blocks authenticated users from visiting login. */
export const guestGuard: CanActivateFn = () => {
  const session = inject(SessionService);
  const router = inject(Router);

  if (!session.hasToken()) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
