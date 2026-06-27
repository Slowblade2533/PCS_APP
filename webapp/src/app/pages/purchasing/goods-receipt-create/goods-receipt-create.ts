import { DatePipe, DecimalPipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal,
  computed,
  effect,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { rxResource } from '@angular/core/rxjs-interop';
import { environment } from '../../../../environments/environment';
import { GoodsReceiptCreatePayload, GoodsReceiptDetail, GoodsReceiptItemCreatePayload } from '../../../shared/models/goods-receipts.models';
import { PurchaseOrderDetail } from '../../../shared/models/purchase-orders.models';
import { PaymentMethod } from '../../../shared/models/shared.models';
import { GoodsReceiptsService } from '../../../shared/services/goods-receipts.service';
import { PurchaseOrdersService } from '../../../shared/services/purchase-orders.service';
import { SweetAlertService } from '../../../shared/services/sweet-alert.service';
import { ImageHoverPreview } from '../../../shared/components/image-hover-preview/image-hover-preview';

@Component({
  selector: 'app-goods-receipt-create',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, DatePipe, ImageHoverPreview],
  templateUrl: './goods-receipt-create.html',
})
export class GoodsReceiptCreate implements OnInit {
  private grService = inject(GoodsReceiptsService);
  private poService = inject(PurchaseOrdersService);
  private swal = inject(SweetAlertService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  apiOrigin = environment.apiUrl.replace('/api', '');


  receiptId = signal<number | null>(null);
  isViewMode = signal<boolean>(false);

  // Initial State from Query Params (when creating from PO)
  sourcePoId = signal<number | null>(null);

  // Form State
  receiptNo = signal<string>('');
  receiptDate = signal<string>(new Date().toLocaleDateString('en-CA'));
  shippingCompany = signal<string>('');
  trackingNo = signal<string>('');
  shippingCost = signal<number>(0);
  shippingPaymentMethod = signal<PaymentMethod | ''>('');
  shippingPaymentRefNo = signal<string>('');
  shippingSourceAccount = signal<string>('');
  shippingSlipUrl = signal<string>('');
  notes = signal<string>('');

  items = signal<GoodsReceiptItemCreatePayload[]>([]);

  submitting = signal<boolean>(false);
  error = signal<string | null>(null);

  receiptResource = rxResource({
    params: () => this.receiptId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.grService.getById(params).pipe(catchError(() => of(null)));
    },
  });

  poResource = rxResource({
    params: () => this.sourcePoId(),
    stream: ({ params }) => {
      if (!params) return of(null);
      return this.poService.getById(params).pipe(catchError(() => of(null)));
    },
  });

  detail = computed(() => this.receiptResource.value() || null);
  sourcePoDetail = computed(() => this.poResource.value() || null);
  loading = computed(() => this.receiptResource.isLoading() || this.poResource.isLoading());

  constructor() {
    effect(() => {
      const poData = this.sourcePoDetail();
      if (poData && !this.isViewMode()) {
        untracked(() => {
          const newItems: GoodsReceiptItemCreatePayload[] = poData.items
            .map((pi: any) => {
              const expectedQty = pi.quantity - pi.receivedQuantity;
              return {
                poItemId: pi.poItemId,
                variantId: pi.variantId,
                expectedQuantity: expectedQty,
                receivedQuantity: expectedQty > 0 ? expectedQty : 0,
                defectiveQuantity: 0,
                damagedQuantity: 0,
              };
            })
            .filter((i: any) => i.expectedQuantity > 0);

          this.items.set(newItems);
        });
      }
    });
  }

  ngOnInit() {
    // Check if there is an ID in the route params -> View Mode
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.receiptId.set(Number(id));
        this.isViewMode.set(true);
      }
    });

    // Check if there is a poId in query params -> Create Mode (from PO)
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const poId = params.get('poId');
      if (poId) {
        // we set sourcePoId only if not viewMode, but actually wait, we can just set it and viewMode condition applies in effect
        this.sourcePoId.set(Number(poId));
      }
    });
  }

  updateItem(index: number, field: keyof GoodsReceiptItemCreatePayload, value: any) {
    this.items.update((curr) => {
      const newItems = [...curr];
      (newItems[index] as any)[field] = value;
      return newItems;
    });
  }

  getPoItem(poItemId: number) {
    return this.sourcePoDetail()?.items.find((i) => i.poItemId === poItemId);
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
      shippingPaymentMethod: this.shippingPaymentMethod()
        ? (this.shippingPaymentMethod() as PaymentMethod)
        : undefined,
      shippingPaymentRefNo: this.shippingPaymentRefNo() || undefined,
      shippingSourceAccount: this.shippingSourceAccount() || undefined,
      shippingSlipUrl: this.shippingSlipUrl() || undefined,
      notes: this.notes(),
      items: this.items().map((i) => ({
        poItemId: Number(i.poItemId),
        variantId: Number(i.variantId),
        expectedQuantity: Number(i.expectedQuantity),
        receivedQuantity: Number(i.receivedQuantity),
        defectiveQuantity: Number(i.defectiveQuantity),
        damagedQuantity: Number(i.damagedQuantity),
      })),
    };

    this.submitting.set(true);
    this.error.set(null);

    this.grService.create(payload).subscribe({
      next: () => {
        this.router.navigate(['/purchasing/goods-receipts']);
      },
      error: (err) => {
        if (err.error?.errors) {
          const errorMessages = Object.values(err.error.errors).flat().join('\n');
          this.error.set(errorMessages);
        } else {
          this.error.set(err.error?.message || 'เกิดข้อผิดพลาดในการบันทึกรายการ');
        }
        this.submitting.set(false);
      },
    });
  }

  completeReceipt() {
    if (!this.receiptId()) return;
    this.swal
      .confirm(
        'ยืนยันการรับสินค้าเข้าสต๊อค',
        'ยืนยันว่าทำการรับสินค้าเข้าคลังเสร็จสิ้น? ระบบจะเพิ่มสต๊อกให้ทันที (ไม่สามารถแก้ไขได้อีก)'
      )
      .then((res) => {
        if (res.isConfirmed) {
          this.submitting.set(true);
          this.grService.complete(this.receiptId()!).subscribe({
            next: () => {
              window.location.reload();
            },
            error: (err) => {
              this.error.set(err.error?.message || 'เกิดข้อผิดพลาด');
              this.submitting.set(false);
            },
          });
        }
      });
  }

  getStatusBadgeClass(status: string | undefined): string {
    if (!status) return '';
    switch (status) {
      case 'PENDING':
        return 'badge-warning';
      case 'COMPLETED':
        return 'badge-success';
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
