import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { BankSelectComponent } from '../../../shared/components/bank-select/bank-select';

@Component({
  selector: 'app-investor-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BankSelectComponent],
  templateUrl: './investor-form.html',
  styleUrl: './investor-form.css',
})
export class InvestorForm implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly investorService = inject(InvestorService);
  private readonly swal = inject(SweetAlertService);

  form!: FormGroup;
  isEditMode = signal<boolean>(false);
  investorId = signal<string | null>(null);
  isLoading = signal<boolean>(false);
  isSubmitted = signal<boolean>(false);

  constructor() {
    this.initForm();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.isEditMode.set(true);
        this.investorId.set(id);
        this.loadInvestorData(id);
      }
    });
  }

  initForm(): void {
    this.form = this.fb.group({
      title: ['นาย', Validators.required],
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      taxId: ['', [Validators.maxLength(50)]],
      address: ['', [Validators.maxLength(500)]],
      phone: ['', [Validators.required, Validators.maxLength(50)]],
      email: ['', [Validators.email, Validators.maxLength(100)]],
      bankAccounts: this.fb.array([]),
    });
  }

  get bankAccounts(): FormArray {
    return this.form.get('bankAccounts') as FormArray;
  }

  createBankAccountFormGroup(data?: any): FormGroup {
    return this.fb.group({
      bankAccountId: [data?.bankAccountId || ''],
      bankName: [data?.bankName || '', Validators.required],
      accountNumber: [data?.accountNumber || '', [Validators.required, Validators.pattern('^[0-9]+$')]],
      accountType: [data?.accountType || 'Savings', Validators.required],
      isDefault: [data?.isDefault || false],
    });
  }

  addBankAccount(): void {
    const defaultVal = this.bankAccounts.length === 0;
    this.bankAccounts.push(this.createBankAccountFormGroup({ isDefault: defaultVal }));
  }

  removeBankAccount(index: number): void {
    this.bankAccounts.removeAt(index);
    // Ensure at least one default if accounts exist
    if (this.bankAccounts.length > 0 && !this.bankAccounts.value.some((b: any) => b.isDefault)) {
      this.bankAccounts.at(0).patchValue({ isDefault: true });
    }
  }

  onDefaultChange(index: number): void {
    const accounts = this.bankAccounts;
    for (let i = 0; i < accounts.length; i++) {
      if (i !== index) {
        accounts.at(i).patchValue({ isDefault: false }, { emitEvent: false });
      }
    }
  }

  loadInvestorData(id: string): void {
    this.isLoading.set(true);
    this.investorService.getInvestorById(id).subscribe({
      next: (res) => {
        try {
          if (res && res.investor) {
            this.form.patchValue({
              title: res.investor.title || 'นาย',
              firstName: res.investor.firstName || '',
              lastName: res.investor.lastName || '',
              taxId: res.investor.taxId || '',
              address: res.investor.address || '',
              phone: res.investor.phone || '',
              email: res.investor.email || '',
            });
          }

          // Set bank accounts
          const accounts = res?.bankAccounts || [];
          accounts.forEach((acc: any) => {
            this.bankAccounts.push(this.createBankAccountFormGroup(acc));
          });
          this.isLoading.set(false);
        } catch (e) {
          console.error('Error parsing investor data:', e);
          this.isLoading.set(false);
        }
      },
      error: (err) => {
        console.error('Failed to load investor details', err);
        this.isLoading.set(false);
        this.swal.error('ไม่พบข้อมูลนักลงทุนดังกล่าว');
        this.router.navigate(['/investors']);
      },
    });
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSubmitted();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const rawPayload = this.form.value;
    const payload = {
      ...rawPayload,
      bankAccounts: (rawPayload.bankAccounts || []).map((b: any) => ({
        ...b,
        bankAccountId: b.bankAccountId && b.bankAccountId.trim() !== '' ? b.bankAccountId : '00000000-0000-0000-0000-000000000000'
      }))
    };

    if (this.isEditMode() && this.investorId()) {
      this.investorService.updateInvestor(this.investorId()!, payload).subscribe({
        next: () => {
          this.isSubmitted.set(true);
          this.swal.success('บันทึกข้อมูลสำเร็จ');
          this.router.navigate(['/investors', this.investorId()]);
        },
        error: (err) => {
          console.error('Failed to update investor', err);
          this.swal.error('ไม่สามารถบันทึกข้อมูลได้ กรุณาตรวจสอบอีกครั้ง');
          this.isLoading.set(false);
        },
      });
    } else {
      this.investorService.createInvestor(payload).subscribe({
        next: (res) => {
          this.isSubmitted.set(true);
          this.swal.success('เพิ่มข้อมูลนักลงทุนสำเร็จ');
          this.router.navigate(['/investors', res.investorId]);
        },
        error: (err) => {
          console.error('Failed to create investor', err);
          this.swal.error('ไม่สามารถเพิ่มข้อมูลได้ กรุณาตรวจสอบอีกครั้ง');
          this.isLoading.set(false);
        },
      });
    }
  }
}
