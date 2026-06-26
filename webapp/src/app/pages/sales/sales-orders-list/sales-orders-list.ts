import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import {
  SalesOrderListItem,
  SalesOrderSearchParams,
  SalesOrderStatus,
} from '../../../shared/models/sales-orders.models';
import { SalesOrdersService } from '../../../shared/services/sales-orders.service';

@Component({
  selector: 'app-sales-orders-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './sales-orders-list.html',
})
export class SalesOrdersList {
  private salesService = inject(SalesOrdersService);
  private destroyRef = inject(DestroyRef);

  searchTerm = signal<string>('');
  selectedStatus = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  dataResource = rxResource<PagedResult<SalesOrderListItem>, SalesOrderSearchParams>({
    params: () => ({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      status: this.selectedStatus() as SalesOrderStatus | '',
      dateFrom: this.dateFrom(),
      dateTo: this.dateTo(),
    }),
    stream: ({ params }) => this.salesService.getAll(params),
  });

  data = computed(
    () =>
      this.dataResource.value() || {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
      },
  );
  loading = computed(() => this.dataResource.isLoading());
  error = computed(() => (this.dataResource.error() ? 'ไม่สามารถดึงข้อมูลรายการได้' : null));

  private searchSubject = new Subject<string>();

  statusOptions: { value: string; label: string }[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'DRAFT', label: 'ฉบับร่าง' },
    { value: 'COMPLETED', label: 'สำเร็จ' },
    { value: 'CANCELLED', label: 'ยกเลิก' },
  ];

  constructor() {
    this.searchSubject
      .pipe(debounceTime(350), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchTerm.set(term);
        this.pageNumber.set(1);
      });
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
      case 'COMPLETED':
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

  getPaymentLabel(method: string | undefined): string {
    if (!method) return '-';
    switch (method) {
      case 'CASH':
        return 'เงินสด';
      case 'TRANSFER':
        return 'โอนเงิน';
      case 'CREDIT':
        return 'เครดิต';
      default:
        return method;
    }
  }

  protected readonly Math = Math;
}
