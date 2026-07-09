import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, of, Subject } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ProductSearchFilter } from '../../../shared/components/product-search-filter/product-search-filter';
import { PagedResult } from '../../../shared/models/pagination.models';
import { StockItem, StockListQuery } from '../../../shared/models/stock.models';
import { StockService } from '../../../shared/services/stock.service';

@Component({
  selector: 'app-stock-list',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    DecimalPipe,
    DatePipe,
    PaginationComponent,
    ProductSearchFilter,
    ImageHoverPreview,
  ],
  templateUrl: './stock-list.html',
})
export class StockList implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly stockService = inject(StockService);

  apiOrigin = environment.apiUrl.replace('/api', '');

  private readonly refresh$ = new BehaviorSubject<void>(undefined);
  private readonly search$ = new Subject<string>();

  loading = signal(false);
  result = signal<PagedResult<StockItem> | null>(null);

  query = signal<StockListQuery>({ page: 1, pageSize: 20, inventoryGroup: 'ForSale' });

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        this.query.update((q) => ({ ...q, search: search || undefined, page: 1 }));
        this.refresh$.next();
      });

    this.refresh$
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(() => {
          const qVal = this.query();
          const s = qVal.search?.trim() || '';
          if (s.length > 0 && s.length < 3) {
            return of({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 1 });
          }
          return this.stockService
            .getStocks(qVal)
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
    this.query.update((q) => ({ ...q, page }));
    this.loadStock();
  }

  hasNextPage(): boolean {
    const total = this.result()?.totalCount ?? 0;
    const qVal = this.query();
    return qVal.page * qVal.pageSize < total;
  }

  loadStock(): void {
    this.refresh$.next();
  }

  onFilterChange(): void {
    this.query.update((q) => ({ ...q, page: 1 }));
    this.loadStock();
  }

  updateProductStatus(status: string): void {
    this.query.update((q) => ({ ...q, productStatus: status || undefined }));
  }

  updateCondition(condition: string): void {
    this.query.update((q) => ({ ...q, condition: condition || undefined }));
  }

  updateInventoryGroup(group: string): void {
    this.query.update((q) => ({ ...q, inventoryGroup: group }));
  }

  onSearchChange(value: string): void {
    this.search$.next(value);
  }
}
