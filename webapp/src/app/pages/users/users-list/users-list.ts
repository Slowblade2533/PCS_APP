import { DatePipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import {
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  switchMap,
  tap,
} from 'rxjs/operators';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { PagedResult } from '../../../shared/models/pagination.models';
import { UserListItem, UserListQuery } from '../../../shared/models/user.models';
import { UserManagementService } from '../../../shared/services/user-management.service';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DatePipe, PaginationComponent],
  templateUrl: './users-list.html',
})
export class UsersList implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly userService = inject(UserManagementService);

  private readonly refresh$ = new BehaviorSubject<void>(undefined);

  loading = signal(false);
  result = signal<PagedResult<UserListItem> | null>(null);
  togglingId = signal<number | null>(null);

  filterForm = new FormGroup({
    searchText: new FormControl(''),
    selectedIsActive: new FormControl<boolean | undefined>(undefined),
  });

  query: UserListQuery = { page: 1, pageSize: 20 };

  ngOnInit(): void {
    this.filterForm
      .get('searchText')
      ?.valueChanges.pipe(
        debounceTime(350),
        distinctUntilChanged(),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((search) => {
        this.query = { ...this.query, search: search || undefined, page: 1 };
        this.refresh$.next();
      });

    this.filterForm
      .get('selectedIsActive')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((isActive) => {
        this.query = { ...this.query, isActive: isActive === null ? undefined : isActive, page: 1 };
        this.refresh$.next();
      });

    this.refresh$
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(() => {
          const s = this.query.search?.trim() || '';
          if (s.length > 0 && s.length < 3) {
            return of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 1 });
          }
          return this.userService
            .getUsers(this.query)
            .pipe(
              catchError(() =>
                of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 1 }),
              ),
            );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => {
        this.result.set(res);
        this.loading.set(false);
      });
  }

  changePage(page: number): void {
    this.query = { ...this.query, page };
    this.loadUsers();
  }

  hasNextPage(): boolean {
    const total = this.result()?.totalCount ?? 0;
    return this.query.page * this.query.pageSize < total;
  }

  loadUsers(): void {
    this.refresh$.next();
  }

  toggleActive(user: UserListItem): void {
    this.togglingId.set(user.id);
    this.userService
      .toggleUserActive(user.id, !user.isActive)
      .pipe(
        finalize(() => {
          this.togglingId.set(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          const current = this.result();
          if (current) {
            this.result.set({
              ...current,
              items: current.items.map((u) =>
                u.id === user.id ? { ...u, isActive: !u.isActive } : u,
              ),
            });
          }
        },
        error: () => this.loadUsers(),
      });
  }
}
