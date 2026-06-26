import { PaymentMethod } from './shared.models';

export type GoodsReceiptStatus = 'PENDING' | 'COMPLETED';

export interface GoodsReceiptListItem {
  receiptId: number;
  receiptNo: string;
  pONo: string;
  supplierName: string;
  receiptDate: string;
  shippingCompany?: string;
  trackingNo?: string;
  shippingCost: number;
  status: GoodsReceiptStatus;
  itemCount: number;
}

export interface GoodsReceiptDetail {
  receiptId: number;
  receiptNo: string;
  purchaseOrderId: number;
  pONo: string;
  supplierName: string;
  receiptDate: string;
  shippingCompany?: string;
  trackingNo?: string;
  shippingCost: number;
  status: GoodsReceiptStatus;
  shippingPaymentMethod?: PaymentMethod;
  shippingPaymentRefNo?: string;
  shippingSourceAccount?: string;
  shippingSlipUrl?: string;
  notes?: string;
  createdAt: string;
  items: GoodsReceiptItem[];
}

export interface GoodsReceiptItem {
  receiptItemId: number;
  pOItemId: number;
  variantId: number;
  sku: string;
  productName: string;
  variantName?: string;
  imageUrl?: string;
  expectedQuantity: number;
  receivedQuantity: number;
  defectiveQuantity: number;
  damagedQuantity: number;
}

export interface GoodsReceiptItemCreatePayload {
  pOItemId: number;
  variantId: number;
  expectedQuantity: number;
  receivedQuantity: number;
  defectiveQuantity: number;
  damagedQuantity: number;
}

export interface GoodsReceiptCreatePayload {
  receiptNo: string;
  purchaseOrderId: number;
  receiptDate?: string;
  shippingCompany?: string;
  trackingNo?: string;
  shippingCost?: number;
  shippingPaymentMethod?: PaymentMethod;
  shippingPaymentRefNo?: string;
  shippingSourceAccount?: string;
  shippingSlipUrl?: string;
  notes?: string;
  createdBy?: number;
  items: GoodsReceiptItemCreatePayload[];
}

export interface GoodsReceiptSearchParams {
  searchTerm?: string;
  purchaseOrderId?: number;
  status?: GoodsReceiptStatus | '';
  pageNumber: number;
  pageSize: number;
}
