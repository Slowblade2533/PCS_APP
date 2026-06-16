import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { SweetAlertService } from '../services/sweet-alert.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const swal = inject(SweetAlertService);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 0) {
        // Network error or server unreachable
        swal.error('ไม่สามารถเชื่อมต่อ server ได้ กรุณาลองใหม่อีกครั้ง หรือติดต่อฝ่าย IT');
      } else if (error.status === 401) {
        // Session expired or cookie invalidated — clear local state and redirect to login
        authService.currentUser.set(null);
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url },
        });
      } else if (error.status === 403) {
        swal
          .error('คุณไม่มีสิทธิ์การเข้าถึงข้อมูลส่วนนี้ หรือไม่ได้รับสิทธิ์ในการทำรายการ')
          .then(() => {
            router.navigate(['/dashboard']);
          });
      }
      return throwError(() => error);
    }),
  );
};
