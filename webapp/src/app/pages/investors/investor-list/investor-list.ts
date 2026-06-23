import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Investor } from '../../../shared/models/investor.models';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investor-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investor-list.html',
  styleUrl: './investor-list.css',
})
export class InvestorList implements OnInit {
  private readonly investorService = inject(InvestorService);
  private readonly swal = inject(SweetAlertService);

  investors = signal<Investor[]>([]);
  isLoading = signal<boolean>(true);
  searchTerm = signal<string>('');

  ngOnInit(): void {
    this.loadInvestors();
  }

  loadInvestors(): void {
    this.isLoading.set(true);
    this.investorService.getInvestors().subscribe({
      next: (data) => {
        this.investors.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load investors', err);
        this.swal.error('ไม่สามารถโหลดข้อมูลนักลงทุนได้');
        this.isLoading.set(false);
      },
    });
  }

  get filteredInvestors(): Investor[] {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.investors();

    return this.investors().filter(
      (i) =>
        i.firstName.toLowerCase().includes(term) ||
        i.lastName.toLowerCase().includes(term) ||
        i.email.toLowerCase().includes(term) ||
        i.phone.includes(term)
    );
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  deleteInvestor(id: string, name: string): void {
    this.swal.confirm(`ยืนยันการลบข้อมูลนักลงทุน ${name}?`, 'การลบข้อมูลนี้จะไม่สามารถกู้คืนได้').then((confirmed) => {
      if (confirmed) {
        this.investorService.deleteInvestor(id).subscribe({
          next: () => {
            this.swal.success('ลบข้อมูลสำเร็จ');
            this.loadInvestors();
          },
          error: (err) => {
            console.error('Failed to delete investor', err);
            this.swal.error('ไม่สามารถลบข้อมูลนักลงทุนได้เนื่องจากมีข้อมูลการลงทุนผูกอยู่');
          },
        });
      }
    });
  }
}
