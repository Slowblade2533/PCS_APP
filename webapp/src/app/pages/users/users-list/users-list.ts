import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';
import { PagedResult } from '../../../shared/models/pagination.models';
import { UserListItem, UserListQuery } from '../../../shared/models/user.models';
import { UserManagementService } from '../../../shared/services/user-management.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RouterLink, DatePipe, PaginationComponent],
  templateUrl: './users-list.html',
  styleUrl: './users-list.css',
})
export class UsersList implements OnInit {
  private readonly userService = inject(UserManagementService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly search$ = new Subject<string>();

  loading = signal(false);
  togglingId = signal<number | null>(null);
  result = signal<PagedResult<UserListItem> | null>(null);

  searchText = '';
  selectedIsActive: boolean | undefined = undefined;

  query: UserListQuery = { page: 1, pageSize: 20 };

  private currentReq?: import('rxjs').Subscription;

  ngOnInit(): void {
    this.loadUsers();

    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.query = { ...this.query, search: search || undefined, page: 1 };
        this.loadUsers();
      });
  }

  loadUsers(): void {
    if (this.currentReq) {
      this.currentReq.unsubscribe();
    }

    this.loading.set(true);
    this.currentReq = this.userService
      .getUsers(this.query)
      .pipe(
        finalize(() => {
          this.loading.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((res) => this.result.set(res));
  }

  onSearchChange(value: string): void {
    this.search$.next(value);
  }

  onStatusChange(isActive: boolean | undefined): void {
    this.query = { ...this.query, isActive, page: 1 };
    this.loadUsers();
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

  changePage(page: number): void {
    this.query = { ...this.query, page };
    this.loadUsers();
  }

  hasNextPage(): boolean {
    const total = this.result()?.totalCount ?? 0;
    return this.query.page * this.query.pageSize < total;
  }
}
