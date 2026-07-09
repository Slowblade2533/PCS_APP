import { CommonModule } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed, rxResource } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of } from 'rxjs';
import { BankSelectComponent } from '../../../shared/components/bank-select/bank-select';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { InvestorBankAccount } from '../../../shared/models/investor.models';
import { InvestorService } from '../../../shared/services/investor.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-investor-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, BankSelectComponent],
  templateUrl: './investor-form.html',
})
export class InvestorForm implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly investorService = inject(InvestorService);
  private readonly swal = inject(SweetAlertService);
  private readonly destroyRef = inject(DestroyRef);

  form!: FormGroup;
  isEditMode = signal<boolean>(false);
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

  isSaving = signal<boolean>(false);
  isLoading = computed(() => this.investorResource.isLoading() || this.isSaving());
  isSubmitted = signal<boolean>(false);

  constructor() {
    this.initForm();

    effect(() => {
      const res = this.investorResource.value();
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

        while (this.bankAccounts.length > 0) {
          this.bankAccounts.removeAt(0);
        }

        const accounts = res.bankAccounts || [];
        accounts.forEach((acc: InvestorBankAccount) => {
          this.bankAccounts.push(this.createBankAccountFormGroup(acc));
        });
      }
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.investorId.set(id);
    }
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

  createBankAccountFormGroup(data?: Partial<InvestorBankAccount>): FormGroup {
    return this.fb.group({
      bankAccountId: [data?.bankAccountId || ''],
      bankName: [data?.bankName || '', Validators.required],
      accountNumber: [
        data?.accountNumber || '',
        [Validators.required, Validators.pattern('^[0-9]+$')],
      ],
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
    if (this.bankAccounts.length > 0 && !this.bankAccounts.value.some((b: InvestorBankAccount) => b.isDefault)) {
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

  // Removed loadInvestorData

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.isSubmitted();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const rawPayload = this.form.value;
    const payload = {
      ...rawPayload,
      bankAccounts: (rawPayload.bankAccounts || []).map((b: InvestorBankAccount) => ({
        ...b,
        bankAccountId:
          b.bankAccountId && b.bankAccountId.trim() !== ''
            ? b.bankAccountId
            : '00000000-0000-0000-0000-000000000000',
      })),
    };

    if (this.isEditMode() && this.investorId()) {
      this.investorService.updateInvestor(this.investorId()!, payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => {
          this.isSubmitted.set(true);
          this.swal.success('บันทึกข้อมูลสำเร็จ');
          this.router.navigate(['/investors', this.investorId()]);
        },
        error: (err) => {
          console.error('Failed to update investor', err);
          this.swal.error('ไม่สามารถบันทึกข้อมูลได้ กรุณาตรวจสอบอีกครั้ง');
          this.isSaving.set(false);
        },
      });
    } else {
      this.investorService.createInvestor(payload).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (res) => {
          this.isSubmitted.set(true);
          this.swal.success('เพิ่มข้อมูลนักลงทุนสำเร็จ');
          this.isSaving.set(false);
          this.router.navigate(['/investors', res.investorId]);
        },
        error: (err) => {
          console.error('Failed to create investor', err);
          this.swal.error('ไม่สามารถเพิ่มข้อมูลได้ กรุณาตรวจสอบอีกครั้ง');
          this.isSaving.set(false);
        },
      });
    }
  }
}
