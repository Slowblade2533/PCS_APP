import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 403) {
        window.alert('คุณไม่มีสิทธิ์การเข้าถึงข้อมูลส่วนนี้ หรือไม่ได้รับสิทธิ์ในการทำรายการ');
        // Optionally redirect to dashboard if they are on a forbidden page
        router.navigate(['/dashboard']);
      }
      return throwError(() => error);
    })
  );
};
