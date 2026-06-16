import { CommonModule, Location } from '@angular/common';
import Big from 'big.js';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, finalize } from 'rxjs/operators';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { VcbOrder, VcbOrderSearch } from '../../../shared/models/procurement.models';
import { ProcurementService } from '../../../shared/services/procurement.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { environment } from '../../../../environments/environment';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';

@Component({
  selector: 'app-vcb-deliveries-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ImageHoverPreview],
  templateUrl: './vcb-deliveries-create.html',
})
export class VcbDeliveriesCreateComponent implements OnInit, HasUnsavedChanges {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  apiOrigin = environment.apiUrl.replace('/api', '');
  private readonly procurementService = inject(ProcurementService);
  private readonly router = inject(Router);
  private readonly swal = inject(SweetAlertService);
  private readonly route = inject(ActivatedRoute);
  public readonly location = inject(Location);

  isEditMode = false;
  deliveryId: number | null = null;

  isSearching = false;
  isSubmitting = false;
  searchResults: VcbOrder[] = [];
  searchSubject = new Subject<string>();
  
  selectedOrders: VcbOrder[] = [];
  deliveryForm!: FormGroup;
  selectedSlipFile: File | null = null;
  slipFilePreviewUrl: string | null = null;

  searchParams: VcbOrderSearch = {
    page: 1,
    pageSize: 5,
    searchTerm: '',
  };
  totalCount = 0;

  addressHistory: string[] = [];
  readonly defaultAddress = '49/61 หมู่บ้านพรบดินทร ซอยนวมินทร์ 163 แยก 17-5 นวลจันทร์ เขตบึงกุ่ม กรุงเทพมหานคร 10240';

  get items(): FormArray {
    return this.deliveryForm.get('items') as FormArray;
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  ngOnInit(): void {
    this.loadAddressHistory();
    this.initForm();
    this.setupOrderSearch();
    this.loadOrders();

    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.isEditMode = true;
        this.deliveryId = +id;
        this.loadDeliveryData(this.deliveryId);
      }
    });
  }

  loadDeliveryData(id: number): void {
    this.procurementService.getVcbDeliveryById(id).subscribe({
      next: (res: any) => {
        const delivery = res.value || res.data || res;
        if (delivery) {
          this.deliveryForm.patchValue({
            deliveryNo: delivery.deliveryNo,
            orderDate: this.formatDate(new Date(delivery.orderDate)),
            shippingAddress: delivery.shippingAddress,
            domesticShippingCompany: delivery.domesticShippingCompany,
            totalAmountBeforeDiscount: Number(delivery.totalAmountBeforeDiscount ?? 0).toFixed(2),
            discountAmount: Number(delivery.discountAmount ?? 0).toFixed(2),
            totalAmount: Number(delivery.totalAmount ?? 0).toFixed(2),
            transferredAmount: Number(delivery.transferredAmount ?? 0).toFixed(2),
            notes: delivery.notes,
          });

          if (delivery.transferSlipUrl) {
            this.slipFilePreviewUrl = this.apiOrigin + delivery.transferSlipUrl;
          } else {
            this.slipFilePreviewUrl = null;
          }

          if (delivery.items && delivery.items.length > 0) {
            this.items.clear();
            delivery.items.forEach((item: any) => {
              const itemForm = this.fb.group({
                packageBoxNo: [item.packageBoxNo, Validators.required],
                domesticTrackingNo: [item.domesticTrackingNo],
                totalWeight: [Number(item.totalWeight ?? 0).toFixed(2), [Validators.required, Validators.min(0.01)]],
                boxDimensions: [item.boxDimensions],
                shippingCost: [Number(item.shippingCost ?? 0).toFixed(2), [Validators.required, Validators.min(0)]],
                subBoxes: this.fb.array([]),
              });

              if (item.containedBoxNumbers) {
                try {
                  const subBoxes = JSON.parse(item.containedBoxNumbers);
                  if (Array.isArray(subBoxes)) {
                    const subBoxesForm = itemForm.get('subBoxes') as FormArray;
                    subBoxes.forEach((sub: any) => {
                      subBoxesForm.push(this.fb.group({
                        boxNo: [sub.boxNo, Validators.required],
                        dimensions: [sub.dimensions || ''],
                        weight: [Number(sub.weight ?? 0).toFixed(2), [Validators.min(0)]]
                      }));
                    });
                  }
                } catch (e) {}
              }
              this.items.push(itemForm);
            });
          }

          if (delivery.vcbOrders && Array.isArray(delivery.vcbOrders)) {
            this.selectedOrders = delivery.vcbOrders.map((o: any) => ({
              id: o.orderId || o.id,
              orderNo: o.orderNo
            }));
          } else if (delivery.orders && Array.isArray(delivery.orders)) {
             this.selectedOrders = delivery.orders.map((o: any) => ({
              id: o.orderId || o.id,
              orderNo: o.orderNo
             }));
          }

          this.deliveryForm.markAsPristine();
          this.cdr.markForCheck();
        }
      },
      error: () => {
        this.swal.error('ไม่สามารถโหลดข้อมูลใบส่งสินค้าได้');
        this.location.back();
      }
    });
  }

  hasUnsavedChanges(): boolean {
    return this.deliveryForm.dirty && !this.isSubmitting;
  }

  private formatDate(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
  }

  private loadAddressHistory(): void {
    const history = localStorage.getItem('vcbDelivery_addressHistory');
    if (history) {
      try {
        this.addressHistory = JSON.parse(history);
      } catch {
        this.addressHistory = [this.defaultAddress];
      }
    } else {
      this.addressHistory = [this.defaultAddress];
    }
  }

  private saveAddressHistory(address: string): void {
    if (!address) return;
    const index = this.addressHistory.indexOf(address);
    if (index > -1) {
      this.addressHistory.splice(index, 1);
    }
    this.addressHistory.unshift(address);
    if (this.addressHistory.length > 10) {
      this.addressHistory.pop();
    }
    localStorage.setItem('vcbDelivery_addressHistory', JSON.stringify(this.addressHistory));
  }

  private initForm(): void {
    const initialAddress = this.addressHistory.length > 0 ? this.addressHistory[0] : this.defaultAddress;

    this.deliveryForm = this.fb.group({
      deliveryNo: ['', Validators.required],
      orderDate: [this.formatDate(new Date()), Validators.required],
      shippingAddress: [initialAddress, Validators.required],
      domesticShippingCompany: ['Sixth Party Logistics (6PT)', Validators.required],
      totalAmountBeforeDiscount: ['0.00', [Validators.required, Validators.min(0)]],
      discountAmount: ['0.00', [Validators.required, Validators.min(0)]],
      totalAmount: ['0.00', [Validators.required, Validators.min(0)]],
      transferredAmount: ['0.00', [Validators.required, Validators.min(0)]],
      notes: [''],
      items: this.fb.array([], Validators.required),
    });

    // Auto-calculate total and totalWeight of parent items from subBoxes
    this.deliveryForm.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(val => {
      const items = val.items || [];
      
      // Auto-calculate totalWeight of each package item if it has subBoxes
      let weightChanged = false;
      this.items.controls.forEach((itemControl) => {
        const subBoxesArray = itemControl.get('subBoxes') as FormArray;
        if (subBoxesArray && subBoxesArray.length > 0) {
          const subBoxesSum = subBoxesArray.controls.reduce((sum: Big, subBoxControl) => {
            const w = subBoxControl.get('weight')?.value || 0;
            return sum.plus(new Big(w || 0));
          }, new Big(0)).toFixed(2);

          const currentTotalWeight = itemControl.get('totalWeight')?.value;
          if (currentTotalWeight !== subBoxesSum) {
            itemControl.get('totalWeight')?.setValue(subBoxesSum, { emitEvent: false });
            weightChanged = true;
          }
        }
      });

      const totalBeforeDiscountBig = items.reduce((sum: Big, item: any) => {
        return sum.plus(new Big(item.shippingCost || 0));
      }, new Big(0));
      const totalBeforeDiscount = totalBeforeDiscountBig.toFixed(2);
      
      if (this.deliveryForm.get('totalAmountBeforeDiscount')?.value !== totalBeforeDiscount) {
         this.deliveryForm.patchValue({ totalAmountBeforeDiscount: totalBeforeDiscount }, { emitEvent: false });
      }

      const discountBig = new Big(val.discountAmount || 0);
      const totalBig = totalBeforeDiscountBig.minus(discountBig);
      const total = totalBig.gt(0) ? totalBig.toFixed(2) : '0.00';
      if (this.deliveryForm.get('totalAmount')?.value !== total) {
         this.deliveryForm.patchValue({ totalAmount: total }, { emitEvent: false });
      }

      if (weightChanged) {
        this.cdr.markForCheck();
      }
    });
  }

  addItem(): void {
    const itemForm = this.fb.group({
      packageBoxNo: ['', Validators.required],
      domesticTrackingNo: [''],
      totalWeight: ['0.00', [Validators.required, Validators.min(0.01)]],
      boxDimensions: [''],
      shippingCost: ['0.00', [Validators.required, Validators.min(0)]],
      subBoxes: this.fb.array([]),
    });
    this.items.push(itemForm);
  }

  getSubBoxes(itemIndex: number): FormArray {
    return this.items.at(itemIndex).get('subBoxes') as FormArray;
  }

  addSubBox(itemIndex: number): void {
    const subBoxForm = this.fb.group({
      boxNo: ['', Validators.required],
      dimensions: [''],
      weight: ['0.00', [Validators.min(0)]]
    });
    this.getSubBoxes(itemIndex).push(subBoxForm);
  }

  formatFinancial(controlName: string, index?: number): void {
    if (index !== undefined) {
      const itemControl = this.items.at(index);
      const control = itemControl.get(controlName);
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: true });
      }
    } else {
      const control = this.deliveryForm.get(controlName);
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: true });
      }
    }
  }

  formatWeight(index: number, subIndex?: number): void {
    if (subIndex !== undefined) {
      const subBoxes = this.getSubBoxes(index);
      const control = subBoxes.at(subIndex).get('weight');
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: true });
      }
    } else {
      const control = this.items.at(index).get('totalWeight');
      if (control) {
        const val = control.value;
        const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
        control.setValue(num.toFixed(2), { emitEvent: true });
      }
    }
  }

  removeSubBox(itemIndex: number, subIndex: number): void {
    this.getSubBoxes(itemIndex).removeAt(subIndex);
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
  }

  onFileChange(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedSlipFile = file;
      const reader = new FileReader();
      reader.onload = () => {
        this.slipFilePreviewUrl = reader.result as string;
        this.cdr.markForCheck();
      };
      reader.readAsDataURL(file);
    }
  }

  clearFile(): void {
    this.selectedSlipFile = null;
    this.slipFilePreviewUrl = null;
    const fileInput = document.getElementById('slipFile') as HTMLInputElement;
    if (fileInput) fileInput.value = '';
  }

  loadOrders(): void {
    this.isSearching = true;
    this.procurementService
      .getVcbOrders(this.searchParams)
      .pipe(
        finalize(() => {
          this.isSearching = false;
          this.cdr.markForCheck();
        }),
      )
      .subscribe((res: any) => {
        this.searchResults = res?.items || [];
        this.totalCount = res?.totalCount || 0;
      });
  }

  onPageChange(page: number): void {
    this.searchParams.page = page;
    this.loadOrders();
  }

  onSearchChange(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.searchSubject.next(term);
  }

  private setupOrderSearch(): void {
    this.searchSubject
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchParams.searchTerm = term;
        this.searchParams.page = 1;
        this.loadOrders();
      });
  }

  selectOrder(order: VcbOrder): void {
    if (!this.selectedOrders.find((o) => o.id === order.id)) {
      this.selectedOrders.push(order);
    }
  }

  removeOrder(orderId: number): void {
    this.selectedOrders = this.selectedOrders.filter((o) => o.id !== orderId);
  }

  onSubmit(): void {
    if (this.deliveryForm.invalid) {
      this.deliveryForm.markAllAsTouched();
      this.swal.warning('กรุณากรอกข้อมูลให้ครบถ้วน');
      return;
    }

    if (this.selectedOrders.length === 0) {
      this.swal.warning('ต้องเลือกใบสั่งซื้ออย่างน้อย 1 รายการ');
      return;
    }

    if (this.items.length === 0) {
      this.swal.warning('ต้องมีรายการกล่องอย่างน้อย 1 รายการ');
      return;
    }

    const formValue = this.deliveryForm.getRawValue();
    this.saveAddressHistory(formValue.shippingAddress);

    const formData = new FormData();
    
    const dto = {
      deliveryNo: formValue.deliveryNo,
      orderDate: new Date(formValue.orderDate).toISOString(),
      shippingAddress: formValue.shippingAddress,
      domesticShippingCompany: formValue.domesticShippingCompany,
      totalAmountBeforeDiscount: Number(formValue.totalAmountBeforeDiscount || 0).toFixed(2),
      discountAmount: Number(formValue.discountAmount || 0).toFixed(2),
      totalAmount: Number(formValue.totalAmount || 0).toFixed(2),
      transferredAmount: Number(formValue.transferredAmount || 0).toFixed(2),
      notes: formValue.notes,
      orderIds: this.selectedOrders.map((o) => o.id),
      items: formValue.items.map((item: any) => ({
        packageBoxNo: item.packageBoxNo,
        domesticTrackingNo: item.domesticTrackingNo,
        totalWeight: Number(item.totalWeight || 0).toFixed(2),
        boxDimensions: item.boxDimensions,
        containedBoxNumbers: item.subBoxes && item.subBoxes.length > 0 ? JSON.stringify(item.subBoxes.map((sub: any) => ({
          boxNo: sub.boxNo,
          dimensions: sub.dimensions || '',
          weight: Number(sub.weight || 0).toFixed(2)
        }))) : null,
        shippingCost: Number(item.shippingCost || 0).toFixed(2)
      }))
    };

    formData.append('data', JSON.stringify(dto));

    if (this.selectedSlipFile) {
      formData.append('slipFile', this.selectedSlipFile);
    }

    this.isSubmitting = true;
    
    const request = this.isEditMode && this.deliveryId
      ? this.procurementService.updateVcbDelivery(this.deliveryId, formData)
      : this.procurementService.createVcbDelivery(formData);

    request.subscribe({
      next: (res: any) => {
        this.isSubmitting = false;
        this.cdr.markForCheck();
        if (res.isSuccess || res.id || res.message) {
          this.deliveryForm.markAsPristine();
          this.swal.success(this.isEditMode ? 'อัปเดตใบส่งสินค้าสำเร็จ' : 'สร้างใบส่งสินค้าสำเร็จ').then(() => {
            this.router.navigate(['/procurement/vcb-deliveries']);
          });
        } else {
          this.swal.error(res.error || 'เกิดข้อผิดพลาด');
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        this.cdr.markForCheck();
        this.swal.error(err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ');
      },
    });
  }

  getShippingLogo(company: string | undefined): string | null {
    if (!company) return null;
    const cmp = company.trim().toLowerCase();
    if (cmp.includes('sixth party')) {
      return '/logistics/sixth_party.jpg';
    } else if (cmp.includes('nim express') || cmp.includes('นิ่มซี่เส็ง')) {
      return '/logistics/nim_express.jpg';
    } else if (cmp.includes('thaipost') || cmp.includes('ไปรษณีย์ไทย')) {
      return '/logistics/thaipost_ems.jpg';
    } else if (cmp.includes('blue & white') || cmp.includes('blue &amp; white') || cmp.includes('blue and white') || cmp.includes('blue & white logistic')) {
      return '/logistics/blue_n_white.jpg';
    } else if (cmp.includes('dhl')) {
      return '/logistics/dhl.jpg';
    }
    return null;
  }
}
