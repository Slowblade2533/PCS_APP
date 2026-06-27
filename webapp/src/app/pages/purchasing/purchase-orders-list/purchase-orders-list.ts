import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import {
  PurchaseOrderListItem,
  PurchaseOrderSearchParams,
  PurchaseOrderStatus,
} from '../../../shared/models/purchase-orders.models';
import { PurchaseOrdersService } from '../../../shared/services/purchase-orders.service';
import { rxResource } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-purchase-orders-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './purchase-orders-list.html',
})
export class PurchaseOrdersList implements OnInit {
  private poService = inject(PurchaseOrdersService);
  private destroyRef = inject(DestroyRef);

  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  searchTerm = signal<string>('');
  selectedStatus = signal<string>('');
  dateFrom = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`
  );
  dateTo = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-${String(new Date().getDate()).padStart(2, '0')}`
  );

  private searchSubject = new Subject<string>();

  statusOptions: { value: string; label: string }[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'DRAFT', label: 'ฉบับร่าง' },
    { value: 'ORDERED', label: 'สั่งซื้อแล้ว' },
    { value: 'PARTIALLY_RECEIVED', label: 'รับสินค้าบางส่วน' },
    { value: 'RECEIVED', label: 'รับสินค้าครบแล้ว' },
    { value: 'CANCELLED', label: 'ยกเลิก' },
  ];

  ordersResource = rxResource<PagedResult<PurchaseOrderListItem>, PurchaseOrderSearchParams>({
    params: () => ({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      status: this.selectedStatus() as PurchaseOrderStatus | '',
      dateFrom: this.dateFrom(),
      dateTo: this.dateTo(),
    }),
    stream: ({ params }) => this.poService.getAll(params),
  });

  data = computed(
    () =>
      this.ordersResource.value() || {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
      },
  );
  loading = computed(() => this.ordersResource.isLoading());
  error = computed(() => (this.ordersResource.error() ? 'ไม่สามารถดึงข้อมูลรายการได้' : null));

  ngOnInit() {
    this.searchSubject
      .pipe(
        debounceTime(350),
        distinctUntilChanged(),
        tap((term) => {
          this.searchTerm.set(term);
          this.pageNumber.set(1);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  onSearch(event: Event) {
    const target = event.target as HTMLInputElement;
    this.searchSubject.next(target.value);
  }

  onFilterChange() {
    this.pageNumber.set(1);
  }

  onPageChange(newPage: number) {
    this.pageNumber.set(newPage);
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'DRAFT':
        return 'badge-neutral';
      case 'ORDERED':
        return 'badge-info';
      case 'PARTIALLY_RECEIVED':
        return 'badge-warning';
      case 'RECEIVED':
        return 'badge-success';
      case 'CANCELLED':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  getStatusLabel(status: string): string {
    const found = this.statusOptions.find((o) => o.value === status);
    return found ? found.label : status;
  }

  protected readonly Math = Math;
}
