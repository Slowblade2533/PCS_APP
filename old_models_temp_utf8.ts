// โ”€โ”€โ”€ Sales Order โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
export type SalesOrderStatus = 'DRAFT' | 'COMPLETED' | 'CANCELLED';
export type StockCondition = 'Normal' | 'Defective' | 'Giveaway' | 'Damaged';
export type PaymentMethod = 'CASH' | 'TRANSFER' | 'CREDIT';

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

// โ”€โ”€โ”€ Purchase Order โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
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

// โ”€โ”€โ”€ Goods Receipt โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
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

// โ”€โ”€โ”€ Stock Adjustments โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€โ”€
export interface StockConditionTransferPayload {
  variantId: number;
  fromCondition: StockCondition;
  toCondition: StockCondition;
  quantity: number;
  reason?: string;
}

export interface StockScrapPayload {
  variantId: number;
  condition: StockCondition;
  quantity: number;
  isSold: boolean;
  scrapPrice?: number;
  reason?: string;
}
