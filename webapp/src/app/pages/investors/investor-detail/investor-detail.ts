import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Investment } from '../../../shared/models/investment.models';
import { Investor, InvestorBankAccount } from '../../../shared/models/investor.models';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { bankLists } from '../../../shared/constants/banks.constants';

@Component({
  selector: 'app-investor-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investor-detail.html',
  styleUrl: './investor-detail.css',
})
export class InvestorDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly investorService = inject(InvestorService);
  private readonly investmentService = inject(InvestmentService);
  private readonly swal = inject(SweetAlertService);

  investor = signal<Investor | null>(null);
  bankAccounts = signal<InvestorBankAccount[]>([]);
  investments = signal<Investment[]>([]);
  isLoading = signal<boolean>(true);

  getBank(symbol: string) {
    if (!symbol) return null;
    return bankLists[symbol] || null;
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.loadInvestorData(id);
      }
    });
  }

  loadInvestorData(id: string): void {
    this.isLoading.set(true);
    // Load investor and bank accounts
    this.investorService.getInvestorById(id).subscribe({
      next: (res) => {
        this.investor.set(res.investor);
        this.bankAccounts.set(res.bankAccounts);

        // Load associated investments
        this.investmentService.getInvestments().subscribe({
          next: (allInvestments) => {
            const investorInvestments = allInvestments.filter((inv) => inv.investorId === id);
            this.investments.set(investorInvestments);
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Failed to load investments', err);
            this.isLoading.set(false);
          },
        });
      },
      error: (err) => {
        console.error('Failed to load investor details', err);
        this.swal.error('ไม่พบข้อมูลนักลงทุนดังกล่าว');
        this.router.navigate(['/investors']);
        this.isLoading.set(false);
      },
    });
  }

  getTotalInvested(): number {
    return this.investments().reduce((sum, inv) => sum + inv.principalAmount, 0);
  }

  deleteInvestor(): void {
    const inv = this.investor();
    if (!inv) return;

    this.swal.confirm(`ยืนยันการลบข้อมูลนักลงทุน ${inv.firstName} ${inv.lastName}?`, 'การลบข้อมูลจะไม่สามารถย้อนกลับได้').then((confirmed) => {
      if (confirmed) {
        this.investorService.deleteInvestor(inv.investorId).subscribe({
          next: () => {
            this.swal.success('ลบข้อมูลสำเร็จ');
            this.router.navigate(['/investors']);
          },
          error: (err) => {
            console.error('Failed to delete investor', err);
            this.swal.error('ไม่สามารถลบข้อมูลนักลงทุนได้เนื่องจากมีข้อมูลการร่วมลงทุนผูกอยู่');
          },
        });
      }
    });
  }
}
