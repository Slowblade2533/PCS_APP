import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, Subject } from 'rxjs';
import { PagedResult } from '../../../shared/models/pagination.models';
import { StockItem, StockListQuery } from '../../../shared/models/stock.models';
import { Branch } from '../../../shared/models/user.models';
import { StockService } from '../../../shared/services/stock.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ProductSearchFilter } from '../../../shared/components/product-search-filter/product-search-filter';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, DatePipe, PaginationComponent, ProductSearchFilter],
  templateUrl: './stock-list.html',
  styleUrl: './stock-list.css',
})
export class StockList implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly search$ = new Subject<string>();

  loading = signal(false);
  result = signal<PagedResult<StockItem> | null>(null);
  branches = signal<Branch[]>([]);

  selectedBranchId: number | null = null;
  
  query: StockListQuery = { page: 1, pageSize: 20 };

  private currentReq?: import('rxjs').Subscription;

  ngOnInit(): void {
    this.loadBranches();
    this.loadStock();

    this.search$
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.query = { ...this.query, search: search || undefined, page: 1 };
        this.loadStock();
      });
  }

  loadBranches(): void {
    this.stockService
      .getBranches()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((branches) => {
        this.branches.set(branches);
      });
  }

  loadStock(): void {
    if (this.currentReq) {
      this.currentReq.unsubscribe();
    }
    
    this.loading.set(true);
    this.currentReq = this.stockService
      .getStocks(this.query)
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

  onFilterChange(): void {
    this.query = { ...this.query, page: 1 };
    this.loadStock();
  }

  onBranchChange(branchId: number | null): void {
    this.query = { ...this.query, branchId: branchId ?? undefined, page: 1 };
    this.loadStock();
  }

  changePage(page: number): void {
    this.query = { ...this.query, page };
    this.loadStock();
  }

  hasNextPage(): boolean {
    const total = this.result()?.totalCount ?? 0;
    return this.query.page * this.query.pageSize < total;
  }
}
