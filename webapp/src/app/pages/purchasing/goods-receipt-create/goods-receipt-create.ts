import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap, tap } from 'rxjs/operators';
import { GoodsReceiptCreatePayload, GoodsReceiptDetail, GoodsReceiptItemCreatePayload, PaymentMethod, PurchaseOrderDetail } from '../../../shared/models/procurement.models';
import { GoodsReceiptService, PurchaseOrderService } from '../../../shared/services/procurement.service';

@Component({
  selector: 'app-goods-receipt-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe],
  templateUrl: './goods-receipt-create.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoodsReceiptCreate implements OnInit {
  private grService = inject(GoodsReceiptService);
  private poService = inject(PurchaseOrderService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  receiptId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);
  detail = signal<GoodsReceiptDetail | null>(null);

  // Initial State from Query Params (when creating from PO)
  sourcePoId = signal<number | null>(null);
  sourcePoDetail = signal<PurchaseOrderDetail | null>(null);

  // Form State
  receiptNo = signal<string>('');
  receiptDate = signal<string>(new Date().toISOString().split('T')[0]);
  shippingCompany = signal<string>('');
  trackingNo = signal<string>('');
  shippingCost = signal<number>(0);
  shippingPaymentMethod = signal<PaymentMethod | ''>('');
  shippingPaymentRefNo = signal<string>('');
  shippingSourceAccount = signal<string>('');
  shippingSlipUrl = signal<string>('');
  notes = signal<string>('');

  items = signal<GoodsReceiptItemCreatePayload[]>([]);

  loading = signal<boolean>(false);
  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  ngOnInit() {
    // Check if there is an ID in the route params -> View Mode
    this.route.paramMap.pipe(
      tap((params) => {
        const id = params.get('id');
        if (id) {
          this.receiptId.set(Number(id));
          this.isViewMode.set(true);
          this.loading.set(true);
        }
      }),
      switchMap((params) => {
        const id = params.get('id');
        if (id) {
          return this.grService.getById(Number(id));
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

    // Check if there is a poId in query params -> Create Mode (from PO)
    this.route.queryParamMap.pipe(
      tap((params) => {
        const poId = params.get('poId');
        if (poId && !this.isViewMode()) {
          this.sourcePoId.set(Number(poId));
          this.loading.set(true);
        }
      }),
      switchMap((params) => {
        const poId = params.get('poId');
        if (poId && !this.isViewMode()) {
          return this.poService.getById(Number(poId));
        }
        return [];
      }),
      tap((res: any) => {
        if (res && !this.isViewMode()) {
          this.sourcePoDetail.set(res);
          // Initialize items based on PO
          const newItems: GoodsReceiptItemCreatePayload[] = res.items.map((pi: any) => {
            const expectedQty = pi.quantity - pi.receivedQuantity;
            return {
              pOItemId: pi.pOItemId,
              variantId: pi.variantId,
              expectedQuantity: expectedQty,
              receivedQuantity: expectedQty > 0 ? expectedQty : 0, // Default to receiving remaining
              defectiveQuantity: 0,
              damagedQuantity: 0,
              // Keep original PO info for display purpose (we will access it from sourcePoDetail in HTML)
            };
          }).filter((i: any) => i.expectedQuantity > 0); // Only show items that still need to be received
          
          this.items.set(newItems);
        }
        this.loading.set(false);
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe();
  }

  updateItem(index: number, field: keyof GoodsReceiptItemCreatePayload, value: any) {
    this.items.update(curr => {
      const newItems = [...curr];
      (newItems[index] as any)[field] = value;
      return newItems;
    });
  }

  getPoItem(poItemId: number) {
    return this.sourcePoDetail()?.items.find(i => i.pOItemId === poItemId);
  }

  onSubmit() {
    if (!this.sourcePoId()) {
      this.error.set('ไม่พบข้อมูลใบสั่งซื้อต้นทาง (PO)');
      return;
    }

    const payload: GoodsReceiptCreatePayload = {
      receiptNo: this.receiptNo(),
      purchaseOrderId: this.sourcePoId()!,
      receiptDate: this.receiptDate(),
      shippingCompany: this.shippingCompany(),
      trackingNo: this.trackingNo(),
      shippingCost: this.shippingCost() > 0 ? this.shippingCost() : undefined,
      shippingPaymentMethod: this.shippingPaymentMethod() ? (this.shippingPaymentMethod() as PaymentMethod) : undefined,
      shippingPaymentRefNo: this.shippingPaymentRefNo() || undefined,
      shippingSourceAccount: this.shippingSourceAccount() || undefined,
      shippingSlipUrl: this.shippingSlipUrl() || undefined,
      notes: this.notes(),
      items: this.items().map(i => ({
        pOItemId: Number(i.pOItemId),
        variantId: Number(i.variantId),
        expectedQuantity: Number(i.expectedQuantity),
        receivedQuantity: Number(i.receivedQuantity),
        defectiveQuantity: Number(i.defectiveQuantity),
        damagedQuantity: Number(i.damagedQuantity)
      }))
    };

    this.submitting.set(true);
    this.error.set(null);

    this.grService.create(payload).subscribe({
      next: () => {
        this.router.navigate(['/purchasing/goods-receipts']);
      },
      error: (err) => {
        this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
        this.submitting.set(false);
      }
    });
  }

  completeReceipt() {
    if (!this.receiptId()) return;
    if (!confirm('ยืนยันว่าทำการรับสินค้าเข้าคลังเสร็จสิ้น? ระบบจะเพิ่มสต๊อกให้ทันที (ไม่สามารถแก้ไขได้อีก)')) return;

    this.submitting.set(true);
    this.grService.complete(this.receiptId()!).subscribe({
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
      case 'PENDING': return 'badge-warning';
      case 'COMPLETED': return 'badge-success';
      default: return 'badge-ghost';
    }
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
