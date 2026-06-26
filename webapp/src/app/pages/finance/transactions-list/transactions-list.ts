import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import {
  FinancialTransaction,
  FinancialTransactionSearchParams,
  TransactionType,
} from '../../../shared/models/financial.models';
import { PagedResult } from '../../../shared/models/pagination.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { rxResource } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-transactions-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './transactions-list.html',
})
export class TransactionsList implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  apiOrigin = environment.apiUrl;

  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  searchTerm = signal<string>('');
  selectedType = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');

  private searchSubject = new Subject<string>();

  typeOptions: { value: string; label: string }[] = [
    { value: '', label: 'ทั้งหมด' },
    { value: 'INVESTMENT', label: 'รับเงินลงทุน' },
    { value: 'SALES', label: 'รายได้จากการขาย' },
    { value: 'PURCHASE_GENERAL', label: 'ซื้อสินค้าทั่วไป' },
    { value: 'PURCHASE_VCB', label: 'สั่งซื้อ VCANBUY' },
    { value: 'FREIGHT_VCB', label: 'ค่าขนส่ง VCANBUY' },
    { value: 'FREIGHT_GENERAL', label: 'ค่าขนส่งทั่วไป' },
    { value: 'EXPENSE', label: 'ค่าใช้จ่าย' },
    { value: 'STOCK_LOSS', label: 'สินค้าสูญหาย' },
    { value: 'SCRAP', label: 'ตัดจำหน่ายทิ้ง' },
    { value: 'TRANSFER_IN', label: 'รับโอนเงิน' },
    { value: 'RECEIPT', label: 'รับเงินเข้า' },
  ];

  transactionsResource = rxResource<
    PagedResult<FinancialTransaction>,
    FinancialTransactionSearchParams
  >({
    params: () => ({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      transactionType: this.selectedType(),
      dateFrom: this.dateFrom(),
      dateTo: this.dateTo(),
    }),
    stream: ({ params }) => this.financialService.getTransactions(params),
  });

  data = computed(
    () =>
      this.transactionsResource.value() || {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
      },
  );
  loading = computed(() => this.transactionsResource.isLoading());
  error = computed(() =>
    this.transactionsResource.error() ? 'ไม่สามารถดึงข้อมูลรายการได้' : null,
  );

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

  getTypeBadgeClass(type: string): string {
    switch (type) {
      case 'INVESTMENT':
        return 'badge-info';
      case 'SALES':
        return 'badge-success';
      case 'TRANSFER_IN':
      case 'RECEIPT':
        return 'badge-secondary text-white';
      case 'PURCHASE_GENERAL':
      case 'PURCHASE_VCB':
        return 'badge-warning';
      case 'FREIGHT_VCB':
      case 'FREIGHT_GENERAL':
        return 'badge-warning';
      case 'EXPENSE':
        return 'badge-error';
      case 'STOCK_LOSS':
      case 'SCRAP':
        return 'badge-neutral';
      default:
        return 'badge-ghost';
    }
  }

  getTypeLabel(type: string): string {
    const found = this.typeOptions.find((o) => o.value === type);
    return found ? found.label : type;
  }

  protected readonly Math = Math;
}
