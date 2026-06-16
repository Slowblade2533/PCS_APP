import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LoginCredentials } from '../../shared/models/user.models';
import { AuthService } from '../../shared/services/auth.service';
import { SweetAlertService } from '../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
})
export class Login implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private swal = inject(SweetAlertService);
  private destroyRef = inject(DestroyRef);

  loginForm: FormGroup;
  isPasswordVisible = false;
  isLoading = signal(false);
  returnUrl = '/dashboard';

  constructor(private fb: FormBuilder) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
    });
  }

  ngOnInit(): void {
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
  }

  togglePasswordVisibility() {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  onSubmit() {
    if (this.loginForm.valid) {
      this.isLoading.set(true);
      const formValue = this.loginForm.getRawValue();
      const credentials: LoginCredentials = {
        email: String(formValue.email ?? ''),
        password: String(formValue.password ?? ''),
        rememberMe: Boolean(formValue.rememberMe),
      };

      this.authService
        .login(credentials)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.isLoading.set(false);
            this.onLoginSuccess();
          },
          error: (err) => {
            this.isLoading.set(false);
            this.swal.error(err.error?.message || 'อีเมลหรือรหัสผ่านไม่ถูกต้อง');
          },
        });
    }
  }

  onLoginSuccess() {
    this.router.navigateByUrl(this.returnUrl);
  }
}
