import { Component, Input } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-financial-summary-cards',
  standalone: true,
  imports: [CommonModule, DecimalPipe],
  templateUrl: './financial-summary-cards.html',
  styles: [
    `
      :host {
        display: block;
        width: 100%;
      }
    `,
  ],
})
export class FinancialSummaryCardsComponent {
  @Input() income: number = 0;
  @Input() expense: number = 0;
  @Input() balance: number = 0;

  @Input() incomeLabel: string = 'รายรับรวม';
  @Input() expenseLabel: string = 'รายจ่ายรวม';
  @Input() balanceLabel: string = 'ยอดคงเหลือสุทธิ';

  @Input() incomeDesc: string = 'รวมตามฟิลเตอร์ปัจจุบัน';
  @Input() expenseDesc: string = 'รวมตามฟิลเตอร์ปัจจุบัน';
  @Input() balanceDesc: string = 'รายรับรวม หัก รายจ่ายรวม';
}
