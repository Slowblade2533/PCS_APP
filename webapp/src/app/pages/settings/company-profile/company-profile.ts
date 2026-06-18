import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './company-profile.html',
})
export class CompanyProfile implements OnInit {
  private destroyRef = inject(DestroyRef);
  private fb = inject(FormBuilder);
  private http = inject(HttpClient);
  private swal = inject(SweetAlertService);

  isLoading = signal(false);
  isSaving = signal(false);

  stagedLogoFile: File | null = null;
  stagedLogoPreviewUrl = signal<string | null>(null);

  stagedVatDocFile: File | null = null;
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
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  loadProfile() {
    this.isLoading.set(true);
    this.http
      .get<any>(`${environment.apiUrl}/branches/${this.branchId}`)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.form.patchValue({
            branchCode: res.branchCode || '00000',
            branchName: res.branchName,
            registrationName: res.registrationName,
            entityType: res.entityType || 'Individual',
            companyType: res.companyType || 'บุคคลธรรมดา',
            isVatRegistered: res.isVatRegistered || false,
            vatDocumentUrl: res.vatDocumentUrl || null,
            taxId: res.taxId,
            address: res.address,
            phone: res.phone,
            email: res.email,
            logoUrl: res.logoUrl,
          });
          
          this.stagedLogoFile = null;
          this.stagedLogoPreviewUrl.set(null);
          this.stagedVatDocFile = null;
          this.stagedVatDocFileName.set(null);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('Failed to load company profile', err);
          this.isLoading.set(false);
        },
      });
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.stagedLogoFile = file;
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
      this.stagedVatDocFile = file;
      this.stagedVatDocFileName.set(file.name);
      this.form.patchValue({ vatDocumentUrl: null }); // Clear old URL if replacing
    }
  }

  removeLogo() {
    this.stagedLogoFile = null;
    this.stagedLogoPreviewUrl.set(null);
    this.form.patchValue({ logoUrl: null });
  }

  removeVatDoc() {
    this.stagedVatDocFile = null;
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

    if (this.stagedLogoFile) {
      const logoData = new FormData();
      logoData.append('file', this.stagedLogoFile);
      uploads['logo'] = this.http.post<any>(`${environment.apiUrl}/upload/company-logo`, logoData);
    }

    if (this.stagedVatDocFile) {
      const vatData = new FormData();
      vatData.append('file', this.stagedVatDocFile);
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
            this.stagedLogoFile = null;
            this.stagedLogoPreviewUrl.set(null);
            this.stagedVatDocFile = null;
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
          }
        });
    } else {
      runSave(null, null);
    }
  }
}
