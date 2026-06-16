import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
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
  isUploadingLogo = signal(false);
  isUploadingVatDoc = signal(false);

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
      const formData = new FormData();
      formData.append('file', file);

      this.isUploadingLogo.set(true);
      this.http
        .post<any>(`${environment.apiUrl}/upload/company-logo`, formData)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.form.patchValue({ logoUrl: res.imageUrl });
            this.isUploadingLogo.set(false);
          },
          error: (err) => {
            console.error('Upload failed', err);
            this.swal.error('อัปโหลดรูปล้มเหลว');
            this.isUploadingLogo.set(false);
          },
        });
    }
  }

  onVatDocSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);

      this.isUploadingVatDoc.set(true);
      this.http
        .post<any>(`${environment.apiUrl}/upload/company-logo`, formData)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (res) => {
            this.form.patchValue({ vatDocumentUrl: res.imageUrl });
            this.isUploadingVatDoc.set(false);
          },
          error: (err) => {
            console.error('Upload failed', err);
            this.swal.error('อัปโหลดเอกสารล้มเหลว');
            this.isUploadingVatDoc.set(false);
          },
        });
    }
  }

  removeLogo() {
    this.form.patchValue({ logoUrl: null });
  }

  removeVatDoc() {
    this.form.patchValue({ vatDocumentUrl: null });
  }

  save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const payload = this.form.value;

    this.http
      .put(`${environment.apiUrl}/branches/${this.branchId}`, payload)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.swal.success('บันทึกข้อมูลสำเร็จ');
          this.isSaving.set(false);
        },
        error: (err) => {
          console.error('Save failed', err);
          this.swal.error('บันทึกข้อมูลล้มเหลว');
          this.isSaving.set(false);
        },
      });
  }
}
