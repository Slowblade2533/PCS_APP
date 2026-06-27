import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { SalesOrderCreatePayload, SalesOrderDetail, SalesOrderItemCreatePayload } from '../../../shared/models/sales-orders.models';
import { PaymentMethod, StockCondition } from '../../../shared/models/shared.models';
import { SalesOrdersService } from '../../../shared/services/sales-orders.service';

@Component({
  selector: 'app-sales-order-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './sales-order-create.html',
})
export class SalesOrderCreate implements OnInit {
  private salesService = inject(SalesOrdersService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  orderId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);

  orderResource = rxResource({
    params: () => this.orderId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.salesService.getById(params).pipe(catchError(() => of(null)));
    },
  });

  detail = computed(() => this.orderResource.value() || null);
  loading = computed(() => this.orderResource.isLoading());

  // Form State
  orderDate = signal<string>(new Date().toLocaleDateString('en-CA'));
  customerName = signal<string>('');
  customerPhone = signal<string>('');
  customerTaxId = signal<string>('');
  customerAddress = signal<string>('');
  paymentMethod = signal<PaymentMethod>('TRANSFER');
  paymentRefNo = signal<string>('');
  vatRate = signal<number>(7);
  slipAttachmentUrl = signal<string>('');
  notes = signal<string>('');

  items = signal<SalesOrderItemCreatePayload[]>([
    { variantId: 0, condition: 'Normal', quantity: 1, unitPrice: 0, discount: 0 },
  ]);

  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  conditionOptions: { value: StockCondition; label: string }[] = [
    { value: 'Normal', label: 'ปกติ' },
    { value: 'Defective', label: 'มีตำหนิ' },
    { value: 'Giveaway', label: 'ของแถม' },
  ];

  ngOnInit() {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.orderId.set(Number(id));
        this.isViewMode.set(true);
      }
    });
  }

  addItem() {
    this.items.update((curr) => [
      ...curr,
      { variantId: 0, condition: 'Normal', quantity: 1, unitPrice: 0, discount: 0 },
    ]);
  }

  removeItem(index: number) {
    this.items.update((curr) => curr.filter((_, i) => i !== index));
  }

  updateItem(index: number, field: keyof SalesOrderItemCreatePayload, value: any) {
    this.items.update((curr) => {
      const newItems = [...curr];
      (newItems[index] as any)[field] = value;
      return newItems;
    });
  }

  get subTotal(): number {
    return this.items().reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
  }

  get totalDiscount(): number {
    return this.items().reduce((sum, item) => sum + Number(item.discount || 0), 0);
  }

  get amountAfterDiscount(): number {
    return this.subTotal - this.totalDiscount;
  }

  get vatAmount(): number {
    return (this.amountAfterDiscount * this.vatRate()) / 100;
  }

  get grandTotal(): number {
    return this.amountAfterDiscount + this.vatAmount;
  }

  onSubmit() {
    if (this.items().some((i) => !i.variantId || i.variantId <= 0)) {
      this.error.set('กรุณาระบุรหัสสินค้า (Variant ID) ให้ครบถ้วนในทุกรายการ');
      return;
    }

    const payload: SalesOrderCreatePayload = {
      orderDate: this.orderDate(),
      customerName: this.customerName(),
      customerPhone: this.customerPhone(),
      customerTaxId: this.customerTaxId(),
      customerAddress: this.customerAddress(),
      vatRate: this.vatRate(),
      paymentMethod: this.paymentMethod(),
      paymentRefNo: this.paymentRefNo(),
      slipAttachmentUrl: this.slipAttachmentUrl(),
      notes: this.notes(),
      items: this.items().map((i) => ({
        variantId: Number(i.variantId),
        condition: i.condition,
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice),
        discount: Number(i.discount),
      })),
    };

    this.submitting.set(true);
    this.error.set(null);

    this.salesService.create(payload).subscribe({
      next: () => {
        this.router.navigate(['/sales/orders']);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
        this.submitting.set(false);
      },
    });
  }

  completeOrder() {
    if (!this.orderId()) return;
    if (
      !confirm(
        'ยืนยันว่าการชำระเงินและส่งมอบสินค้าเสร็จสิ้น ระบบจะทำการตัดสต๊อกอัตโนมัติ (ไม่สามารถยกเลิกได้)',
      )
    )
      return;

    this.submitting.set(true);
    this.salesService.complete(this.orderId()!).subscribe({
      next: () => {
        window.location.reload();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาด');
        this.submitting.set(false);
      },
    });
  }

  cancelOrder() {
    if (!this.orderId()) return;
    if (!confirm('ยืนยันการยกเลิกใบสั่งขายนี้?')) return;

    this.submitting.set(true);
    this.salesService.cancel(this.orderId()!).subscribe({
      next: () => {
        window.location.reload();
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาด');
        this.submitting.set(false);
      },
    });
  }

  getStatusBadgeClass(status: string | undefined): string {
    if (!status) return '';
    switch (status) {
      case 'DRAFT':
        return 'badge-neutral';
      case 'COMPLETED':
        return 'badge-success';
      case 'CANCELLED':
        return 'badge-error';
      default:
        return 'badge-ghost';
    }
  }

  getPaymentLabel(method: string | undefined): string {
    if (!method) return '-';
    switch (method) {
      case 'CASH':
        return 'เงินสด';
      case 'TRANSFER':
        return 'โอนเงิน';
      case 'CREDIT':
        return 'เครดิต';
      default:
        return method;
    }
  }
}
