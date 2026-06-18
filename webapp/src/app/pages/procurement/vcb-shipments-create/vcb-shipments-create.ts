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
import { Subject, forkJoin } from 'rxjs';
import { debounceTime, finalize } from 'rxjs/operators';
import { environment } from '../../../../environments/environment';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import {
  VcbOrder,
  VcbOrderItem,
  VcbDelivery,
  VcbDeliverySearch,
  VcbShipmentCreate,
} from '../../../shared/models/procurement.models';
import { ProcurementService } from '../../../shared/services/procurement.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-vcb-shipments-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './vcb-shipments-create.html',
})
export class VcbShipmentsCreateComponent implements OnInit, HasUnsavedChanges {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly procurementService = inject(ProcurementService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly swal = inject(SweetAlertService);
  public readonly location = inject(Location);
  public readonly apiOrigin = environment.apiUrl.replace('/api', '');

  isEditMode = false;
  shipmentId: number | null = null;
  isSearching = false;
  isSubmitting = false;
  searchResults: VcbDelivery[] = [];
  searchSubject = new Subject<string>();
  selectedDelivery: VcbDelivery | null = null;
  availableOrderItems: VcbOrderItem[] = [];
  availableSubBoxes: string[] = [];
  shipmentForm!: FormGroup;

  searchParams: VcbDeliverySearch = {
    page: 1,
    pageSize: 5,
    searchTerm: '',
    status: 'Shipping',
  };
  totalCount = 0;

  get items(): FormArray {
    return this.shipmentForm.get('items') as FormArray;
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: any): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  ngOnInit(): void {
    this.initForm();
    this.setupOrderSearch();
    
    this.route.paramMap.subscribe(params => {
      const idStr = params.get('id');
      if (idStr) {
        const id = parseInt(idStr, 10);
        if (!isNaN(id)) {
          this.isEditMode = true;
          this.shipmentId = id;
          this.loadShipmentForEdit(id);
        }
      } else {
        this.loadDeliveries();
      }
    });
  }

  loadShipmentForEdit(id: number): void {
    this.isSearching = true;
    this.procurementService.getVcbShipmentById(id).subscribe({
      next: (res: any) => {
        const shipment = res.value || res.data || res;
        if (shipment) {
          this.shipmentForm.patchValue({
            deliveryId: shipment.deliveryId,
            notes: shipment.notes,
            isForceCloseOrder: shipment.isForceCloseOrder
          });

          this.procurementService.getVcbDeliveryById(shipment.deliveryId).subscribe({
            next: (deliveryRes: any) => {
              const fullDelivery = deliveryRes.value || deliveryRes.data || deliveryRes;
              if (fullDelivery) {
                this.selectedDelivery = fullDelivery;
                
                this.availableSubBoxes = [];
                if (fullDelivery.items) {
                  const receivedSet = new Set<string>(fullDelivery.receivedBoxNumbers || []);
                  fullDelivery.items.forEach((dItem: any) => {
                    if (dItem.containedBoxNumbers) {
                      try {
                        const subBoxes = JSON.parse(dItem.containedBoxNumbers);
                        if (Array.isArray(subBoxes)) {
                          subBoxes.forEach((sub: any) => {
                            if (sub.boxNo && !this.availableSubBoxes.includes(sub.boxNo)) {
                              const isRec = receivedSet.has(sub.boxNo);
                              const isCurrentShipmentBox = shipment.items.some((si: any) => 
                                si.boxNumbers && si.boxNumbers.split(',').map((b: string) => b.trim()).includes(sub.boxNo)
                              );
                              if (!isRec || isCurrentShipmentBox) {
                                this.availableSubBoxes.push(sub.boxNo);
                              }
                            }
                          });
                        }
                      } catch (e) {
                        if (!this.availableSubBoxes.includes(dItem.containedBoxNumbers)) {
                          const isRec = receivedSet.has(dItem.containedBoxNumbers);
                          const isCurrentShipmentBox = shipment.items.some((si: any) => 
                            si.boxNumbers && si.boxNumbers.split(',').map((b: string) => b.trim()).includes(dItem.containedBoxNumbers)
                          );
                          if (!isRec || isCurrentShipmentBox) {
                            this.availableSubBoxes.push(dItem.containedBoxNumbers);
                          }
                        }
                      }
                    }
                  });
                }

                if (fullDelivery.orders && fullDelivery.orders.length > 0) {
                  const orderRequests = fullDelivery.orders.map((o: any) =>
                    this.procurementService.getVcbOrderById(o.orderId)
                  );
                  forkJoin(orderRequests).subscribe({
                    next: (orderResponses: any) => {
                      this.availableOrderItems = [];
                      orderResponses.forEach((orderRes: any) => {
                        const order = orderRes.value || orderRes.data || orderRes;
                        if (order && order.items) {
                          order.items.forEach((item: any) => {
                            this.availableOrderItems.push(item);
                          });
                        }
                      });

                      this.items.clear();
                      shipment.items.forEach((sItem: any) => {
                        const matchingOrderItem = this.availableOrderItems.find(aoi => aoi.id === sItem.orderItemId);
                        const expectedQty = matchingOrderItem 
                          ? (matchingOrderItem.remainingQuantity ?? matchingOrderItem.quantity)
                          : sItem.expectedQuantity;

                        const itemForm = this.fb.group({
                          orderItemId: [sItem.orderItemId, Validators.required],
                          variantId: [sItem.variantId, Validators.required],
                          sku: [sItem.sku],
                          productName: [sItem.productName],
                          variantName: [sItem.variantName],
                          imageUrl: [sItem.imageUrl],
                          expectedQuantity: [expectedQty],
                          boxNumbers: [sItem.boxNumbers, Validators.required],
                          receiptStatus: [sItem.receiptStatus, Validators.required],
                          goodQuantity: [sItem.goodQuantity, [Validators.required, Validators.min(0)]],
                          defectiveQuantity: [sItem.defectiveQuantity, [Validators.required, Validators.min(0)]],
                          refundAmount: [Number(sItem.refundAmount || 0).toFixed(2), [Validators.min(0)]],
                        });

                        if (sItem.receiptStatus === 'WrongItem') {
                          itemForm.patchValue({ goodQuantity: 0, defectiveQuantity: 0 }, { emitEvent: false });
                          itemForm.get('goodQuantity')?.disable();
                          itemForm.get('defectiveQuantity')?.disable();
                        }

                        itemForm.get('receiptStatus')?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((status) => {
                          if (status === 'WrongItem') {
                            itemForm.patchValue({ goodQuantity: 0, defectiveQuantity: 0 }, { emitEvent: false });
                            itemForm.get('goodQuantity')?.disable();
                            itemForm.get('defectiveQuantity')?.disable();
                          } else {
                            itemForm.get('goodQuantity')?.enable();
                            itemForm.get('defectiveQuantity')?.enable();
                          }
                        });

                        this.items.push(itemForm);
                      });

                      this.isSearching = false;
                      this.cdr.markForCheck();
                    },
                    error: () => {
                      this.isSearching = false;
                      this.cdr.markForCheck();
                    }
                  });
                } else {
                  this.isSearching = false;
                  this.cdr.markForCheck();
                }
              } else {
                this.isSearching = false;
                this.cdr.markForCheck();
              }
            },
            error: () => {
              this.isSearching = false;
              this.cdr.markForCheck();
            }
          });
        } else {
          this.isSearching = false;
          this.cdr.markForCheck();
        }
      },
      error: () => {
        this.isSearching = false;
        this.cdr.markForCheck();
        this.swal.error('ไม่สามารถโหลดข้อมูลใบรับสินค้าได้');
      }
    });
  }

  addAllItems(): void {
    this.availableOrderItems
      .filter((item) => (item.remainingQuantity ?? item.quantity) > 0)
      .forEach((item) => this.addItemToShipment(item));
  }

  addItemToShipment(orderItem: VcbOrderItem): void {
    const expectedQty = orderItem.remainingQuantity ?? orderItem.quantity;
    const itemForm = this.fb.group({
      orderItemId: [orderItem.id, Validators.required],
      variantId: [orderItem.variantId, Validators.required],
      sku: [orderItem.sku],
      productName: [orderItem.productName],
      variantName: [orderItem.variantName],
      imageUrl: [orderItem.imageUrl],
      expectedQuantity: [expectedQty],
      boxNumbers: ['', Validators.required],
      receiptStatus: ['Complete', Validators.required],
      goodQuantity: [expectedQty, [Validators.required, Validators.min(0)]],
      defectiveQuantity: [0, [Validators.required, Validators.min(0)]],
      refundAmount: ['0.00', [Validators.min(0)]],
    });

    // Handle status change to reset values
    itemForm
      .get('receiptStatus')
      ?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        if (status === 'WrongItem') {
          itemForm.patchValue({ goodQuantity: 0, defectiveQuantity: 0 }, { emitEvent: false });
          itemForm.get('goodQuantity')?.disable();
          itemForm.get('defectiveQuantity')?.disable();
        } else {
          itemForm.get('goodQuantity')?.enable();
          itemForm.get('defectiveQuantity')?.enable();
        }
      });

    this.items.push(itemForm);
  }

  hasUnsavedChanges(): boolean {
    return this.shipmentForm.dirty && !this.isSubmitting;
  }

  loadDeliveries(): void {
    this.isSearching = true;
    this.procurementService
      .getVcbDeliveries(this.searchParams)
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
    this.loadDeliveries();
  }

  onSearchChange(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this.searchSubject.next(term);
  }

  onSubmit(): void {
    if (this.shipmentForm.invalid) {
      this.shipmentForm.markAllAsTouched();
      this.swal.warning('กรุณากรอกข้อมูลให้ครบถ้วน');
      return;
    }

    if (this.items.length === 0) {
      this.swal.warning('ต้องมีรายการพัสดุอย่างน้อย 1 รายการ');
      return;
    }

    if (!this.validateItemQuantities()) {
      return;
    }

    const formValue = this.shipmentForm.getRawValue();

    if (formValue.isForceCloseOrder) {
      let totalRefund = new Big(0);
      let hasMissing = false;
      for (let i = 0; i < this.items.length; i++) {
        const item = this.items.at(i);
        const expected = item.get('expectedQuantity')?.value || 0;
        const received = (item.get('goodQuantity')?.value || 0) + (item.get('defectiveQuantity')?.value || 0);
        if (received < expected) hasMissing = true;
        totalRefund = totalRefund.plus(new Big(item.get('refundAmount')?.value || 0));
      }
      
      if (hasMissing && !formValue.notes && totalRefund.eq(0)) {
        this.swal.warning('กรณีสั่งปิดออเดอร์ทันทีโดยที่สินค้ารับไม่ครบ กรุณาระบุ "หมายเหตุ" หรือต้องมี "ยอดคืนเงิน"');
        return;
      }
    }

    this.isSubmitting = true;
    const dto: VcbShipmentCreate = {
      deliveryId: formValue.deliveryId,
      notes: formValue.notes,
      isForceCloseOrder: formValue.isForceCloseOrder,
      items: formValue.items.map((i: any) => ({
        orderItemId: i.orderItemId,
        variantId: i.variantId,
        boxNumbers: i.boxNumbers,
        receiptStatus: i.receiptStatus,
        expectedQuantity: i.expectedQuantity,
        goodQuantity: i.goodQuantity || 0,
        defectiveQuantity: i.defectiveQuantity || 0,
        refundAmount: Number(i.refundAmount || 0).toFixed(2),
      })),
    };

    if (this.isEditMode && this.shipmentId) {
      this.procurementService.updateVcbShipment(this.shipmentId, dto).subscribe({
        next: (res: any) => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          if (res.isSuccess) {
            this.shipmentForm.markAsPristine();
            this.swal.success('แก้ไขใบรับสินค้าสำเร็จ').then(() => {
              this.swal
                .confirm(
                  'คุณต้องการกดยืนยันการรับสินค้าเข้าสต็อกทันทีเลยหรือไม่?',
                  'ใช่, ยืนยันรับเข้า',
                  'เก็บไว้เป็นฉบับร่างก่อน',
                )
                .then((result) => {
                  if (result.isConfirmed) {
                    this.procurementService
                      .updateVcbShipmentStatus(this.shipmentId!, 'Completed')
                      .subscribe(() => {
                        this.swal.success('รับเข้าสต็อกเรียบร้อย');
                        this.router.navigate(['/procurement/vcb-shipments']);
                        this.cdr.markForCheck();
                      });
                  } else {
                    this.router.navigate(['/procurement/vcb-shipments']);
                  }
                });
            });
          } else {
            this.swal.error(res.error || 'เกิดข้อผิดพลาด');
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          this.swal.error(err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ');
        }
      });
    } else {
      this.procurementService.createVcbShipment(dto).subscribe({
        next: (res) => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          if (res.isSuccess) {
            this.shipmentForm.markAsPristine();
            this.swal
              .success('สร้างใบรับสินค้าสำเร็จ พร้อมนำส่งสถานะ Completed เพื่อตัดสต็อก')
              .then(() => {
                this.swal
                  .confirm(
                    'คุณต้องการกดยืนยันการรับสินค้าเข้าสต็อกทันทีเลยหรือไม่?',
                    'ใช่, ยืนยันรับเข้า',
                    'เก็บไว้เป็นฉบับร่างก่อน',
                  )
                  .then((result) => {
                    if (result.isConfirmed) {
                      const newId = (res.value || res.data)!;
                      this.procurementService
                        .updateVcbShipmentStatus(newId, 'Completed')
                        .subscribe(() => {
                          this.swal.success('รับเข้าสต็อกเรียบร้อย');
                          this.router.navigate(['/procurement/vcb-shipments']);
                          this.cdr.markForCheck();
                        });
                    } else {
                      this.router.navigate(['/procurement/vcb-shipments']);
                    }
                  });
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
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
  }

  formatFinancial(controlName: string, index: number): void {
    const itemControl = this.items.at(index);
    const control = itemControl.get(controlName);
    if (control) {
      const val = control.value;
      const num = val !== null && val !== undefined && val !== '' ? parseFloat(val) : 0;
      control.setValue(num.toFixed(2), { emitEvent: false });
    }
  }

  selectDelivery(delivery: VcbDelivery): void {
    this.isSearching = true;
    this.procurementService.getVcbDeliveryById(delivery.id).subscribe({
      next: (res: any) => {
        const fullDelivery = res.value || res.data || res;
        if (fullDelivery) {
          this.selectedDelivery = fullDelivery;
          this.shipmentForm.patchValue({ deliveryId: fullDelivery.id });
          this.items.clear();
          this.availableOrderItems = [];
          this.availableSubBoxes = [];

          if (fullDelivery.items) {
            const receivedSet = new Set<string>(fullDelivery.receivedBoxNumbers || []);
            fullDelivery.items.forEach((dItem: any) => {
              if (dItem.containedBoxNumbers) {
                try {
                  const subBoxes = JSON.parse(dItem.containedBoxNumbers);
                  if (Array.isArray(subBoxes)) {
                    subBoxes.forEach((sub: any) => {
                      if (sub.boxNo && !this.availableSubBoxes.includes(sub.boxNo) && !receivedSet.has(sub.boxNo)) {
                        this.availableSubBoxes.push(sub.boxNo);
                      }
                    });
                  }
                } catch (e) {
                  // Fallback for simple string if not JSON
                  if (!this.availableSubBoxes.includes(dItem.containedBoxNumbers) && !receivedSet.has(dItem.containedBoxNumbers)) {
                    this.availableSubBoxes.push(dItem.containedBoxNumbers);
                  }
                }
              }
            });
          }

          if (fullDelivery.orders && fullDelivery.orders.length > 0) {
            const orderRequests = fullDelivery.orders.map((o: any) =>
              this.procurementService.getVcbOrderById(o.orderId),
            );
            forkJoin(orderRequests).subscribe({
              next: (orderResponses: any) => {
                orderResponses.forEach((orderRes: any) => {
                  const order = orderRes.value || orderRes.data || orderRes;
                  if (order && order.items) {
                    order.items.forEach((item: any) => {
                      this.availableOrderItems.push(item);
                    });
                    order.items
                      .filter((item: any) => (item.remainingQuantity ?? item.quantity) > 0)
                      .forEach((item: any) => this.addItemToShipment(item));
                  }
                });
                this.isSearching = false;
                this.cdr.markForCheck();
              },
              error: () => {
                this.isSearching = false;
                this.cdr.markForCheck();
              },
            });
          } else {
            this.isSearching = false;
            this.cdr.markForCheck();
          }
        } else {
          this.isSearching = false;
          this.cdr.markForCheck();
        }
      },
      error: () => {
        this.isSearching = false;
        this.cdr.markForCheck();
      },
    });
  }

  getParsedSubBoxes(jsonStr: string | undefined): any[] {
    if (!jsonStr) return [];
    try {
      const parsed = JSON.parse(jsonStr);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  isBoxReceived(boxNo: string): boolean {
    if (!this.selectedDelivery || !this.selectedDelivery.receivedBoxNumbers) return false;
    return this.selectedDelivery.receivedBoxNumbers.includes(boxNo);
  }

  validateItemQuantities(): boolean {
    const grouped: { [key: number]: any[] } = {};
    for (let i = 0; i < this.items.length; i++) {
      const item = this.items.at(i);
      const orderItemId = item.get('orderItemId')?.value;
      if (orderItemId) {
        if (!grouped[orderItemId]) {
          grouped[orderItemId] = [];
        }
        grouped[orderItemId].push(item);
      }
    }

    for (const orderItemId in grouped) {
      const group = grouped[orderItemId];
      const firstItem = group[0];
      const sku = firstItem.get('sku')?.value || '';
      const productName = firstItem.get('productName')?.value || '';
      const status = firstItem.get('receiptStatus')?.value;
      const expected = firstItem.get('expectedQuantity')?.value || 0;

      let totalGood = 0;
      let totalDefective = 0;
      for (const item of group) {
        totalGood += item.get('goodQuantity')?.value || 0;
        totalDefective += item.get('defectiveQuantity')?.value || 0;
      }
      const total = totalGood + totalDefective;

      if (status === 'Complete' && total !== expected) {
        this.swal.warning(
          `สินค้า "${productName || sku}": สถานะ "รับครบ" แต่ยอดรวมทุกกล่องที่ได้รับ (${total}) ไม่เท่ากับจำนวนที่สั่ง (${expected})`,
        );
        return false;
      }
      if (status === 'Incomplete' && total >= expected) {
        this.swal.warning(
          `สินค้า "${productName || sku}": สถานะ "รับไม่ครบ" แต่ยอดรวมทุกกล่องที่ได้รับ (${total}) มีค่ามากกว่าหรือเท่ากับจำนวนที่สั่ง (${expected})`,
        );
        return false;
      }
      if (status === 'Over' && total <= expected) {
        this.swal.warning(
          `สินค้า "${productName || sku}": สถานะ "รับเกิน" แต่ยอดรวมทุกกล่องที่ได้รับ (${total}) มีค่าน้อยกว่าหรือเท่ากับจำนวนที่สั่ง (${expected})`,
        );
        return false;
      }
    }
    return true;
  }

  private formatDate(date: Date): string {
    const tzoffset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - tzoffset).toISOString().slice(0, 16);
  }

  private initForm(): void {
    this.shipmentForm = this.fb.group({
      deliveryId: [null, Validators.required],
      notes: [''],
      isForceCloseOrder: [false],
      items: this.fb.array([], Validators.required),
    });
  }

  private setupOrderSearch(): void {
    this.searchSubject
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe((term) => {
        this.searchParams.searchTerm = term;
        this.searchParams.page = 1;
        this.loadDeliveries();
      });
  }
}
