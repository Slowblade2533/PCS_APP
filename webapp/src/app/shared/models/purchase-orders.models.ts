import { PaymentMethod } from './shared.models';

export type PurchaseOrderStatus = 'DRAFT' | 'ORDERED' | 'PARTIALLY_RECEIVED' | 'RECEIVED' | 'CANCELLED';

export interface PurchaseOrderListItem {
  purchaseOrderId: number;
  pONo: string;
  pODate: string;
  supplierName: string;
  grandTotal: number;
  status: PurchaseOrderStatus;
  paymentMethod?: PaymentMethod;
  itemCount: number;
  totalOrdered: number;
  totalReceived: number;
}

export interface PurchaseOrderDetail {
  purchaseOrderId: number;
  pONo: string;
  pODate: string;
  supplierName: string;
  supplierPhone?: string;
  supplierTaxId?: string;
  supplierAddress?: string;
  subTotal: number;
  discountTotal: number;
  shippingCost: number;
  vatRate: number;
  vatAmount: number;
  grandTotal: number;
  status: PurchaseOrderStatus;
  taxInvoiceId?: number;
  expectedDeliveryDate?: string;
  paymentMethod?: PaymentMethod;
  paymentRefNo?: string;
  sourceAccountInfo?: string;
  receiverAccountName?: string;
  slipAttachmentUrl?: string;
  notes?: string;
  createdAt: string;
  items: PurchaseOrderItem[];
}

export interface PurchaseOrderItem {
  pOItemId: number;
  variantId: number;
  sku: string;
  productName: string;
  variantName?: string;
  imageUrl?: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  receivedQuantity: number;
}

export interface PurchaseOrderItemCreatePayload {
  variantId: number;
  quantity: number;
  unitPrice: number;
}

export interface PurchaseOrderCreatePayload {
  pONo: string;
  pODate: string;
  supplierName: string;
  supplierPhone?: string;
  supplierTaxId?: string;
  supplierAddress?: string;
  discountTotal?: number;
  shippingCost?: number;
  vatRate?: number;
  expectedDeliveryDate?: string;
  paymentMethod?: PaymentMethod;
  paymentRefNo?: string;
  sourceAccountInfo?: string;
  receiverAccountId?: number;
  slipAttachmentUrl?: string;
  notes?: string;
  createdBy?: number;
  items: PurchaseOrderItemCreatePayload[];
}

export interface PurchaseOrderSearchParams {
  searchTerm?: string;
  status?: PurchaseOrderStatus | '';
  dateFrom?: string;
  dateTo?: string;
  pageNumber: number;
  pageSize: number;
}
