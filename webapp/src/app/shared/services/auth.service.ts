import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, switchMap, tap } from 'rxjs';
import { API_BASE_URL } from '../config/api.config';

export interface User {
  id: string;
  email: string;
  name: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
  rememberMe?: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly apiUrl = `${API_BASE_URL}/auth`;
  private readonly httpOptions = { withCredentials: true };

  currentUser = signal<User | null>(null);
  isInitialized = signal<boolean>(false);

  isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  initializeAuth(): Observable<boolean> {
    return this.ensureCsrfToken().pipe(
      switchMap(() => this.checkAuthStatus()),
      catchError(() => {
        this.currentUser.set(null);
        this.isInitialized.set(true);
        return of(false);
      }),
    );
  }

  ensureCsrfToken(): Observable<void> {
    return this.http
      .get<void>(`${this.apiUrl}/csrf-token`, this.httpOptions)
      .pipe(map(() => void 0));
  }

  login(credentials: LoginCredentials): Observable<User> {
    const payload = {
      email: credentials.email,
      password: credentials.password,
    };

    return this.ensureCsrfToken().pipe(
      switchMap(() => this.http.post<User>(`${this.apiUrl}/login`, payload, this.httpOptions)),
      tap((user) => {
        this.currentUser.set(user);
        this.isInitialized.set(true);
      }),
    );
  }

  checkAuthStatus(): Observable<boolean> {
    return this.http.get<User>(`${this.apiUrl}/me`, this.httpOptions).pipe(
      map((user) => {
        this.currentUser.set(user);
        this.isInitialized.set(true);
        return true;
      }),
      catchError(() => {
        this.currentUser.set(null);
        this.isInitialized.set(true);
        return of(false);
      }),
    );
  }

  logout(): void {
    this.ensureCsrfToken()
      .pipe(switchMap(() => this.http.post(`${this.apiUrl}/logout`, {}, this.httpOptions)))
      .subscribe({
        next: () => {
          this.currentUser.set(null);
          this.router.navigate(['/login']);
        },
        error: () => {
          this.currentUser.set(null);
          this.router.navigate(['/login']);
        },
      });
    /*
    this.http
      .post(`${this.apiUrl}/logout`, {}, this.httpOptions)
      .pipe(switchMap(() => this.ensureCsrfToken()))
      .subscribe({
        next: () => {
          this.currentUser.set(null);
          this.router.navigate(['/login']);
        },
        error: () => {
          this.currentUser.set(null);
          this.router.navigate(['/login']);
        },
      });
      */
  }
}
