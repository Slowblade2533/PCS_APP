import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, map, Observable, of, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginCredentials, User } from '../models/user.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  currentUser = signal<User | null>(null);
  isInitialized = signal<boolean>(false);

  private csrfTokenFetched = false;
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  checkAuthStatus(): Observable<boolean> {
    return this.http.get<User>(`${this.apiUrl}/me`).pipe(
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

  ensureCsrfToken(): Observable<void> {
    if (this.csrfTokenFetched) return of(void 0);
    return this.http.get<void>(`${this.apiUrl}/csrf-token`).pipe(
      tap(() => (this.csrfTokenFetched = true)),
      map(() => void 0),
    );
  }

  hasPermission(permission: string): boolean {
    const user = this.currentUser();
    if (!user || !user.permissions) return false;
    return user.permissions.includes(permission);
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

  isLoggedIn(): boolean {
    return this.currentUser() !== null;
  }

  login(credentials: LoginCredentials): Observable<User> {
    const payload = {
      email: credentials.email,
      password: credentials.password,
    };

    return this.ensureCsrfToken().pipe(
      switchMap(() => this.http.post<User>(`${this.apiUrl}/login`, payload)),
      tap((user) => {
        this.currentUser.set(user);
        this.isInitialized.set(true);
      }),
    );
  }

  logout(): void {
    this.ensureCsrfToken()
      .pipe(switchMap(() => this.http.post(`${this.apiUrl}/logout`, {})))
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
  }
}
