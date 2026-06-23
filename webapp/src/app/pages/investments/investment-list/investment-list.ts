import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Investment } from '../../../shared/models/investment.models';
import { InvestmentService } from '../../../shared/services/investment.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investment-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investment-list.html',
  styleUrl: './investment-list.css',
})
export class InvestmentList implements OnInit {
  private readonly investmentService = inject(InvestmentService);
  private readonly swal = inject(SweetAlertService);

  investments = signal<Investment[]>([]);
  isLoading = signal<boolean>(true);
  searchTerm = signal<string>('');

  ngOnInit(): void {
    this.loadInvestments();
  }

  loadInvestments(): void {
    this.isLoading.set(true);
    this.investmentService.getInvestments().subscribe({
      next: (data) => {
        this.investments.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load investments', err);
        this.swal.error('ไม่สามารถโหลดข้อมูลการลงทุนได้');
        this.isLoading.set(false);
      },
    });
  }

  get filteredInvestments(): Investment[] {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.investments();

    return this.investments().filter(
      (inv) =>
        inv.investorName?.toLowerCase().includes(term) ||
        (inv.investmentType === 0 ? 'equity' : 'loan').includes(term) ||
        (inv.investmentType === 0 ? 'หุ้นส่วน' : 'เงินกู้ยืม').includes(term)
    );
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  deleteInvestment(id: string): void {
    this.swal.confirm('ยืนยันการลบรายการลงทุน?', 'การดำเนินการนี้จะไม่สามารถกู้คืนได้ และควรระมัดระวังเรื่องยอดบัญชีที่บันทึกไปแล้ว').then((confirmed) => {
      if (confirmed) {
        this.investmentService.deleteInvestment(id).subscribe({
          next: () => {
            this.swal.success('ลบรายการสำเร็จ');
            this.loadInvestments();
          },
          error: (err) => {
            console.error('Failed to delete investment', err);
            this.swal.error('ไม่สามารถลบรายการลงทุนได้ เนื่องจากมีประวัติการจ่ายชำระเงินคืนผูกอยู่');
          },
        });
      }
    });
  }
}
