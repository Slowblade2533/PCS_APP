import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FinancialService } from '../../../shared/services/financial.service';
import { BalanceSheetReport } from '../../../shared/models/financial.models';

@Component({
  selector: 'app-balance-sheet',
  standalone: true,
  imports: [DecimalPipe, FormsModule],
  templateUrl: './balance-sheet.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BalanceSheet implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  asOfDate = signal<string>(new Date().toISOString().split('T')[0]); // Today
  
  report = signal<BalanceSheetReport | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    this.error.set(null);
    this.financialService.getBalanceSheet(this.asOfDate())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.report.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('ไม่สามารถดึงข้อมูลงบดุลได้');
          this.loading.set(false);
        }
      });
  }
}
