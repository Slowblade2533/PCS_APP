import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const permissionGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Get the required permission from the route data
  const requiredPermission = route.data?.['permission'];

  if (!requiredPermission) {
    return true; // No permission required
  }

  if (authService.hasPermission(requiredPermission)) {
    return true;
  }

  // Not authorized
  window.alert('คุณไม่มีสิทธิ์การเข้าถึงข้อมูลส่วนนี้');
  return router.createUrlTree(['/dashboard']);
};
