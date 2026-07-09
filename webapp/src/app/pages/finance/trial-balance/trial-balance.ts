import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { BehaviorSubject } from 'rxjs';
import { catchError, debounceTime, switchMap, tap } from 'rxjs/operators';
import { TrialBalanceRow } from '../../../shared/models/financial.models';
import { FinancialService } from '../../../shared/services/financial.service';

@Component({
  selector: 'app-trial-balance',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './trial-balance.html',
})
export class TrialBalance implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  data = signal<TrialBalanceRow[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  dateFrom = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-01`
  );
  dateTo = signal<string>(
    `${new Date().getFullYear()}-${String(new Date().getMonth() + 1).padStart(2, '0')}-${String(new Date().getDate()).padStart(2, '0')}`
  );

  private refresh$ = new BehaviorSubject<void>(undefined);

  ngOnInit() {
    this.refresh$
      .pipe(
        debounceTime(300),
        tap(() => {
          this.loading.set(true);
          this.error.set(null);
        }),
        switchMap(() => {
          return this.financialService.getTrialBalance(this.dateFrom(), this.dateTo()).pipe(
            catchError((err) => {
              this.error.set('ไม่สามารถดึงข้อมูลรายงานได้');
              return [];
            }),
          );
        }),
        tap((res: TrialBalanceRow[]) => {
          if (res) {
            this.data.set(res);
          }
          this.loading.set(false);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  onFilterChange() {
    this.refresh$.next();
  }

  getNetDebit(row: TrialBalanceRow): number {
    return row.balance > 0 ? row.balance : 0;
  }

  getNetCredit(row: TrialBalanceRow): number {
    return row.balance < 0 ? -row.balance : 0;
  }

  get totalDebit(): number {
    return this.data().reduce((sum, row) => sum + this.getNetDebit(row), 0);
  }

  get totalCredit(): number {
    return this.data().reduce((sum, row) => sum + this.getNetCredit(row), 0);
  }
}
