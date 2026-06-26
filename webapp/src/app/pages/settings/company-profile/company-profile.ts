import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal, computed, effect } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { rxResource } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { CompanyBankAccount } from '../../../shared/models/financial.models';
import { BankSelectComponent } from '../../../shared/components/bank-select/bank-select';
import { bankLists, Bank } from '../../../shared/constants/banks.constants';

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BankSelectComponent],
  templateUrl: './company-profile.html',
})
export class CompanyProfile implements OnInit {
  private destroyRef = inject(DestroyRef);
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private swal = inject(SweetAlertService);

  isSaving = signal(false);
  showBankForm = signal(false);
  editingBankId = signal<number | null>(null);
  bankForm: FormGroup;

  stagedLogoFile = signal<File | null>(null);
  stagedLogoPreviewUrl = signal<string | null>(null);

  stagedVatDocFile = signal<File | null>(null);
  stagedVatDocFileName = signal<string | null>(null);

  apiOrigin = environment.apiUrl;

  form: FormGroup;

  // We will assume branch ID = 1 is the main branch for now
  branchId = 1;

  constructor() {
    this.form = this.fb.group({
      branchCode: ['', [Validators.required, Validators.pattern(/^\d{5}$/)]],
      branchName: ['', Validators.required],
      registrationName: [''],
      entityType: ['Individual'],
      companyType: ['บุคคลธรรมดา'],
      isVatRegistered: [false],
      vatDocumentUrl: [null],
      taxId: [''],
      address: [''],
      phone: [''],
      email: [''],
      logoUrl: [null],
    });

    this.bankForm = this.fb.group({
      bankName: ['', Validators.required],
      accountNo: ['', [Validators.required, Validators.pattern(/^[0-9\-]{10,20}$/)]],
      accountName: ['', Validators.required],
      accountType: ['Business', Validators.required],
      isActive: [true],
    });

    effect(() => {
      const profile = this.profileResource.value();
      if (profile) {
        this.form.patchValue({
          branchCode: profile.branchCode || '00000',
          branchName: profile.branchName,
          registrationName: profile.registrationName,
          entityType: profile.entityType || 'Individual',
          companyType: profile.companyType || 'บุคคลธรรมดา',
          isVatRegistered: profile.isVatRegistered || false,
          vatDocumentUrl: profile.vatDocumentUrl || null,
          taxId: profile.taxId,
          address: profile.address,
          phone: profile.phone,
          email: profile.email,
          logoUrl: profile.logoUrl,
        });
        this.stagedLogoFile.set(null);
        this.stagedLogoPreviewUrl.set(null);
        this.stagedVatDocFile.set(null);
        this.stagedVatDocFileName.set(null);
      }
    });
  }

  profileResource = rxResource({
    params: () => this.branchId,
    stream: ({ params }) => this.http.get<any>(`${environment.apiUrl}/branches/${params}`),
  });

  bankAccountsResource = rxResource({
    params: () => true,
    stream: () =>
      this.http.get<CompanyBankAccount[]>(`${environment.apiUrl}/bank-accounts/company`),
  });

  companyBankAccounts = computed(() => this.bankAccountsResource.value() || []);
  isLoading = computed(() => this.profileResource.isLoading());

  ngOnInit(): void {
    // rxResource handles loading automatically
  }

  loadProfile() {
    this.profileResource.reload();
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.stagedLogoFile.set(file);
      const reader = new FileReader();
      reader.onload = () => {
        this.stagedLogoPreviewUrl.set(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  }

  onVatDocSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.stagedVatDocFile.set(file);
      this.stagedVatDocFileName.set(file.name);
      this.form.patchValue({ vatDocumentUrl: null }); // Clear old URL if replacing
    }
  }

  removeLogo() {
    this.stagedLogoFile.set(null);
    this.stagedLogoPreviewUrl.set(null);
    this.form.patchValue({ logoUrl: null });
  }

  removeVatDoc() {
    this.stagedVatDocFile.set(null);
    this.stagedVatDocFileName.set(null);
    this.form.patchValue({ vatDocumentUrl: null });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    const uploads: { [key: string]: Observable<any> } = {};

    const logoFile = this.stagedLogoFile();
    if (logoFile) {
      const logoData = new FormData();
      logoData.append('file', logoFile);
      uploads['logo'] = this.http.post<any>(`${environment.apiUrl}/upload/company-logo`, logoData);
    }

    const vatFile = this.stagedVatDocFile();
    if (vatFile) {
      const vatData = new FormData();
      vatData.append('file', vatFile);
      uploads['vatDoc'] = this.http.post<any>(`${environment.apiUrl}/upload/company-logo`, vatData);
    }

    const runSave = (logoUrl: string | null, vatUrl: string | null) => {
      const payload = { ...this.form.value };
      if (logoUrl !== null) payload.logoUrl = logoUrl;
      if (vatUrl !== null) payload.vatDocumentUrl = vatUrl;

      this.http
        .put(`${environment.apiUrl}/branches/${this.branchId}`, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.swal.success('บันทึกข้อมูลสำเร็จ');
            this.stagedLogoFile.set(null);
            this.stagedLogoPreviewUrl.set(null);
            this.stagedVatDocFile.set(null);
            this.stagedVatDocFileName.set(null);
            this.loadProfile();
            this.isSaving.set(false);
          },
          error: (err) => {
            console.error('Save failed', err);
            this.swal.error('บันทึกข้อมูลล้มเหลว');
            this.isSaving.set(false);
          },
        });
    };

    if (Object.keys(uploads).length > 0) {
      forkJoin(uploads)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res: any) => {
            const logoUrl = res.logo ? res.logo.imageUrl : null;
            const vatUrl = res.vatDoc ? res.vatDoc.imageUrl : null;
            runSave(logoUrl, vatUrl);
          },
          error: (err) => {
            console.error('Upload failed during save', err);
            this.swal.error('อัปโหลดไฟล์ล้มเหลว');
            this.isSaving.set(false);
          },
        });
    } else {
      runSave(null, null);
    }
  }

  loadBankAccounts() {
    this.bankAccountsResource.reload();
  }

  openAddBankForm() {
    this.editingBankId.set(null);
    this.bankForm.reset({
      bankName: '',
      accountNo: '',
      accountName: '',
      accountType: 'Business',
      isActive: true,
    });
    this.showBankForm.set(true);
  }

  openEditBankForm(bank: CompanyBankAccount) {
    this.editingBankId.set(bank.id);
    this.bankForm.patchValue({
      bankName: bank.bankName,
      accountNo: bank.accountNo,
      accountName: bank.accountName,
      accountType: bank.accountType,
      isActive: bank.isActive,
    });
    this.showBankForm.set(true);
  }

  saveBankAccount() {
    if (this.bankForm.invalid) {
      this.bankForm.markAllAsTouched();
      return;
    }

    const payload = this.bankForm.value;
    const editingId = this.editingBankId();

    if (editingId !== null) {
      this.http
        .put(`${environment.apiUrl}/bank-accounts/company/${editingId}`, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.swal.success('แก้ไขบัญชีธนาคารสำเร็จ');
            this.showBankForm.set(false);
            this.editingBankId.set(null);
            this.loadBankAccounts();
          },
          error: (err) => {
            console.error('Failed to update bank account', err);
            this.swal.error(err.error?.message || 'แก้ไขบัญชีธนาคารล้มเหลว');
          },
        });
    } else {
      this.http
        .post(`${environment.apiUrl}/bank-accounts/company`, payload)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.swal.success('เพิ่มบัญชีธนาคารสำเร็จ');
            this.showBankForm.set(false);
            this.loadBankAccounts();
          },
          error: (err) => {
            console.error('Failed to create bank account', err);
            this.swal.error(err.error?.message || 'เพิ่มบัญชีธนาคารล้มเหลว');
          },
        });
    }
  }

  deleteBankAccount(id: number) {
    this.swal.confirm('ยืนยันการลบบัญชีธนาคารนี้หรือไม่?', 'ลบ', 'ยกเลิก').then((result) => {
      if (result.isConfirmed) {
        this.http
          .delete(`${environment.apiUrl}/bank-accounts/company/${id}`)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.swal.success('ลบบัญชีธนาคารสำเร็จ');
              this.loadBankAccounts();
            },
            error: (err) => {
              console.error('Failed to delete bank account', err);
              this.swal.error('ลบบัญชีธนาคารล้มเหลว');
            },
          });
      }
    });
  }

  getBankInfo(bankName: string): Bank | null {
    if (!bankName) return null;
    return bankLists[bankName] || null;
  }
}
