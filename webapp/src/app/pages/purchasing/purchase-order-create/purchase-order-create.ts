import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap, tap } from 'rxjs/operators';
import { PaymentMethod, PurchaseOrderCreatePayload, PurchaseOrderDetail, PurchaseOrderItemCreatePayload, PurchaseOrderStatus } from '../../../shared/models/procurement.models';
import { PurchaseOrderService } from '../../../shared/services/procurement.service';

@Component({
  selector: 'app-purchase-order-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './purchase-order-create.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseOrderCreate implements OnInit {
  private poService = inject(PurchaseOrderService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  poId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);
  detail = signal<PurchaseOrderDetail | null>(null);

  // Form State
  pONo = signal<string>('');
  pODate = signal<string>(new Date().toISOString().split('T')[0]);
  supplierName = signal<string>('');
  supplierPhone = signal<string>('');
  supplierTaxId = signal<string>('');
  supplierAddress = signal<string>('');
  paymentMethod = signal<PaymentMethod>('TRANSFER');
  paymentRefNo = signal<string>('');
  sourceAccountInfo = signal<string>('');
  vatRate = signal<number>(7);
  discountTotal = signal<number>(0);
  shippingCost = signal<number>(0);
  expectedDeliveryDate = signal<string>('');
  slipAttachmentUrl = signal<string>('');
  notes = signal<string>('');

  items = signal<PurchaseOrderItemCreatePayload[]>([
    { variantId: 0, quantity: 1, unitPrice: 0 }
  ]);

  loading = signal<boolean>(false);
  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  statusOptions: { value: PurchaseOrderStatus; label: string }[] = [
    { value: 'DRAFT', label: 'ฉบับร่าง' },
    { value: 'ORDERED', label: 'สั่งซื้อแล้ว' },
    { value: 'PARTIALLY_RECEIVED', label: 'รับสินค้าบางส่วน' },
    { value: 'RECEIVED', label: 'รับสินค้าครบแล้ว' },
    { value: 'CANCELLED', label: 'ยกเลิก' },
  ];

  ngOnInit() {
    this.route.paramMap.pipe(
      tap((params) => {
        const id = params.get('id');
        if (id) {
          this.poId.set(Number(id));
          this.isViewMode.set(true);
          this.loading.set(true);
        }
      }),
      switchMap((params) => {
        const id = params.get('id');
        if (id) {
          return this.poService.getById(Number(id));
        }
        return [];
      }),
      tap((res: any) => {
        if (res) {
          this.detail.set(res);
        }
        this.loading.set(false);
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  addItem() {
    this.items.update(curr => [...curr, { variantId: 0, quantity: 1, unitPrice: 0 }]);
  }

  removeItem(index: number) {
    this.items.update(curr => curr.filter((_, i) => i !== index));
  }

  updateItem(index: number, field: keyof PurchaseOrderItemCreatePayload, value: any) {
    this.items.update(curr => {
      const newItems = [...curr];
      (newItems[index] as any)[field] = value;
      return newItems;
    });
  }

  get subTotal(): number {
    return this.items().reduce((sum, item) => sum + (item.quantity * item.unitPrice), 0);
  }

  get amountAfterDiscount(): number {
    return this.subTotal - this.discountTotal() + this.shippingCost();
  }

  get vatAmount(): number {
    return (this.amountAfterDiscount * this.vatRate()) / 100;
  }

  get grandTotal(): number {
    return this.amountAfterDiscount + this.vatAmount;
  }

  onSubmit() {
    if (this.items().some(i => !i.variantId || i.variantId <= 0)) {
      this.error.set('กรุณาระบุรหัสสินค้า (Variant ID) ให้ครบถ้วนในทุกรายการ');
      return;
    }

    const payload: PurchaseOrderCreatePayload = {
      pONo: this.pONo(),
      pODate: this.pODate(),
      supplierName: this.supplierName(),
      supplierPhone: this.supplierPhone(),
      supplierTaxId: this.supplierTaxId(),
      supplierAddress: this.supplierAddress(),
      discountTotal: this.discountTotal(),
      shippingCost: this.shippingCost(),
      vatRate: this.vatRate(),
      expectedDeliveryDate: this.expectedDeliveryDate() ? this.expectedDeliveryDate() : undefined,
      paymentMethod: this.paymentMethod(),
      paymentRefNo: this.paymentRefNo(),
      sourceAccountInfo: this.sourceAccountInfo(),
      slipAttachmentUrl: this.slipAttachmentUrl(),
      notes: this.notes(),
      items: this.items().map(i => ({
        variantId: Number(i.variantId),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice)
      }))
    };

    this.submitting.set(true);
    this.error.set(null);

    this.poService.create(payload).subscribe({
      next: () => {
        this.router.navigate(['/purchasing/purchase-orders']);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
        this.submitting.set(false);
      }
    });
  }

  updateStatus(status: PurchaseOrderStatus) {
    if (!this.poId()) return;
    if (!confirm(`ยืนยันการเปลี่ยนสถานะเป็น ${status}?`)) return;

    this.submitting.set(true);
    this.poService.updateStatus(this.poId()!, status).subscribe({
      next: () => {
        window.location.reload();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาด');
        this.submitting.set(false);
      }
    });
  }

  getStatusBadgeClass(status: string | undefined): string {
    if (!status) return '';
    switch (status) {
      case 'DRAFT': return 'badge-neutral';
      case 'ORDERED': return 'badge-info';
      case 'PARTIALLY_RECEIVED': return 'badge-warning';
      case 'RECEIVED': return 'badge-success';
      case 'CANCELLED': return 'badge-error';
      default: return 'badge-ghost';
    }
  }

  getStatusLabel(status: string | undefined): string {
    if (!status) return '';
    const found = this.statusOptions.find((o) => o.value === status);
    return found ? found.label : status;
  }

  getPaymentLabel(method: string | undefined): string {
    if (!method) return '-';
    switch (method) {
      case 'CASH': return 'เงินสด';
      case 'TRANSFER': return 'โอนเงิน';
      case 'CREDIT': return 'เครดิต';
      default: return method;
    }
  }
}
