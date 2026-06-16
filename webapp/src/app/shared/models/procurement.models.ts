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
