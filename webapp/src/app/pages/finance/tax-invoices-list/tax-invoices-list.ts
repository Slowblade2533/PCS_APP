import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import { TaxInvoice, TaxInvoiceSearchParams } from '../../../shared/models/financial.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { rxResource } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-tax-invoices-list',
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe],
  templateUrl: './tax-invoices-list.html',
})
export class TaxInvoicesList implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  pageNumber = signal<number>(1);
  pageSize = signal<number>(20);

  searchTerm = signal<string>('');
  selectedTaxType = signal<string>('');
  dateFrom = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`
  );
  dateTo = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-${String(new Date().getDate()).padStart(2, '0')}`
  );

  private searchSubject = new Subject<string>();

  invoicesResource = rxResource<PagedResult<TaxInvoice>, TaxInvoiceSearchParams>({
    params: () => ({
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: this.searchTerm(),
      taxType: this.selectedTaxType() as any,
      dateFrom: this.dateFrom(),
      dateTo: this.dateTo(),
    }),
    stream: ({ params }) => this.financialService.getTaxInvoices(params),
  });

  data = computed(
    () =>
      this.invoicesResource.value() || {
        items: [],
        totalCount: 0,
        pageNumber: 1,
        pageSize: 20,
        totalPages: 0,
      },
  );
  loading = computed(() => this.invoicesResource.isLoading());
  error = computed(() =>
    this.invoicesResource.error() ? 'ไม่สามารถดึงข้อมูลใบกำกับภาษีได้' : null,
  );

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

  protected readonly Math = Math;
}
