import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { InvestmentSchedule } from '../../../shared/models/investment.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investment-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ImageHoverPreview],
  templateUrl: './investment-detail.html',
})
export class InvestmentDetail implements OnInit {
  apiOrigin = environment.apiUrl.replace('/api', '');

  isPdf(url: string | null | undefined): boolean {
    if (!url) return false;
    return url.toLowerCase().endsWith('.pdf');
  }

  isImage(url: string | null | undefined): boolean {
    if (!url) return false;
    const lower = url.toLowerCase();
    return (
      lower.endsWith('.jpg') ||
      lower.endsWith('.jpeg') ||
      lower.endsWith('.png') ||
      lower.endsWith('.webp') ||
      lower.endsWith('.gif')
    );
  }

  getFileUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    return `${this.apiOrigin}${url}`;
  }
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly investmentService = inject(InvestmentService);
  private readonly investorService = inject(InvestorService);
  private readonly financialService = inject(FinancialService);
  private readonly swal = inject(SweetAlertService);

  investmentId = signal<string>('');

  investmentResource = rxResource({
    params: () => this.investmentId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.investmentService.getInvestmentById(params);
    },
  });

  investment = computed(() => this.investmentResource.value()?.investment || null);
  schedules = computed(() => {
    const schedules = this.investmentResource.value()?.schedules || [];
    return [...schedules].sort((a, b) => a.installmentNumber - b.installmentNumber);
  });
  interestSchedules = computed(() => {
    const interestSchedules = this.investmentResource.value()?.interestSchedules || [];
    return [...interestSchedules].sort((a, b) => a.startMonth - b.startMonth);
  });

  investorId = computed(() => this.investment()?.investorId);

  investorResource = rxResource({
    params: () => this.investorId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.investorService.getInvestorById(params);
    },
  });

  investor = computed(() => this.investorResource.value()?.investor || null);
  investorBankAccounts = computed(() => this.investorResource.value()?.bankAccounts || []);

  companyBankAccountsResource = rxResource({
    params: () => true,
    stream: () => this.financialService.getCompanyBankAccounts(),
  });

  companyBankAccounts = computed(
    () => this.companyBankAccountsResource.value()?.filter((b) => b.isActive) || [],
  );

  isLoading = computed(
    () =>
      this.investmentResource.isLoading() ||
      this.investorResource.isLoading() ||
      this.companyBankAccountsResource.isLoading(),
  );

  isSubmittingRepayment = signal<boolean>(false);
  isUploadingSlip = signal<boolean>(false);

  // Repayment Modal
  showRepaymentModal = signal<boolean>(false);
  selectedSchedule = signal<InvestmentSchedule | null>(null);
  repaymentForm!: FormGroup;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.investmentId.set(id);
      this.initRepaymentForm();
    } else {
      this.swal.error('ไม่พบรหัสการลงทุน');
      this.router.navigate(['/investments']);
    }
  }

  loadInvestmentDetail(): void {
    this.investmentResource.reload();
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

    this.investmentService
      .payInstallment(this.investmentId(), schedule.scheduleId, payload)
      .subscribe({
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
    return bank
      ? `${bank.bankName} - ${bank.accountNo} (${bank.accountName})`
      : `บัญชีธนาคาร ID: ${bankId}`;
  }

  getInvestorBankDetails(bankId?: string): string {
    if (!bankId) return '-';
    const bank = this.investorBankAccounts().find((b) => b.bankAccountId === bankId);
    return bank
      ? `${bank.bankName} - ${bank.accountNumber} (${bank.accountType === 'Saving' ? 'ออมทรัพย์' : 'กระแสรายวัน'})`
      : '-';
  }

  getTypeLabel(type?: number): string {
    if (type === 0) return 'หุ้นส่วน (Equity)';
    if (type === 1) return 'เงินกู้ยืม (Loan)';
    return '-';
  }

  getStatusLabel(status?: number): string {
    switch (status) {
      case 0:
        return 'กำลังดำเนินการ (Active)';
      case 1:
        return 'ชำระคืนครบแล้ว (Repaid)';
      case 2:
        return 'ผิดนัดชำระ (Defaulted)';
      case 3:
        return 'ยกเลิกสัญญา (Cancelled)';
      default:
        return '-';
    }
  }

  getScheduleStatusBadgeClass(status: number): string {
    switch (status) {
      case 0:
        return 'badge-info';
      case 1:
        return 'badge-success';
      case 2:
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  getScheduleStatusLabel(status: number): string {
    switch (status) {
      case 0:
        return 'ค้างชำระ';
      case 1:
        return 'ชำระแล้ว';
      case 2:
        return 'เกินกำหนดชำระ';
      default:
        return 'ไม่ทราบ';
    }
  }
}
