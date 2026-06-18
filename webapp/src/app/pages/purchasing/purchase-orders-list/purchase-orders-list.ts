import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import { PurchaseOrderListItem, PurchaseOrderSearchParams, PurchaseOrderStatus } from '../../../shared/models/procurement.models';
import { PurchaseOrderService } from '../../../shared/services/procurement.service';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './purchase-orders-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseOrdersList implements OnInit {
  private poService = inject(PurchaseOrderService);
  private destroyRef = inject(DestroyRef);

  data = signal<PagedResult<PurchaseOrderListItem>>({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  searchTerm = signal<string>('');
  selectedStatus = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');

  private searchSubject = new Subject<string>();
  private refresh$ = new BehaviorSubject<void>(undefined);

  statusOptions: { value: string; label: string }[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'DRAFT', label: 'ฉบับร่าง' },
    { value: 'ORDERED', label: 'สั่งซื้อแล้ว' },
    { value: 'PARTIALLY_RECEIVED', label: 'รับสินค้าบางส่วน' },
    { value: 'RECEIVED', label: 'รับสินค้าครบแล้ว' },
    { value: 'CANCELLED', label: 'ยกเลิก' },
  ];

  ngOnInit() {
    this.searchSubject
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        tap((term) => {
          this.searchTerm.set(term);
          this.data.update((d) => ({ ...d, pageNumber: 1 }));
          this.refresh$.next();
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();

    this.refresh$
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(() => {
          const params: PurchaseOrderSearchParams = {
            pageNumber: this.data().pageNumber,
            pageSize: this.data().pageSize,
            searchTerm: this.searchTerm(),
            status: this.selectedStatus() as PurchaseOrderStatus | '',
            dateFrom: this.dateFrom(),
            dateTo: this.dateTo()
          };
          return this.poService.getAll(params).pipe(
            catchError((err) => {
              this.error.set('ไม่สามารถดึงข้อมูลรายการได้');
              return [];
            })
          );
        }),
        tap((res: any) => {
          if (res && res.items) {
            this.data.set(res);
          }
          this.loading.set(false);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();
  }

  onSearch(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  onFilterChange() {
    this.data.update((d) => ({ ...d, pageNumber: 1 }));
    this.refresh$.next();
  }

  onPageChange(newPage: number) {
    this.data.update((d) => ({ ...d, pageNumber: newPage }));
    this.refresh$.next();
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'DRAFT': return 'badge-neutral';
      case 'ORDERED': return 'badge-info';
      case 'PARTIALLY_RECEIVED': return 'badge-warning';
      case 'RECEIVED': return 'badge-success';
      case 'CANCELLED': return 'badge-error';
      default: return 'badge-ghost';
    }
  }

  getStatusLabel(status: string): string {
    const found = this.statusOptions.find((o) => o.value === status);
    return found ? found.label : status;
  }

  protected readonly Math = Math;
}
