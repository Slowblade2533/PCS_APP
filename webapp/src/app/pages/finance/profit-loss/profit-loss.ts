import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { FinancialService } from '../../../shared/services/financial.service';
import { ProfitAndLossReport } from '../../../shared/models/financial.models';

@Component({
  selector: 'app-profit-loss',
  standalone: true,
  imports: [DecimalPipe, FormsModule],
  templateUrl: './profit-loss.html',
})
export class ProfitLoss implements OnInit {
  private financialService = inject(FinancialService);
  private destroyRef = inject(DestroyRef);

  dateFrom = signal<string>(new Date(new Date().getFullYear(), 0, 1).toLocaleDateString('en-CA')); // Start of year
  dateTo = signal<string>(new Date().toLocaleDateString('en-CA')); // Today

  report = signal<ProfitAndLossReport | null>(null);
  loading = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.loading.set(true);
    this.error.set(null);
    this.financialService
      .getProfitAndLoss(this.dateFrom(), this.dateTo())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (data) => {
          this.report.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set('ไม่สามารถดึงข้อมูลงบกำไรขาดทุนได้');
          this.loading.set(false);
        },
      });
  }
}
