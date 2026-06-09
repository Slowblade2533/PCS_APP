import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin, Observable } from 'rxjs';
import {
  Branch,
  Role,
  Permission,
  UserCreateRequest,
  UserDetail,
  UserUpdateRequest,
  UserPermissionAssignmentRequest,
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
  styleUrl: './user-create.css',
})
export class UserCreate implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserManagementService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  initLoading = signal(true);
  submitting = signal(false);
  isEditMode = signal(false);
  errorMsg = signal('');

  roles = signal<Role[]>([]);
  branches = signal<Branch[]>([]);
  permissions = signal<Permission[]>([]);
  permissionGroups = signal<PermissionGroup[]>([]);

  form!: FormGroup;
  private userId: number | null = null;

  get permissionIds(): FormArray {
    return this.form.get('permissionIds') as FormArray;
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.userId = idParam ? +idParam : null;
    this.isEditMode.set(!!this.userId);

    this.buildForm();
    this.loadMasterData();
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

  private loadMasterData(): void {
    const requests$: Observable<any[]> = this.userId
      ? forkJoin([
          this.userService.getRoles(),
          this.userService.getBranches(),
          this.userService.getPermissions(),
          this.userService.getUserById(this.userId),
        ])
      : forkJoin([
          this.userService.getRoles(),
          this.userService.getBranches(),
          this.userService.getPermissions(),
        ]);

    requests$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.initLoading.set(false);
        }),
      )
      .subscribe({
        next: (results) => {
          const roles = results[0] as Role[];
          const branches = results[1] as Branch[];
          const permissions = results[2] as Permission[];
          this.roles.set(roles);
          this.branches.set(branches);
          this.permissions.set(permissions);
          this.groupPermissions(permissions);

          if (results.length === 4) {
            const user = results[3] as UserDetail;
            this.patchForm(user);
          }
        },
        error: () => this.errorMsg.set('โหลดข้อมูลไม่สำเร็จ'),
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
    
    const result: PermissionGroup[] = Object.keys(groups).map(key => ({
      moduleName: key,
      permissions: groups[key]
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
      permissionIds: user.permissions?.map(p => p.permissionId) || []
    });
  }

  isInvalid(controlName: string): boolean {
    const ctrl = this.form.get(controlName);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  isPermissionChecked(permissionId: number): boolean {
      const pIds = this.form.get('permissionIds')?.value as number[];
      return pIds ? pIds.includes(permissionId) : false;
  }

  togglePermission(permissionId: number): void {
      const ctrl = this.form.get('permissionIds');
      let pIds = [...(ctrl?.value as number[] || [])];
      
      if (pIds.includes(permissionId)) {
          pIds = pIds.filter(id => id !== permissionId);
      } else {
          pIds.push(permissionId);
      }
      
      ctrl?.setValue(pIds);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      return;
    }

    const { firstName, lastName, username, email, password, isActive, roleId, permissionIds } =
      this.form.value;

    const permissionPayload: UserPermissionAssignmentRequest[] = (permissionIds as number[]).map(pId => ({
        permissionId: pId
    }));

    this.submitting.set(true);
    this.errorMsg.set('');

    const request$: Observable<any> = this.isEditMode()
      ? this.userService.updateUser(this.userId!, {
          email,
          firstName,
          lastName,
          isActive,
          roleId,
          permissionAssignments: permissionPayload
        } as UserUpdateRequest)
      : this.userService.createUser({
          username,
          email,
          password,
          firstName,
          lastName,
          isActive,
          roleId,
          permissionAssignments: permissionPayload
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
}
