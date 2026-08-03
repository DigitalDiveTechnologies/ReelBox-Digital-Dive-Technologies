import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../auth/session/session.service';

/**
 * Foundation RoleGuard (PDF §7).
 * Route data:
 *   data: { roles?: string[]; permissions?: string[] }
 * Empty arrays / missing data => allow (authGuard already applied).
 */
export const roleGuard: CanActivateFn = (route) => {
  const session = inject(SessionService);
  const router = inject(Router);

  const roles = (route.data['roles'] as string[] | undefined) ?? [];
  const permissions =
    (route.data['permissions'] as string[] | undefined) ?? [];

  if (roles.length === 0 && permissions.length === 0) {
    return true;
  }

  const roleOk = roles.length === 0 || session.hasAnyRole(roles);
  const permissionOk =
    permissions.length === 0 || session.hasAnyPermission(permissions);

  if (roleOk && permissionOk) {
    return true;
  }

  return router.createUrlTree(['/403']);
};
