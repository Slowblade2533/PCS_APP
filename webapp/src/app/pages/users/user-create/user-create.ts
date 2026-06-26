import {
  Component,
  DestroyRef,
  HostListener,
  inject,
  OnInit,
  signal,
  effect,
  untracked,
  computed,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of, forkJoin, Observable } from 'rxjs';
import { finalize, catchError } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import {
  Branch,
  Permission,
  Role,
  UserCreateRequest,
  UserDetail,
  UserPermissionAssignmentRequest,
  UserUpdateRequest,
} from '../../../shared/models/user.models';
import { UserManagementService } from '../../../shared/services/user-management.service';

interface PermissionGroup {
  moduleName: string;
  permissions: Permission[];
}

@Component({
  selector: 'app-user-create',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './user-create.html',
})
export class UserCreate implements OnInit, HasUnsavedChanges {
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserManagementService);

  errorMsg = signal('');
  initLoading = signal(true);
  isEditMode = signal(false);
  submitting = signal(false);

  branches = signal<Branch[]>([]);
  permissionGroups = signal<PermissionGroup[]>([]);
  permissions = signal<Permission[]>([]);
  roles = signal<Role[]>([]);

  form!: FormGroup;
  userId = signal<number | null>(null);

  get permissionIds(): FormArray {
    return this.form.get('permissionIds') as FormArray;
  }

  masterDataResource = rxResource({
    params: () => this.userId(),
    stream: ({ params }) => {
      // Return forkJoin with an object instead of array to easily infer types
      return forkJoin({
        roles: this.userService.getRoles(),
        branches: this.userService.getBranches(),
        permissions: this.userService.getPermissions(),
        user: params ? this.userService.getUserById(params) : of(null),
      }).pipe(
        catchError((err) => {
          console.error(err);
          return of(null);
        }),
      );
    },
  });

  constructor() {
    effect(() => {
      const results = this.masterDataResource.value();
      if (results) {
        untracked(() => {
          this.roles.set(results.roles);
          this.branches.set(results.branches);
          this.permissions.set(results.permissions);
          this.groupPermissions(results.permissions);

          if (results.user) {
            this.patchForm(results.user);
          }
          this.initLoading.set(false);
        });
      } else if (results === null && !this.masterDataResource.isLoading()) {
        untracked(() => {
          this.errorMsg.set('โหลดข้อมูลไม่สำเร็จ');
          this.initLoading.set(false);
        });
      }
    });
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.userId.set(+idParam);
      this.isEditMode.set(true);
    }

    this.buildForm();
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.submitting();
  }

  isInvalid(controlName: string): boolean {
    const ctrl = this.form.get(controlName);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  isPermissionChecked(permissionId: number): boolean {
    const pIds = this.form.get('permissionIds')?.value as number[];
    return pIds ? pIds.includes(permissionId) : false;
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    const { firstName, lastName, username, email, password, isActive, roleId, permissionIds } =
      this.form.value;

    const permissionPayload: UserPermissionAssignmentRequest[] = (permissionIds as number[]).map(
      (pId) => ({
        permissionId: pId,
      }),
    );

    this.submitting.set(true);
    this.errorMsg.set('');

    const request$: Observable<any> = this.isEditMode()
      ? this.userService.updateUser(this.userId()!, {
          email,
          firstName,
          lastName,
          isActive,
          roleId,
          permissionAssignments: permissionPayload,
        } as UserUpdateRequest)
      : this.userService.createUser({
          username,
          email,
          password,
          firstName,
          lastName,
          isActive,
          roleId,
          permissionAssignments: permissionPayload,
        } as UserCreateRequest);

    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.submitting.set(false);
        }),
      )
      .subscribe({
        next: () => this.router.navigate(['/users']),
        error: (err) => {
          this.errorMsg.set(err.error?.message ?? 'เกิดข้อผิดพลาด กรุณาลองใหม่');
        },
      });
  }

  togglePermission(permissionId: number): void {
    const ctrl = this.form.get('permissionIds');
    let pIds = [...((ctrl?.value as number[]) || [])];

    if (pIds.includes(permissionId)) {
      pIds = pIds.filter((id) => id !== permissionId);
    } else {
      pIds.push(permissionId);
    }

    ctrl?.setValue(pIds);
  }

  private buildForm(): void {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      username: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
      password: this.isEditMode() ? [''] : ['', [Validators.required, Validators.minLength(8)]],
      isActive: [true],
      roleId: [null, Validators.required],
      permissionIds: [[]],
    });
  }

  private groupPermissions(perms: Permission[]): void {
    const groups: Record<string, Permission[]> = {};
    for (const p of perms) {
      const moduleName = p.permissionCode.split(':')[0].toUpperCase();
      if (!groups[moduleName]) {
        groups[moduleName] = [];
      }
      groups[moduleName].push(p);
    }

    const result: PermissionGroup[] = Object.keys(groups).map((key) => ({
      moduleName: key,
      permissions: groups[key],
    }));

    this.permissionGroups.set(result);
  }

  private patchForm(user: UserDetail): void {
    this.form.patchValue({
      firstName: user.firstName,
      lastName: user.lastName,
      username: user.username,
      email: user.email,
      isActive: user.isActive,
      roleId: user.roleId,
      permissionIds: user.permissions?.map((p) => p.permissionId) || [],
    });
  }
}
