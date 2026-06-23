import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CompanyBankAccount } from '../../../shared/models/financial.models';
import { Investment, InvestmentInterestSchedule, InvestmentSchedule } from '../../../shared/models/investment.models';
import { Investor, InvestorBankAccount } from '../../../shared/models/investor.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investment-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './investment-detail.html',
  styleUrl: './investment-detail.css',
})
export class InvestmentDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly investmentService = inject(InvestmentService);
  private readonly investorService = inject(InvestorService);
  private readonly financialService = inject(FinancialService);
  private readonly swal = inject(SweetAlertService);

  investmentId = '';
  investment = signal<Investment | null>(null);
  schedules = signal<InvestmentSchedule[]>([]);
  interestSchedules = signal<InvestmentInterestSchedule[]>([]);
  investor = signal<Investor | null>(null);
  investorBankAccounts = signal<InvestorBankAccount[]>([]);
  companyBankAccounts = signal<CompanyBankAccount[]>([]);
  isLoading = signal<boolean>(true);
  isSubmittingRepayment = signal<boolean>(false);
  isUploadingSlip = signal<boolean>(false);

  // Repayment Modal
  showRepaymentModal = signal<boolean>(false);
  selectedSchedule = signal<InvestmentSchedule | null>(null);
  repaymentForm!: FormGroup;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.investmentId = id;
      this.loadInvestmentDetail();
      this.loadCompanyBankAccounts();
      this.initRepaymentForm();
    } else {
      this.swal.error('ไม่พบรหัสการลงทุน');
      this.router.navigate(['/investments']);
    }
  }

  loadInvestmentDetail(): void {
    this.isLoading.set(true);
    this.investmentService.getInvestmentById(this.investmentId).subscribe({
      next: (data) => {
        this.investment.set(data.investment);
        this.schedules.set(data.schedules.sort((a, b) => a.installmentNumber - b.installmentNumber));
        this.interestSchedules.set(data.interestSchedules.sort((a, b) => a.startMonth - b.startMonth));

        // Load investor details
        this.loadInvestorDetail(data.investment.investorId);
      },
      error: (err) => {
        console.error('Failed to load investment detail', err);
        this.swal.error('ไม่สามารถโหลดรายละเอียดสัญญาการร่วมลงทุนได้');
        this.isLoading.set(false);
      },
    });
  }

  loadInvestorDetail(investorId: string): void {
    this.investorService.getInvestorById(investorId).subscribe({
      next: (data) => {
        this.investor.set(data.investor);
        this.investorBankAccounts.set(data.bankAccounts);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load investor details', err);
        // Do not block showing the investment details if investor bank accounts fail to load
        this.isLoading.set(false);
      },
    });
  }

  loadCompanyBankAccounts(): void {
    this.financialService.getCompanyBankAccounts().subscribe({
      next: (data) => {
        this.companyBankAccounts.set(data.filter((b) => b.isActive));
      },
      error: (err) => {
        console.error('Failed to load company bank accounts', err);
      },
    });
  }

  initRepaymentForm(): void {
    this.repaymentForm = this.fb.group({
      paidAmount: [0, [Validators.required, Validators.min(0.01)]],
      isCash: [false],
      companyBankAccountId: ['', Validators.required],
      slipUrl: [''],
    });

    // Listen to changes in isCash to toggle validators for bank account
    this.repaymentForm.get('isCash')?.valueChanges.subscribe((isCash) => {
      const bankCtrl = this.repaymentForm.get('companyBankAccountId');
      if (isCash) {
        bankCtrl?.clearValidators();
        bankCtrl?.setValue('');
      } else {
        bankCtrl?.setValidators(Validators.required);
      }
      bankCtrl?.updateValueAndValidity();
    });
  }

  openRepaymentModal(schedule: InvestmentSchedule): void {
    this.selectedSchedule.set(schedule);
    const totalDue = schedule.principalAmount + schedule.interestAmount;
    
    this.repaymentForm.patchValue({
      paidAmount: Math.round(totalDue * 100) / 100,
      isCash: this.investment()?.isCash || false,
      companyBankAccountId: this.investment()?.companyBankAccountId || '',
      slipUrl: '',
    });

    this.showRepaymentModal.set(true);
  }

  closeRepaymentModal(): void {
    this.showRepaymentModal.set(false);
    this.selectedSchedule.set(null);
    this.repaymentForm.reset();
  }

  onSlipUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingSlip.set(true);

    this.financialService.uploadAttachment(file).subscribe({
      next: (res) => {
        this.repaymentForm.get('slipUrl')?.setValue(res.imageUrl);
        this.isUploadingSlip.set(false);
        this.swal.success('อัปโหลดสลิปสำเร็จ');
      },
      error: (err) => {
        console.error('Slip upload failed', err);
        this.swal.error('ไม่สามารถอัปโหลดไฟล์สลิปได้ รองรับรูปภาพ/PDF ไม่เกิน 5MB');
        this.isUploadingSlip.set(false);
      },
    });
  }

  submitRepayment(): void {
    if (this.repaymentForm.invalid || !this.selectedSchedule()) {
      this.repaymentForm.markAllAsTouched();
      return;
    }

    this.isSubmittingRepayment.set(true);
    const schedule = this.selectedSchedule()!;
    const payload = this.repaymentForm.value;

    this.investmentService.payInstallment(this.investmentId, schedule.scheduleId, payload).subscribe({
      next: () => {
        this.swal.success('บันทึกการชำระเงินเรียบร้อย');
        this.isSubmittingRepayment.set(false);
        this.closeRepaymentModal();
        this.loadInvestmentDetail(); // Reload to update status and remaining balances
      },
      error: (err) => {
        console.error('Failed to submit repayment', err);
        this.swal.error(err.error?.message || 'บันทึกการชำระเงินล้มเหลว กรุณาตรวจสอบข้อมูล');
        this.isSubmittingRepayment.set(false);
      },
    });
  }

  getCompanyName(bankId?: number): string {
    if (!bankId) return '-';
    const bank = this.companyBankAccounts().find((b) => b.id === bankId);
    return bank ? `${bank.bankName} - ${bank.accountNo} (${bank.accountName})` : `บัญชีธนาคาร ID: ${bankId}`;
  }

  getInvestorBankDetails(bankId?: string): string {
    if (!bankId) return '-';
    const bank = this.investorBankAccounts().find((b) => b.bankAccountId === bankId);
    return bank ? `${bank.bankName} - ${bank.accountNumber} (${bank.accountType === 'Saving' ? 'ออมทรัพย์' : 'กระแสรายวัน'})` : '-';
  }

  getTypeLabel(type?: number): string {
    if (type === 0) return 'หุ้นส่วน (Equity)';
    if (type === 1) return 'เงินกู้ยืม (Loan)';
    return '-';
  }

  getStatusLabel(status?: number): string {
    switch (status) {
      case 0: return 'กำลังดำเนินการ (Active)';
      case 1: return 'ชำระคืนครบแล้ว (Repaid)';
      case 2: return 'ผิดนัดชำระ (Defaulted)';
      case 3: return 'ยกเลิกสัญญา (Cancelled)';
      default: return '-';
    }
  }

  getScheduleStatusBadgeClass(status: number): string {
    switch (status) {
      case 0: return 'badge-info';
      case 1: return 'badge-success';
      case 2: return 'badge-error';
      default: return 'badge-ghost';
    }
  }

  getScheduleStatusLabel(status: number): string {
    switch (status) {
      case 0: return 'ค้างชำระ';
      case 1: return 'ชำระแล้ว';
      case 2: return 'เกินกำหนดชำระ';
      default: return 'ไม่ทราบ';
    }
  }
}
