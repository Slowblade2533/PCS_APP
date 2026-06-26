import { StockCondition, PaymentMethod } from './shared.models';

export type SalesOrderStatus = 'DRAFT' | 'COMPLETED' | 'CANCELLED';

export interface SalesOrderListItem {
  orderId: number;
  orderNo: string;
  orderDate: string;
  customerName?: string;
  grandTotal: number;
  orderStatus: SalesOrderStatus;
  paymentMethod?: PaymentMethod;
  itemCount: number;
}

export interface SalesOrderDetail {
  orderId: number;
  orderNo: string;
  orderDate: string;
  customerName?: string;
  customerPhone?: string;
  customerTaxId?: string;
  customerAddress?: string;
  subTotal: number;
  discountTotal: number;
  vatRate: number;
  vatAmount: number;
  grandTotal: number;
  orderStatus: SalesOrderStatus;
  taxInvoiceId?: number;
  paymentMethod?: PaymentMethod;
  paymentRefNo?: string;
  receiverAccountName?: string;
  slipAttachmentUrl?: string;
  notes?: string;
  createdAt: string;
  items: SalesOrderItem[];
}

export interface SalesOrderItem {
  orderItemId: number;
  variantId: number;
  sku: string;
  productName: string;
  variantName?: string;
  imageUrl?: string;
  condition: StockCondition;
  quantity: number;
  unitPrice: number;
  discount: number;
  lineTotal: number;
}

export interface SalesOrderItemCreatePayload {
  variantId: number;
  condition: StockCondition;
  quantity: number;
  unitPrice: number;
  discount: number;
}

export interface SalesOrderCreatePayload {
  orderDate?: string;
  customerName?: string;
  customerPhone?: string;
  customerTaxId?: string;
  customerAddress?: string;
  vatRate?: number;
  paymentMethod?: PaymentMethod;
  paymentRefNo?: string;
  receiverAccountId?: number;
  slipAttachmentUrl?: string;
  notes?: string;
  createdBy?: number;
  items: SalesOrderItemCreatePayload[];
}

export interface SalesOrderSearchParams {
  searchTerm?: string;
  status?: SalesOrderStatus | '';
  dateFrom?: string;
  dateTo?: string;
  pageNumber: number;
  pageSize: number;
}
