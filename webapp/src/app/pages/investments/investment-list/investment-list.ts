import { CommonModule } from '@angular/common';
import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Investment } from '../../../shared/models/investment.models';
import { InvestmentService } from '../../../shared/services/investment.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { FinancialSummaryCardsComponent } from '../../../shared/components/financial-summary-cards/financial-summary-cards';

@Component({
  selector: 'app-investment-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FinancialSummaryCardsComponent],
  templateUrl: './investment-list.html',
})
export class InvestmentList {
  private readonly investmentService = inject(InvestmentService);
  private readonly swal = inject(SweetAlertService);
  private readonly destroyRef = inject(DestroyRef);

  investmentsResource = rxResource({
    stream: () => this.investmentService.getInvestments(),
  });

  searchTerm = signal<string>('');

  constructor() {
    effect(() => {
      const err = this.investmentsResource.error();
      if (err) {
        console.error('Failed to load investments', err);
        this.swal.error('ไม่สามารถโหลดข้อมูลการลงทุนได้');
      }
    });
  }

  get filteredInvestments(): Investment[] {
    const term = this.searchTerm().toLowerCase().trim();
    const investments = this.investmentsResource.value() || [];
    let filtered = investments;

    if (term) {
      filtered = investments.filter(
        (inv) =>
          inv.investorName?.toLowerCase().includes(term) ||
          (inv.investmentType === 0 ? 'equity' : 'loan').includes(term) ||
          (inv.investmentType === 0 ? 'หุ้นส่วน' : 'เงินกู้ยืม').includes(term),
      );
    }

    // Sort by newest to oldest
    return filtered.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  }

  get totalInvestment(): number {
    return (this.investmentsResource.value() || []).reduce((sum, inv) => sum + inv.principalAmount, 0);
  }

  // To be properly implemented when expense tracking per investment is available
  get totalSpent(): number {
    return 0; 
  }

  get remainingBalance(): number {
    return this.totalInvestment - this.totalSpent;
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  deleteInvestment(id: string): void {
    this.swal
      .confirm(
        'ยืนยันการลบรายการลงทุน?',
        'การดำเนินการนี้จะไม่สามารถกู้คืนได้ และควรระมัดระวังเรื่องยอดบัญชีที่บันทึกไปแล้ว',
      )
      .then((confirmed) => {
        if (confirmed) {
          this.investmentService.deleteInvestment(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: () => {
              this.swal.success('ลบรายการสำเร็จ');
              this.investmentsResource.reload();
            },
            error: (err) => {
              console.error('Failed to delete investment', err);
              this.swal.error(
                'ไม่สามารถลบรายการลงทุนได้ เนื่องจากมีประวัติการจ่ายชำระเงินคืนผูกอยู่',
              );
            },
          });
        }
      });
  }
}
