import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FinancialService } from '../../../shared/services/financial.service';
import { ChartOfAccount } from '../../../shared/models/financial.models';

@Component({
  selector: 'app-chart-of-accounts',
  standalone: true,
  templateUrl: './chart-of-accounts.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChartOfAccountsList implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  accounts = signal<ChartOfAccount[]>([]);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loading.set(true);
    this.financialService.getAccounts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.accounts.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('ไม่สามารถดึงข้อมูลผังบัญชีได้');
          this.loading.set(false);
        }
      });
  }

  getAccountTypeLabel(type: string): string {
    switch (type) {
      case 'ASSET': return 'สินทรัพย์ (Asset)';
      case 'LIABILITY': return 'หนี้สิน (Liability)';
      case 'EQUITY': return 'ส่วนของเจ้าของ (Equity)';
      case 'REVENUE': return 'รายได้ (Revenue)';
      case 'EXPENSE': return 'ค่าใช้จ่าย (Expense)';
      default: return type;
    }
  }

  getAccountTypeClass(type: string): string {
    switch (type) {
      case 'ASSET': return 'badge-info';
      case 'LIABILITY': return 'badge-error';
      case 'EQUITY': return 'badge-warning';
      case 'REVENUE': return 'badge-success';
      case 'EXPENSE': return 'badge-neutral';
      default: return 'badge-ghost';
    }
  }
}
