import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Investor } from '../../../shared/models/investor.models';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investor-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investor-list.html',
})
export class InvestorList {
  private readonly investorService = inject(InvestorService);
  private readonly swal = inject(SweetAlertService);

  investorsResource = rxResource({
    stream: () => this.investorService.getInvestors(),
  });

  searchTerm = signal<string>('');

  constructor() {
    effect(() => {
      const err = this.investorsResource.error();
      if (err) {
        console.error('Failed to load investors', err);
        this.swal.error('ไม่สามารถโหลดข้อมูลนักลงทุนได้');
      }
    });
  }

  get filteredInvestors(): Investor[] {
    const term = this.searchTerm().toLowerCase().trim();
    const investors = this.investorsResource.value() || [];
    if (!term) return investors;

    return investors.filter(
      (i) =>
        i.firstName.toLowerCase().includes(term) ||
        i.lastName.toLowerCase().includes(term) ||
        i.email.toLowerCase().includes(term) ||
        i.phone.includes(term),
    );
  }

  onSearch(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchTerm.set(input.value);
  }

  deleteInvestor(id: string, name: string): void {
    this.swal
      .confirm(`ยืนยันการลบข้อมูลนักลงทุน ${name}?`, 'การลบข้อมูลนี้จะไม่สามารถกู้คืนได้')
      .then((confirmed) => {
        if (confirmed) {
          this.investorService.deleteInvestor(id).subscribe({
            next: () => {
              this.swal.success('ลบข้อมูลสำเร็จ');
              this.investorsResource.reload();
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
