import { CommonModule, Location } from '@angular/common';
import Big from 'big.js';
import {
  Component,
  DestroyRef,
  HostListener,
  OnInit,
  inject,
  signal,
  computed,
  effect,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, forkJoin, of } from 'rxjs';
import { debounceTime, switchMap, catchError, map } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { HasUnsavedChanges } from '../../../shared/guards/has-unsaved-changes.interface';
import { VcbOrder, VcbOrderItem } from '../../../shared/models/vcb-orders.models';
import { VcbDelivery, VcbDeliverySearch } from '../../../shared/models/vcb-deliveries.models';
import { VcbShipmentCreate } from '../../../shared/models/vcb-shipments.models';
import { VcbDeliveriesService } from '../../../shared/services/vcb-deliveries.service';
import { VcbOrdersService } from '../../../shared/services/vcb-orders.service';
import { VcbShipmentsService } from '../../../shared/services/vcb-shipments.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';

@Component({
  selector: 'app-vcb-shipments-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PaginationComponent],
  templateUrl: './vcb-shipments-create.html',
})
export class VcbShipmentsCreateComponent implements OnInit, HasUnsavedChanges {
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);
  private readonly vcbDeliveriesService = inject(VcbDeliveriesService);
  private readonly vcbOrdersService = inject(VcbOrdersService);
  private readonly vcbShipmentsService = inject(VcbShipmentsService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly swal = inject(SweetAlertService);
  public readonly location = inject(Location);
  public readonly apiOrigin = environment.apiUrl.replace('/api', '');

  isEditMode = signal(false);
  shipmentId = signal<number | null>(null);
  selectedDeliveryId = signal<number | null>(null);

  isSubmitting = signal(false);
  searchSubject = new Subject<string>();
  selectedDelivery = signal<VcbDelivery | null>(null);
  availableOrderItems = signal<VcbOrderItem[]>([]);
  availableSubBoxes = signal<string[]>([]);
  shipmentForm!: FormGroup;

  searchParams = signal<VcbDeliverySearch>({
    page: 1,
    pageSize: 5,
    searchTerm: '',
    status: 'Shipping',
  });

  deliveriesResource = rxResource({
    params: () => this.searchParams(),
    stream: ({ params }) =>
      this.vcbDeliveriesService
        .getVcbDeliveries(params)
        .pipe(catchError(() => of({ items: [], totalCount: 0 }))),
  });

  searchResults = computed(() => this.deliveriesResource.value()?.items || []);
  totalCount = computed(() => this.deliveriesResource.value()?.totalCount || 0);

  shipmentDataResource = rxResource({
    params: () => this.shipmentId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.vcbShipmentsService.getVcbShipmentById(params).pipe(
        switchMap((res) => {
          const shipment: any = res.value || res.data || ((res as any).id ? res : undefined);
          if (!shipment) return of(null);
          return this.vcbDeliveriesService.getVcbDeliveryById(shipment.deliveryId).pipe(
            switchMap((deliveryRes) => {
              const delivery: any = deliveryRes.value || deliveryRes.data || ((deliveryRes as any).id ? deliveryRes : undefined);
              if (!delivery || !delivery.orders?.length)
                return of({ shipment, delivery, orderResponses: [] });
              const orderReqs = delivery.orders.map((o: any) =>
                this.vcbOrdersService.getVcbOrderById(o.orderId),
              );
              return forkJoin(orderReqs).pipe(
                map((orderResponses) => ({ shipment, delivery, orderResponses })),
              );
            }),
          );
        }),
        catchError(() => {
          this.swal.error('ไม่สามารถโหลดข้อมูลใบรับสินค้าได้');
          return of(null);
        }),
      );
    },
  });

  deliveryDataResource = rxResource({
    params: () => this.selectedDeliveryId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.vcbDeliveriesService.getVcbDeliveryById(params).pipe(
        switchMap((deliveryRes) => {
          const delivery: any = deliveryRes.value || deliveryRes.data || ((deliveryRes as any).id ? deliveryRes : undefined);
          if (!delivery || !delivery.orders?.length) return of({ delivery, orderResponses: [] });
          const orderReqs = delivery.orders.map((o: any) =>
            this.vcbOrdersService.getVcbOrderById(o.orderId),
          );
          return forkJoin(orderReqs).pipe(map((orderResponses) => ({ delivery, orderResponses })));
        }),
        catchError(() => of(null)),
      );
    },
  });

  isSearching = computed(
    () =>
      this.deliveriesResource.isLoading() ||
      this.shipmentDataResource.isLoading() ||
      this.deliveryDataResource.isLoading(),
  );

  get items(): FormArray {
    return this.shipmentForm.get('items') as FormArray;
  }

  @HostListener('window:beforeunload', ['$event'])
  unloadNotification($event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      $event.returnValue = true;
    }
  }

  constructor() {
    effect(() => {
      const data = this.shipmentDataResource.value();
      if (data && this.isEditMode()) {
        untracked(() => {
          this.applyEditData(data.shipment, data.delivery, data.orderResponses as any[]);
        });
      }
    });

    effect(() => {
      const data = this.deliveryDataResource.value();
      if (data && !this.isEditMode()) {
        untracked(() => {
          this.applyNewDeliveryData(data.delivery, data.orderResponses as any[]);
        });
      }
    });
  }

  ngOnInit(): void {
    this.initForm();
    this.setupOrderSearch();

    this.route.paramMap.subscribe((params) => {
      const idStr = params.get('id');
      if (idStr) {
        const id = parseInt(idStr, 10);
        if (!isNaN(id)) {
          this.isEditMode.set(true);
          this.shipmentId.set(id);
        }
      }
    });
  }

  applyEditData(shipment: any, fullDelivery: any, orderResponses: any[]): void {
    this.shipmentForm.patchValue({
      deliveryId: shipment.deliveryId,
      notes: shipment.notes,
      isForceCloseOrder: shipment.isForceCloseOrder,
    });

    if (fullDelivery) {
      this.selectedDelivery.set(fullDelivery);

      const tempSubBoxes: string[] = [];
      if (fullDelivery.items) {
        const receivedSet = new Set<string>(fullDelivery.receivedBoxNumbers || []);
        fullDelivery.items.forEach((dItem: any) => {
          if (dItem.containedBoxNumbers) {
            try {
              const subBoxes = JSON.parse(dItem.containedBoxNumbers);
              if (Array.isArray(subBoxes)) {
                subBoxes.forEach((sub: { boxNo: string }) => {
                  if (sub.boxNo && !tempSubBoxes.includes(sub.boxNo)) {
                    const isRec = receivedSet.has(sub.boxNo);
                    const isCurrentShipmentBox = shipment.items.some(
                      (si: any) =>
                        si.boxNumbers &&
                        si.boxNumbers
                          .split(',')
                          .map((b: string) => b.trim())
                          .includes(sub.boxNo),
                    );
                    if (!isRec || isCurrentShipmentBox) {
                      tempSubBoxes.push(sub.boxNo);
                    }
                  }
                });
              }
            } catch (e) {
              if (!tempSubBoxes.includes(dItem.containedBoxNumbers)) {
                const isRec = receivedSet.has(dItem.containedBoxNumbers);
                const isCurrentShipmentBox = shipment.items.some(
                  (si: any) =>
                    si.boxNumbers &&
                    si.boxNumbers
                      .split(',')
                      .map((b: string) => b.trim())
                      .includes(dItem.containedBoxNumbers || ''),
                );
                if (!isRec || isCurrentShipmentBox) {
                  tempSubBoxes.push(dItem.containedBoxNumbers);
                }
              }
            }
          }
        });
      }
      this.availableSubBoxes.set(tempSubBoxes);

      const tempOrderItems: VcbOrderItem[] = [];
      orderResponses.forEach((orderRes) => {
        const order = orderRes.value || orderRes.data;
        if (order && order.items) {
          order.items.forEach((item: any) => {
            tempOrderItems.push(item);
          });
        }
      });
      this.availableOrderItems.set(tempOrderItems);

      this.items.clear();
      shipment.items.forEach((sItem: any) => {
        const matchingOrderItem = tempOrderItems.find((aoi) => aoi.id === sItem.orderItemId);
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
      });
    }
  }

  addAllItems(): void {
    this.availableOrderItems()
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
    return this.shipmentForm.dirty && !this.isSubmitting();
  }

  // Removed loadDeliveries()

  onPageChange(page: number): void {
    this.searchParams.update((p) => ({ ...p, page }));
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
        const received =
          (item.get('goodQuantity')?.value || 0) + (item.get('defectiveQuantity')?.value || 0);
        if (received < expected) hasMissing = true;
        totalRefund = totalRefund.plus(new Big(item.get('refundAmount')?.value || 0));
      }

      if (hasMissing && !formValue.notes && totalRefund.eq(0)) {
        this.swal.warning(
          'กรณีสั่งปิดออเดอร์ทันทีโดยที่สินค้ารับไม่ครบ กรุณาระบุ "หมายเหตุ" หรือต้องมี "ยอดคืนเงิน"',
        );
        return;
      }
    }

    this.isSubmitting.set(true);
    const dto: VcbShipmentCreate = {
      deliveryId: formValue.deliveryId,
      notes: formValue.notes,
      isForceCloseOrder: formValue.isForceCloseOrder,
      items: formValue.items.map(
        (i: {
          orderItemId: number;
          variantId: number;
          boxNumbers: string;
          receiptStatus: string;
          expectedQuantity: number;
          goodQuantity: number;
          defectiveQuantity: number;
          refundAmount: string | number;
        }) => ({
          orderItemId: i.orderItemId,
          variantId: i.variantId,
          boxNumbers: i.boxNumbers,
          receiptStatus: i.receiptStatus,
          expectedQuantity: i.expectedQuantity,
          goodQuantity: i.goodQuantity || 0,
          defectiveQuantity: i.defectiveQuantity || 0,
          refundAmount: Number(i.refundAmount || 0).toFixed(2),
        }),
      ),
    };

    if (this.isEditMode() && this.shipmentId()) {
      this.vcbShipmentsService.updateVcbShipment(this.shipmentId()!, dto).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
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
                    this.vcbShipmentsService
                      .updateVcbShipmentStatus(this.shipmentId()!, 'Completed')
                      .subscribe(() => {
                        this.swal.success('รับเข้าสต็อกเรียบร้อย');
                        this.router.navigate(['/procurement/vcb-shipments']);
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
          this.isSubmitting.set(false);
          this.swal.error(err.error?.error || 'เกิดข้อผิดพลาดในการเชื่อมต่อ');
        },
      });
    } else {
      this.vcbShipmentsService.createVcbShipment(dto).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
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
                      this.vcbShipmentsService
                        .updateVcbShipmentStatus(newId, 'Completed')
                        .subscribe(() => {
                          this.swal.success('รับเข้าสต็อกเรียบร้อย');
                          this.router.navigate(['/procurement/vcb-shipments']);
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
          this.isSubmitting.set(false);
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

  applyNewDeliveryData(fullDelivery: any, orderResponses: any[]): void {
    if (fullDelivery) {
      this.selectedDelivery.set(fullDelivery);
      this.shipmentForm.patchValue({ deliveryId: fullDelivery.id });
      this.items.clear();
      this.availableOrderItems.set([]);
      this.availableSubBoxes.set([]);

      const tempSubBoxes: string[] = [];
      if (fullDelivery.items) {
        const receivedSet = new Set<string>(fullDelivery.receivedBoxNumbers || []);
        fullDelivery.items.forEach((dItem: any) => {
          if (dItem.containedBoxNumbers) {
            try {
              const subBoxes = JSON.parse(dItem.containedBoxNumbers);
              if (Array.isArray(subBoxes)) {
                subBoxes.forEach((sub: { boxNo: string }) => {
                  if (
                    sub.boxNo &&
                    !tempSubBoxes.includes(sub.boxNo) &&
                    !receivedSet.has(sub.boxNo)
                  ) {
                    tempSubBoxes.push(sub.boxNo);
                  }
                });
              }
            } catch (e) {
              if (
                !tempSubBoxes.includes(dItem.containedBoxNumbers) &&
                !receivedSet.has(dItem.containedBoxNumbers)
              ) {
                tempSubBoxes.push(dItem.containedBoxNumbers);
              }
            }
          }
        });
      }
      this.availableSubBoxes.set(tempSubBoxes);

      const tempOrderItems: VcbOrderItem[] = [];
      orderResponses.forEach((orderRes) => {
        const order = orderRes.value || orderRes.data;
        if (order && order.items) {
          order.items.forEach((item: any) => {
            tempOrderItems.push(item);
          });
          order.items
            .filter((item: any) => (item.remainingQuantity ?? item.quantity) > 0)
            .forEach((item: any) => this.addItemToShipment(item));
        }
      });
      this.availableOrderItems.set(tempOrderItems);
    }
  }

  selectDelivery(delivery: VcbDelivery): void {
    this.selectedDeliveryId.set(delivery.id);
  }

  getParsedSubBoxes(
    jsonStr: string | undefined,
  ): { boxNo: string; dimensions?: string; weight?: number | string }[] {
    if (!jsonStr) return [];
    try {
      const parsed = JSON.parse(jsonStr);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  isBoxReceived(boxNo: string): boolean {
    const delivery = this.selectedDelivery();
    if (!delivery || !delivery.receivedBoxNumbers) return false;
    return delivery.receivedBoxNumbers.includes(boxNo);
  }

  validateItemQuantities(): boolean {
    const grouped: { [key: number]: import('@angular/forms').AbstractControl[] } = {};
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
        this.searchParams.update((p) => ({ ...p, searchTerm: term, page: 1 }));
      });
  }
}
