import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FinancialService } from '../../../shared/services/financial.service';
import { ChartOfAccount, GeneralLedgerRow } from '../../../shared/models/financial.models';

@Component({
  selector: 'app-general-ledger',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule],
  templateUrl: './general-ledger.html',
})
export class GeneralLedger implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  accounts = signal<ChartOfAccount[]>([]);
  selectedAccountId = signal<number | null>(null);

  dateFrom = signal<string>(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1).toLocaleDateString('en-CA'),
  );
  dateTo = signal<string>(
    new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).toLocaleDateString('en-CA'),
  );

  rows = signal<GeneralLedgerRow[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.financialService
      .getAccounts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((accs) => {
        this.accounts.set(accs);
      });
  }

  loadData() {
    const accId = this.selectedAccountId();
    if (!accId) {
      this.error.set('กรุณาเลือกบัญชี');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.financialService
      .getGeneralLedger(accId, this.dateFrom(), this.dateTo())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.rows.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('ไม่สามารถดึงข้อมูลบัญชีแยกประเภทได้');
          this.loading.set(false);
        },
      });
  }
}
