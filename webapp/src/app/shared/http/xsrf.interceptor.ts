import { HttpInterceptorFn } from '@angular/common/http';
import { API_ORIGIN } from '../config/api.config';

const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

function readCookie(name: string): string | null {
  const target = `${name}=`;
  const cookie = document.cookie
    .split('; ')
    .find((part) => part.startsWith(target));

  if (!cookie) {
    return null;
  }

  const encoded = cookie.slice(target.length);
  return decodeURIComponent(encoded);
}

export const xsrfInterceptor: HttpInterceptorFn = (req, next) => {
  if (!UNSAFE_METHODS.has(req.method.toUpperCase())) {
    return next(req);
  }

  // Angular built-in XSRF skips absolute URLs, so add header manually for trusted API origin.
  if (!req.url.startsWith(API_ORIGIN)) {
    return next(req);
  }

  const token = readCookie('XSRF-TOKEN');
  if (!token || req.headers.has('X-XSRF-TOKEN')) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        'X-XSRF-TOKEN': token,
      },
    }),
  );
};
