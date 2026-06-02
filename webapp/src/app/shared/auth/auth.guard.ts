import { inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { CanActivateFn, Router } from '@angular/router';
import { filter, first, map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const redirectedToLogin = () => {
    return router.createUrlTree(['/login'], {
      queryParams: {
        returnUrl: state.url,
      },
    });
  };

  if (authService.isInitialized()) {
    return authService.isLoggedIn() ? true : redirectedToLogin();
  }

  return toObservable(authService.isInitialized).pipe(
    filter((initialized) => initialized === true),
    first(),
    map(() => {
      return authService.isLoggedIn() ? true : redirectedToLogin();
    }),
  );
};
