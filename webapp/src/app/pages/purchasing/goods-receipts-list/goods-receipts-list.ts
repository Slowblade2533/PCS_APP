import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import {
  GoodsReceiptListItem,
  GoodsReceiptSearchParams,
  GoodsReceiptStatus,
} from '../../../shared/models/goods-receipts.models';
import { GoodsReceiptsService } from '../../../shared/services/goods-receipts.service';
import { rxResource } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-goods-receipts-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './goods-receipts-list.html',
})
export class GoodsReceiptsList implements OnInit {
  private receiptService = inject(GoodsReceiptsService);
  private destroyRef = inject(DestroyRef);

  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  searchTerm = signal<string>('');
  selectedStatus = signal<string>('');

  private searchSubject = new Subject<string>();

  statusOptions: { value: string; label: string }[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'PENDING', label: 'รอดำเนินการ' },
    { value: 'COMPLETED', label: 'รับเข้าสต๊อคแล้ว' },
  ];

  receiptsResource = rxResource<PagedResult<GoodsReceiptListItem>, GoodsReceiptSearchParams>({
    params: () => ({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      status: this.selectedStatus() as GoodsReceiptStatus | '',
    }),
    stream: ({ params }) => this.receiptService.getAll(params),
  });

  data = computed(
    () =>
      this.receiptsResource.value() || {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
      },
  );
  loading = computed(() => this.receiptsResource.isLoading());
  error = computed(() => (this.receiptsResource.error() ? 'ไม่สามารถดึงข้อมูลรายการได้' : null));

  ngOnInit() {
    this.searchSubject
      .pipe(
        debounceTime(300),
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
      case 'PENDING':
        return 'badge-warning';
      case 'COMPLETED':
        return 'badge-success';
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
