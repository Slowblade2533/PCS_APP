import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { CompanyBankAccount } from '../../../shared/models/financial.models';
import { Investor } from '../../../shared/models/investor.models';
import { FinancialService } from '../../../shared/services/financial.service';
import { InvestmentService } from '../../../shared/services/investment.service';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investment-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './investment-form.html',
  styleUrl: './investment-form.css',
})
export class InvestmentForm implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly investmentService = inject(InvestmentService);
  private readonly investorService = inject(InvestorService);
  private readonly financialService = inject(FinancialService);
  private readonly swal = inject(SweetAlertService);

  form!: FormGroup;
  investors = signal<Investor[]>([]);
  companyBankAccounts = signal<CompanyBankAccount[]>([]);
  isLoading = signal<boolean>(false);
  isUploading = signal<boolean>(false);
  isSubmitted = signal<boolean>(false);

  constructor() {
    this.initForm();
  }

  ngOnInit(): void {
    this.loadDropdownData();
  }

  initForm(): void {
    this.form = this.fb.group({
      investorId: ['', Validators.required],
      investmentType: [1, Validators.required], // 0 = Equity, 1 = Loan (default to Loan)
      principalAmount: [0, [Validators.required, Validators.min(1)]],
      currency: ['THB', Validators.required],
      interestRate: [0, [Validators.min(0)]],
      startDate: [new Date().toISOString().substring(0, 10), Validators.required],
      maturityDate: [''],
      contractUrl: [''],
      companyBankAccountId: [''],
      isCash: [false],
      installmentCount: [1, [Validators.min(1)]],

      interestSchedules: this.fb.array([]),
      schedules: this.fb.array([]),
    });

    // Listen to changes
    this.form.get('isCash')?.valueChanges.subscribe((isCash) => {
      const bankCtrl = this.form.get('companyBankAccountId');
      if (isCash) {
        bankCtrl?.clearValidators();
        bankCtrl?.setValue('');
      } else {
        bankCtrl?.setValidators(Validators.required);
      }
      bankCtrl?.updateValueAndValidity();
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
      })
    );
  }

  removeInterestSchedule(index: number): void {
    this.interestSchedules.removeAt(index);
  }

  addScheduleRow(data?: any): void {
    this.schedules.push(
      this.fb.group({
        installmentNumber: [data?.installmentNumber || this.schedules.length + 1, Validators.required],
        dueDate: [data?.dueDate || '', Validators.required],
        principalAmount: [data?.principalAmount || 0, [Validators.required, Validators.min(0)]],
        interestAmount: [data?.interestAmount || 0, [Validators.required, Validators.min(0)]],
      })
    );
  }

  removeScheduleRow(index: number): void {
    this.schedules.removeAt(index);
  }

  loadDropdownData(): void {
    this.investorService.getInvestors().subscribe((data) => this.investors.set(data));
    this.financialService.getCompanyBankAccounts().subscribe((data) => {
      this.companyBankAccounts.set(data.filter((b) => b.isActive));
    });
  }

  onFileUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.isUploading.set(true);

    this.financialService.uploadAttachment(file).subscribe({
      next: (res) => {
        this.form.get('contractUrl')?.setValue(res.imageUrl);
        this.isUploading.set(false);
        this.swal.success('อัปโหลดไฟล์สัญญาสำเร็จ');
      },
      error: (err) => {
        console.error('Upload failed', err);
        this.swal.error(err.error?.message || 'อัปโหลดไฟล์ล้มเหลว รองรับเฉพาะรูปภาพและ PDF ไม่เกิน 5MB');
        this.isUploading.set(false);
      },
    });
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
      const interest = outstanding * (rate / 100) / 12;

      // Due date (add i months)
      const dueDate = new Date(startDate);
      dueDate.setMonth(startDate.getMonth() + i);

      this.addScheduleRow({
        installmentNumber: i,
        dueDate: dueDate.toISOString().substring(0, 10),
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

    this.isLoading.set(true);
    const payload = this.form.value;

    this.investmentService.createInvestment(payload).subscribe({
      next: (res) => {
        this.isSubmitted.set(true);
        this.swal.success('บันทึกรายการลงทุนสำเร็จ');
        this.router.navigate(['/investments', res.investmentId]);
      },
      error: (err) => {
        console.error('Failed to create investment', err);
        this.swal.error(err.error?.message || 'บันทึกรายการลงทุนล้มเหลว กรุณาตรวจสอบข้อมูลอีกครั้ง');
        this.isLoading.set(false);
      },
    });
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSubmitted();
  }
}
