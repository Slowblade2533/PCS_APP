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
  createdByUsername?: string;
  createdAt: string;
  updatedByUsername?: string;
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
