import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Investment } from '../../../shared/models/investment.models';
import { InvestmentService } from '../../../shared/services/investment.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investment-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investment-list.html',
})
export class InvestmentList {
  private readonly investmentService = inject(InvestmentService);
  private readonly swal = inject(SweetAlertService);

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
    if (!term) return investments;

    return investments.filter(
      (inv) =>
        inv.investorName?.toLowerCase().includes(term) ||
        (inv.investmentType === 0 ? 'equity' : 'loan').includes(term) ||
        (inv.investmentType === 0 ? 'หุ้นส่วน' : 'เงินกู้ยืม').includes(term),
    );
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
          this.investmentService.deleteInvestment(id).subscribe({
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
