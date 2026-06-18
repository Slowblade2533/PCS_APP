import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject, Subject } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { PagedResult } from '../../../shared/models/pagination.models';
import { TaxInvoice, TaxInvoiceSearchParams } from '../../../shared/models/financial.models';
import { FinancialService } from '../../../shared/services/financial.service';

@Component({
  selector: 'app-tax-invoices-list',
  standalone: true,
  imports: [FormsModule, DecimalPipe, DatePipe],
  templateUrl: './tax-invoices-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaxInvoicesList implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  data = signal<PagedResult<TaxInvoice>>({ items: [], totalCount: 0, pageNumber: 1, pageSize: 20, totalPages: 0 });
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  searchTerm = signal<string>('');
  selectedTaxType = signal<string>('');
  dateFrom = signal<string>('');
  dateTo = signal<string>('');

  private searchSubject = new Subject<string>();
  private refresh$ = new BehaviorSubject<void>(undefined);

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
          const params: TaxInvoiceSearchParams = {
            pageNumber: this.data().pageNumber,
            pageSize: this.data().pageSize,
            searchTerm: this.searchTerm(),
            taxType: this.selectedTaxType() as any,
            dateFrom: this.dateFrom(),
            dateTo: this.dateTo()
          };
          return this.financialService.getTaxInvoices(params).pipe(
            catchError((err) => {
              this.error.set('ไม่สามารถดึงข้อมูลใบกำกับภาษีได้');
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

  protected readonly Math = Math;
}
