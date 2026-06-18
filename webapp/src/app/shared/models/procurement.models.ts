export interface VcbOrder {
  id: number;
  orderNo: string;
  orderDate: string;
  totalAmount: number;
  status: string;
  branchId: number;
  notes?: string;
  transferSlipUrl?: string;
  createdBy?: number;
  createdAt: string;
  updatedAt: string;
  itemCount?: number;
  items: VcbOrderItem[];
}

export interface VcbOrderCreate {
  orderNo: string;
  orderDate: string;
  totalAmount: number | string;
  branchId: number;
  notes?: string;
  items: VcbOrderItemCreate[];
}

export interface VcbOrderItem {
  id: number;
  orderId: number;
  variantId: number;
  sku: string;
  productName: string;
  variantName: string;
  imageUrl?: string;
  quantity: number;
  totalPrice: number;
  receivedQuantity?: number;
  remainingQuantity?: number;
}

export interface VcbOrderItemCreate {
  variantId: number;
  quantity: number;
  totalPrice: number | string;
  // UI helper
  sku?: string;
  productName?: string;
  variantName?: string;
}

export interface VcbOrderSearch {
  searchTerm?: string;
  status?: string;
  branchId?: number;
  page: number;
  pageSize: number;
}

export interface VcbShipment {
  id: number;
  receiptNo: string;
  receiptDate: string;
  deliveryId: number;
  deliveryNo?: string;
  status: string;
  notes?: string;
  isForceCloseOrder?: boolean;
  createdBy?: number;
  createdAt: string;
  updatedAt: string;
  itemsCount?: number;
  items: VcbShipmentItem[];
}

export interface VcbShipmentCreate {
  deliveryId: number;
  notes?: string;
  isForceCloseOrder?: boolean;
  items: VcbShipmentItemCreate[];
}

export interface VcbShipmentItem {
  id: number;
  shipmentId: number;
  orderItemId: number;
  variantId: number;
  sku: string;
  imageUrl?: string;
  productName: string;
  variantName: string;
  boxNumbers?: string;
  receiptStatus: string;
  expectedQuantity: number;
  goodQuantity: number;
  defectiveQuantity: number;
  refundAmount: number;
}

export interface VcbShipmentItemCreate {
  orderItemId: number;
  variantId: number;
  boxNumbers: string;
  receiptStatus: string;
  expectedQuantity: number;
  goodQuantity: number;
  defectiveQuantity: number;
  refundAmount: number | string;
  // UI Helper
  sku?: string;
  productName?: string;
  variantName?: string;
}

export interface VcbShipmentSearch {
  searchTerm?: string;
  status?: string;
  deliveryId?: number;
  page: number;
  pageSize: number;
}

export interface VcbDelivery {
  id: number;
  deliveryNo: string;
  orderDate: string;
  shippingAddress: string;
  domesticShippingCompany: string;
  totalAmountBeforeDiscount: number;
  discountAmount: number;
  totalAmount: number;
  transferredAmount: number;
  transferSlipUrl?: string;
  status: string;
  notes?: string;
  createdBy?: number;
  createdAt: string;
  updatedAt: string;
  orders: VcbDeliveryOrder[];
  items: VcbDeliveryItem[];
  packageBoxes?: string;
  orderNumbers?: string;
  receivedBoxNumbers?: string[];
}

export interface VcbDeliveryOrder {
  deliveryId: number;
  orderId: number;
  orderNo?: string;
}

export interface VcbDeliveryItem {
  id: number;
  deliveryId: number;
  packageBoxNo: string;
  domesticTrackingNo?: string;
  totalWeight: number;
  boxDimensions?: string;
  containedBoxNumbers?: string;
  shippingCost: number;
}

export interface VcbDeliveryCreate {
  deliveryNo: string;
  orderDate: string;
  shippingAddress: string;
  domesticShippingCompany: string;
  totalAmountBeforeDiscount: number | string;
  discountAmount: number | string;
  totalAmount: number | string;
  transferredAmount: number | string;
  notes?: string;
  orderIds: number[];
  items: VcbDeliveryItemCreate[];
}

export interface VcbDeliveryItemCreate {
  packageBoxNo: string;
  domesticTrackingNo?: string;
  totalWeight: number | string;
  boxDimensions?: string;
  containedBoxNumbers?: string;
  shippingCost: number | string;
}

export interface VcbDeliverySearch {
  searchTerm?: string;
  status?: string;
  page: number;
  pageSize: number;
}
// ─── Sales Order ──────────────────────────────────────────────────────────────
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

// ─── Purchase Order ───────────────────────────────────────────────────────────
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

// ─── Goods Receipt ────────────────────────────────────────────────────────────
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

// ─── Stock Adjustments ────────────────────────────────────────────────────────
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
