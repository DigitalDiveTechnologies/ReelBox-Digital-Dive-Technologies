import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AdminAuthEndpoints } from './models/admin-auth.models';
import { SessionService } from './session/session.service';
import { TokenService } from './session/token.service';

const AUTH_PATHS: readonly string[] = [
  AdminAuthEndpoints.login,
  AdminAuthEndpoints.refresh,
];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokens = inject(TokenService);
  const session = inject(SessionService);
  const router = inject(Router);

  const isAuthCall = AUTH_PATHS.some((path) => req.url.includes(path));
  const accessToken = tokens.getAccessToken();

  const authReq =
    !isAuthCall && accessToken
      ? req.clone({
          setHeaders: {
            Authorization: `Bearer ${accessToken}`,
          },
        })
      : req;

  return next(authReq).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        // 403 must not auto-navigate — pages surface permission errors inline.
        // Route-level forbidden UX is handled by role guards only.
        if (error.status === 401 && !isAuthCall) {
          session.clear();
          void router.navigate(['/auth/login'], {
            queryParams: { reason: 'unauthorized' },
          });
        }
      }
      return throwError(() => error);
    }),
  );
};
