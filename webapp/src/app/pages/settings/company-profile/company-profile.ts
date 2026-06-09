import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './company-profile.html',
})
export class CompanyProfile implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);
  
  form: FormGroup;
  isLoading = signal(false);
  isSaving = signal(false);
  isUploadingLogo = signal(false);
  isUploadingVatDoc = signal(false);
  
  // We will assume branch ID = 1 is the main branch for now
  branchId = 1;
  apiOrigin = environment.apiUrl;

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
    this.http.get<any>(`${environment.apiUrl}/branches/${this.branchId}`).subscribe({
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
      }
    });
  }

  onFileSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);
      
      this.isUploadingLogo.set(true);
      this.http.post<any>(`${environment.apiUrl}/upload/company-logo`, formData).subscribe({
        next: (res) => {
          this.form.patchValue({ logoUrl: res.imageUrl });
          this.isUploadingLogo.set(false);
        },
        error: (err) => {
          console.error('Upload failed', err);
          alert('อัปโหลดรูปล้มเหลว');
          this.isUploadingLogo.set(false);
        }
      });
    }
  }

  onVatDocSelected(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      const formData = new FormData();
      formData.append('file', file);
      
      this.isUploadingVatDoc.set(true);
      this.http.post<any>(`${environment.apiUrl}/upload/company-logo`, formData).subscribe({
        next: (res) => {
          this.form.patchValue({ vatDocumentUrl: res.imageUrl });
          this.isUploadingVatDoc.set(false);
        },
        error: (err) => {
          console.error('Upload failed', err);
          alert('อัปโหลดเอกสารล้มเหลว');
          this.isUploadingVatDoc.set(false);
        }
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

    this.http.put(`${environment.apiUrl}/branches/${this.branchId}`, payload).subscribe({
      next: () => {
        alert('บันทึกข้อมูลสำเร็จ');
        this.isSaving.set(false);
      },
      error: (err) => {
        console.error('Save failed', err);
        alert('บันทึกข้อมูลล้มเหลว');
        this.isSaving.set(false);
      }
    });
  }
}
