import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { FinancialService } from '../../../shared/services/financial.service';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

function formatDateString(val: any): string {
  if (!val) return '';
  if (typeof val === 'string') {
    return val.split('T')[0];
  }
  if (val instanceof Date) {
    const y = val.getFullYear();
    const m = String(val.getMonth() + 1).padStart(2, '0');
    const d = String(val.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
  return '';
}

@Component({
  selector: 'app-investment-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ImageHoverPreview],
  templateUrl: './investment-form.html',
})
export class InvestmentForm implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly investmentService = inject(InvestmentService);
  private readonly investorService = inject(InvestorService);
  private readonly financialService = inject(FinancialService);
  private readonly swal = inject(SweetAlertService);

  form!: FormGroup;
  investmentId = signal<string | null>(null);
  selectedInvestorId = signal<string | null>(null);
  isEditMode = computed(() => !!this.investmentId());
  hasPaidSchedules = signal<boolean>(false);

  investorsResource = rxResource({
    stream: () => this.investorService.getInvestors().pipe(catchError(() => of([]))),
  });
  investors = computed(() => this.investorsResource.value() || []);

  companyBankAccountsResource = rxResource({
    stream: () =>
      this.financialService
        .getCompanyBankAccounts()
        .pipe(map((data) => data.filter((b) => b.isActive))),
  });
  companyBankAccounts = computed(() => this.companyBankAccountsResource.value() || []);

  investorBankAccountsResource = rxResource({
    params: () => this.selectedInvestorId(),
    stream: ({ params }) => {
      if (!params) return of([]);
      return this.investorService.getInvestorById(params).pipe(
        map((res) => res.bankAccounts || []),
        catchError(() => of([])),
      );
    },
  });
  investorBankAccounts = computed(() => this.investorBankAccountsResource.value() || []);

  investmentDetailResource = rxResource({
    params: () => this.investmentId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.investmentService.getInvestmentById(params).pipe(
        catchError((err) => {
          console.error('Failed to load investment detail', err);
          this.swal.error('ไม่สามารถโหลดรายละเอียดสัญญาการลงทุนได้');
          this.router.navigate(['/investments']);
          return of(null);
        }),
      );
    },
  });

  personalAccounts = computed(() =>
    this.companyBankAccounts().filter((acc) => acc.accountType === 'Personal'),
  );
  businessAccounts = computed(() =>
    this.companyBankAccounts().filter((acc) => acc.accountType === 'Business' || !acc.accountType),
  );
  isSaving = signal<boolean>(false);
  isLoading = computed(
    () =>
      this.investorsResource.isLoading() ||
      this.companyBankAccountsResource.isLoading() ||
      this.investmentDetailResource.isLoading() ||
      this.isSaving(),
  );
  isUploadingContract = signal<boolean>(false);
  isUploadingPaymentProof = signal<boolean>(false);
  isUploading = computed(() => this.isUploadingContract() || this.isUploadingPaymentProof());
  isSubmitted = signal<boolean>(false);

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

  constructor() {
    this.initForm();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.investmentId.set(id);
    }
  }

  initForm(): void {
    this.form = this.fb.group({
      investorId: ['', Validators.required],
      investmentType: [1, Validators.required], // 0 = Equity, 1 = Loan (default to Loan)
      principalAmount: [0, [Validators.required, Validators.min(1)]],
      currency: ['THB', Validators.required],
      interestRate: [0, [Validators.min(0)]],
      startDate: [formatDateString(new Date()), Validators.required],
      maturityDate: [''],
      contractUrl: [''],
      paymentProofUrl: [''],
      companyBankAccountId: [''],
      isCash: [false],
      investorBankAccountId: [''],
      installmentCount: [1, [Validators.min(1)]],

      interestSchedules: this.fb.array([]),
      schedules: this.fb.array([]),
    });

    // Listen to changes
    this.form.get('investorId')?.valueChanges.subscribe((investorId) => {
      if (investorId) {
        this.selectedInvestorId.set(investorId);
      } else {
        this.selectedInvestorId.set(null);
        this.form.get('investorBankAccountId')?.setValue('');
        this.updateInvestorBankValidators();
      }
    });

    effect(() => {
      const accounts = this.investorBankAccountsResource.value();
      if (accounts) {
        const currentVal = this.form.get('investorBankAccountId')?.value;
        const exists = accounts.some((a) => a.bankAccountId === currentVal);

        if (!exists && this.selectedInvestorId()) {
          const defaultAcc = accounts.find((a) => a.isDefault);
          if (defaultAcc) {
            this.form.get('investorBankAccountId')?.setValue(defaultAcc.bankAccountId);
          } else if (accounts.length > 0) {
            this.form.get('investorBankAccountId')?.setValue(accounts[0].bankAccountId);
          } else {
            this.form.get('investorBankAccountId')?.setValue('');
          }
        }
        this.updateInvestorBankValidators();
      }
    });

    effect(() => {
      const data = this.investmentDetailResource.value();
      if (data) {
        const inv = data.investment;
        const schedules = data.schedules;
        const interestSchedules = data.interestSchedules;

        const hasPaid = schedules.some((s: any) => s.status === 1);
        this.hasPaidSchedules.set(hasPaid);

        this.form.patchValue({
          investorId: inv.investorId,
          investmentType: inv.investmentType,
          principalAmount: inv.principalAmount,
          currency: inv.currency,
          interestRate: inv.interestRate || 0,
          startDate: formatDateString(inv.startDate),
          maturityDate: formatDateString(inv.maturityDate),
          contractUrl: inv.contractUrl,
          paymentProofUrl: inv.paymentProofUrl,
          companyBankAccountId: inv.companyBankAccountId || '',
          isCash: inv.isCash,
          investorBankAccountId: inv.investorBankAccountId || '',
          installmentCount: schedules.length,
        });

        this.clearInterestSchedules();
        if (interestSchedules && interestSchedules.length > 0) {
          interestSchedules.forEach((ins: any) => {
            this.interestSchedules.push(
              this.fb.group({
                startMonth: [ins.startMonth, Validators.required],
                endMonth: [ins.endMonth, Validators.required],
                interestRate: [ins.interestRate, [Validators.required, Validators.min(0)]],
              }),
            );
          });
        }

        this.clearSchedules();
        if (schedules && schedules.length > 0) {
          schedules.forEach((s: any) => {
            this.schedules.push(
              this.fb.group({
                installmentNumber: [s.installmentNumber, Validators.required],
                dueDate: [
                  formatDateString(s.dueDate),
                  Validators.required,
                ],
                principalAmount: [s.principalAmount, [Validators.required, Validators.min(0)]],
                interestAmount: [s.interestAmount, [Validators.required, Validators.min(0)]],
              }),
            );
          });
        }

        if (hasPaid) {
          this.form.get('investorId')?.disable();
          this.form.get('investmentType')?.disable();
          this.form.get('principalAmount')?.disable();
          this.form.get('interestRate')?.disable();
          this.form.get('startDate')?.disable();
          this.form.get('maturityDate')?.disable();
          this.form.get('installmentCount')?.disable();
          this.form.get('isCash')?.disable();
          this.form.get('investorBankAccountId')?.disable();
          this.form.get('companyBankAccountId')?.disable();
          this.interestSchedules.disable();
          this.schedules.disable();
        }
      }
    });

    this.form.get('isCash')?.valueChanges.subscribe((isCash) => {
      const bankCtrl = this.form.get('companyBankAccountId');
      if (isCash) {
        bankCtrl?.clearValidators();
        bankCtrl?.setValue('');
      } else {
        bankCtrl?.setValidators(Validators.required);
      }
      bankCtrl?.updateValueAndValidity();
      this.updateInvestorBankValidators();
    });

    this.form.get('investmentType')?.valueChanges.subscribe((type) => {
      const rateCtrl = this.form.get('interestRate');
      const instCtrl = this.form.get('installmentCount');
      if (Number(type) === 0) {
        // Equity
        rateCtrl?.setValue(0);
        rateCtrl?.disable();
        instCtrl?.setValue(0);
        instCtrl?.disable();
        this.clearSchedules();
        this.clearInterestSchedules();
      } else {
        // Loan
        rateCtrl?.enable();
        instCtrl?.enable();
      }
    });
  }

  get interestSchedules(): FormArray {
    return this.form.get('interestSchedules') as FormArray;
  }

  get schedules(): FormArray {
    return this.form.get('schedules') as FormArray;
  }

  clearSchedules(): void {
    while (this.schedules.length !== 0) {
      this.schedules.removeAt(0);
    }
  }

  clearInterestSchedules(): void {
    while (this.interestSchedules.length !== 0) {
      this.interestSchedules.removeAt(0);
    }
  }

  addInterestSchedule(): void {
    this.interestSchedules.push(
      this.fb.group({
        startMonth: [1, Validators.required],
        endMonth: [6, Validators.required],
        interestRate: [0, [Validators.required, Validators.min(0)]],
      }),
    );
  }

  removeInterestSchedule(index: number): void {
    this.interestSchedules.removeAt(index);
  }

  addScheduleRow(data?: any): void {
    this.schedules.push(
      this.fb.group({
        installmentNumber: [
          data?.installmentNumber || this.schedules.length + 1,
          Validators.required,
        ],
        dueDate: [data?.dueDate || '', Validators.required],
        principalAmount: [data?.principalAmount || 0, [Validators.required, Validators.min(0)]],
        interestAmount: [data?.interestAmount || 0, [Validators.required, Validators.min(0)]],
      }),
    );
  }

  removeScheduleRow(index: number): void {
    this.schedules.removeAt(index);
  }

  // Removed loadDropdownData, loadInvestmentDetail, and loadInvestorBankAccounts

  updateInvestorBankValidators(): void {
    const investorBankCtrl = this.form.get('investorBankAccountId');
    const isCash = this.form.get('isCash')?.value;
    const hasAccounts = this.investorBankAccounts().length > 0;

    if (!isCash && hasAccounts) {
      investorBankCtrl?.setValidators(Validators.required);
    } else {
      investorBankCtrl?.clearValidators();
    }
    investorBankCtrl?.updateValueAndValidity();
  }

  onContractUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingContract.set(true);

    this.financialService.uploadAttachment(file).subscribe({
      next: (res) => {
        this.form.get('contractUrl')?.setValue(res.imageUrl);
        this.isUploadingContract.set(false);
        this.swal.success('อัปโหลดไฟล์สัญญาสำเร็จ');
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.swal.error(
          err.error?.message ||
            'อัปโหลดไฟล์สัญญาเสร็จสิ้นล้มเหลว รองรับเฉพาะรูปภาพและ PDF ไม่เกิน 5MB',
        );
        this.isUploadingContract.set(false);
      },
    });
  }

  onPaymentProofUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploadingPaymentProof.set(true);

    this.financialService.uploadAttachment(file).subscribe({
      next: (res) => {
        this.form.get('paymentProofUrl')?.setValue(res.imageUrl);
        this.isUploadingPaymentProof.set(false);
        this.swal.success('อัปโหลดหลักฐานการรับเงินสำเร็จ');
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.swal.error(
          err.error?.message ||
            'อัปโหลดหลักฐานการรับเงินล้มเหลว รองรับเฉพาะรูปภาพและ PDF ไม่เกิน 5MB',
        );
        this.isUploadingPaymentProof.set(false);
      },
    });
  }

  clearContract(input: HTMLInputElement): void {
    this.form.get('contractUrl')?.setValue('');
    input.value = '';
  }

  clearPaymentProof(input: HTMLInputElement): void {
    this.form.get('paymentProofUrl')?.setValue('');
    input.value = '';
  }

  autoCalculateSchedules(): void {
    const type = Number(this.form.get('investmentType')?.value);
    if (type === 0) return; // Equity has no schedules

    const principal = Number(this.form.get('principalAmount')?.value);
    const count = Number(this.form.get('installmentCount')?.value);
    const defaultRate = Number(this.form.get('interestRate')?.value);
    const startDateStr = this.form.get('startDate')?.value;

    if (!principal || !count || !startDateStr) {
      this.swal.error('กรุณาระบุ เงินต้น จำนวนงวด และวันที่เริ่มต้นสัญญาก่อนคำนวณ');
      return;
    }

    this.clearSchedules();

    const startDate = new Date(startDateStr);
    let outstanding = principal;
    const basePrincipal = principal / count;

    // Parse interest schedules
    const intScheds = this.interestSchedules.value as any[];

    for (let i = 1; i <= count; i++) {
      // Find interest rate for this month
      let rate = defaultRate;
      const matchedSched = intScheds.find((s) => i >= s.startMonth && i <= s.endMonth);
      if (matchedSched) {
        rate = matchedSched.interestRate;
      }

      // Interest calculation: Simple monthly flat on outstanding
      const interest = (outstanding * (rate / 100)) / 12;

      // Due date (add i months)
      const dueDate = new Date(startDate);
      dueDate.setMonth(startDate.getMonth() + i);

      this.addScheduleRow({
        installmentNumber: i,
        dueDate: formatDateString(dueDate),
        principalAmount: Math.round(basePrincipal * 100) / 100,
        interestAmount: Math.round(interest * 100) / 100,
      });

      outstanding -= basePrincipal;
    }

    // Set maturity date to last installment's due date
    if (this.schedules.length > 0) {
      const lastDueDate = this.schedules.at(this.schedules.length - 1).get('dueDate')?.value;
      this.form.get('maturityDate')?.setValue(lastDueDate);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    // Use getRawValue to get values from disabled controls as well
    const payload = this.form.getRawValue();

    // Clean nullable fields of empty strings so System.Text.Json doesn't fail parsing
    if (!payload.investorBankAccountId) {
      payload.investorBankAccountId = null;
    }
    if (!payload.companyBankAccountId) {
      payload.companyBankAccountId = null;
    } else {
      payload.companyBankAccountId = Number(payload.companyBankAccountId);
    }
    if (!payload.maturityDate) {
      payload.maturityDate = null;
    }
    if (!payload.interestRate && payload.interestRate !== 0) {
      payload.interestRate = null;
    }
    if (!payload.contractUrl) {
      payload.contractUrl = null;
    }
    if (!payload.paymentProofUrl) {
      payload.paymentProofUrl = null;
    }

    if (this.isEditMode()) {
      this.investmentService.updateInvestment(this.investmentId()!, payload).subscribe({
        next: () => {
          this.isSubmitted.set(true);
          this.swal.success('แก้ไขรายละเอียดสัญญาสำเร็จ');
          this.router.navigate(['/investments', this.investmentId()]);
        },
        error: (err) => {
          console.error('Failed to update investment', err);
          this.swal.error(
            err.error?.message || 'แก้ไขรายละเอียดสัญญาล้มเหลว กรุณาตรวจสอบข้อมูลอีกครั้ง',
          );
          this.isSaving.set(false);
        },
      });
    } else {
      this.investmentService.createInvestment(payload).subscribe({
        next: (res) => {
          this.isSubmitted.set(true);
          this.swal.success('บันทึกรายการลงทุนสำเร็จ');
          this.isSaving.set(false);
          this.router.navigate(['/investments', res.investmentId]);
        },
        error: (err) => {
          console.error('Failed to create investment', err);
          this.swal.error(
            err.error?.message || 'บันทึกรายการลงทุนล้มเหลว กรุณาตรวจสอบข้อมูลอีกครั้ง',
          );
          this.isSaving.set(false);
        },
      });
    }
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSubmitted();
  }
}
