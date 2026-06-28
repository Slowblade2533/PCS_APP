import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FinancialService } from '../../../shared/services/financial.service';
import { GeneralJournalRow } from '../../../shared/models/financial.models';
import { FinancialSummaryCardsComponent } from '../../../shared/components/financial-summary-cards/financial-summary-cards';

@Component({
  selector: 'app-general-journal',
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, NgClass, FinancialSummaryCardsComponent],
  templateUrl: './general-journal.html',
})
export class GeneralJournal implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  dateFrom = signal<string>(
    new Date(new Date().getFullYear(), new Date().getMonth(), 1).toLocaleDateString('en-CA'),
  );
  dateTo = signal<string>(
    new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).toLocaleDateString('en-CA'),
  );

  rows = signal<(GeneralJournalRow & { isAlternate?: boolean })[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  totalMoneyIn = computed(() => {
    return this.rows()
      .filter((row) => this.isCashAccount(row.accountCode))
      .reduce((sum, row) => sum + (row.debitAmount || 0), 0);
  });

  totalMoneyOut = computed(() => {
    return this.rows()
      .filter((row) => this.isCashAccount(row.accountCode))
      .reduce((sum, row) => sum + (row.creditAmount || 0), 0);
  });

  netBalance = computed(() => {
    return this.totalMoneyIn() - this.totalMoneyOut();
  });

  isCashAccount(code: string): boolean {
    if (!code) return false;
    return code.startsWith('100');
  }

  getTransactionClass(type: string): string {
    if (!type) return '';
    const upperType = type.toUpperCase();

    // Incoming list (รับเข้า - green)
    if (['INVESTMENT', 'SALES', 'TRANSFER_IN', 'RECEIPT'].includes(upperType)) {
      return 'bg-success/5';
    }

    // Outgoing list (จ่ายออก - red)
    if (
      [
        'PURCHASE_GENERAL',
        'PURCHASE_VCB',
        'FREIGHT_VCB',
        'FREIGHT_GENERAL',
        'EXPENSE',
        'GOODS_RECEIPT',
      ].includes(upperType)
    ) {
      return 'bg-error/5';
    }

    return ''; // Other
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    this.error.set(null);
    this.financialService
      .getGeneralJournal(this.dateFrom(), this.dateTo())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          let currentTxId = -1;
          let isAlternate = false;
          const processed = data.map((row) => {
            if (row.transactionId !== currentTxId) {
              currentTxId = row.transactionId;
              isAlternate = !isAlternate;
            }
            return { ...row, isAlternate };
          });
          this.rows.set(processed);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('ไม่สามารถดึงข้อมูลสมุดรายวันทั่วไปได้');
          this.loading.set(false);
        },
      });
  }
}
