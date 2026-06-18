import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { catchError, debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { FinancialTransaction, FinancialTransactionSearchParams, TransactionType } from '../../../shared/models/financial.models';
import { PagedResult } from '../../../shared/models/pagination.models';
import { FinancialService } from '../../../shared/services/financial.service';

@Component({
  selector: 'app-transactions-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './transactions-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransactionsList implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  apiOrigin = environment.apiUrl;

  data = signal<PagedResult<FinancialTransaction>>({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  searchTerm = signal<string>('');
  selectedType = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');

  private searchSubject = new Subject<string>();
  private refresh$ = new BehaviorSubject<void>(undefined);

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
          const params: FinancialTransactionSearchParams = {
            pageNumber: this.data().pageNumber,
            pageSize: this.data().pageSize,
            searchTerm: this.searchTerm(),
            transactionType: this.selectedType(),
            dateFrom: this.dateFrom(),
            dateTo: this.dateTo()
          };
          return this.financialService.getTransactions(params).pipe(
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

  getTypeBadgeClass(type: string): string {
    switch (type) {
      case 'INVESTMENT': return 'badge-info';
      case 'SALES': return 'badge-success';
      case 'TRANSFER_IN':
      case 'RECEIPT': return 'badge-secondary text-white';
      case 'PURCHASE_GENERAL':
      case 'PURCHASE_VCB': return 'badge-warning';
      case 'FREIGHT_VCB':
      case 'FREIGHT_GENERAL': return 'badge-warning';
      case 'EXPENSE': return 'badge-error';
      case 'STOCK_LOSS':
      case 'SCRAP': return 'badge-neutral';
      default: return 'badge-ghost';
    }
  }

  getTypeLabel(type: string): string {
    const found = this.typeOptions.find((o) => o.value === type);
    return found ? found.label : type;
  }

  protected readonly Math = Math;
}
