import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { bankLists } from '../../../shared/constants/banks.constants';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investor-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './investor-detail.html',
})
export class InvestorDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly investorService = inject(InvestorService);
  private readonly investmentService = inject(InvestmentService);
  private readonly swal = inject(SweetAlertService);
  private readonly destroyRef = inject(DestroyRef);

  investorId = signal<string | null>(null);

  investorResource = rxResource({
    params: () => this.investorId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.investorService.getInvestorById(params).pipe(
        catchError((err) => {
          console.error('Failed to load investor details', err);
          this.swal.error('ไม่พบข้อมูลนักลงทุนดังกล่าว');
          this.router.navigate(['/investors']);
          return of(null);
        }),
      );
    },
  });

  investor = computed(() => this.investorResource.value()?.investor || null);
  bankAccounts = computed(() => this.investorResource.value()?.bankAccounts || []);

  investmentsResource = rxResource({
    params: () => this.investorId(),
    stream: ({ params }) => {
      if (!params) return of([]);
      return this.investmentService.getInvestments().pipe(
        map((allInvestments) => allInvestments.filter((inv) => inv.investorId === params)),
        catchError((err) => {
          console.error('Failed to load investments', err);
          return of([]);
        }),
      );
    },
  });

  investments = computed(() => this.investmentsResource.value() || []);
  isLoading = computed(
    () => this.investorResource.isLoading() || this.investmentsResource.isLoading(),
  );

  getBank(symbol: string) {
    if (!symbol) return null;
    return bankLists[symbol] || null;
  }

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.investorId.set(id);
      }
    });
  }

  getTotalInvested(): number {
    return this.investments().reduce((sum, inv) => sum + inv.principalAmount, 0);
  }

  deleteInvestor(): void {
    const inv = this.investor();
    if (!inv) return;

    this.swal
      .confirm(
        `ยืนยันการลบข้อมูลนักลงทุน ${inv.firstName} ${inv.lastName}?`,
        'การลบข้อมูลจะไม่สามารถย้อนกลับได้',
      )
      .then((confirmed) => {
        if (confirmed) {
          this.investorService.deleteInvestor(inv.investorId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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
