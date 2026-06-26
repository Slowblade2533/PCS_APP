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
  createdByUsername?: string;
  createdAt: string;
  updatedByUsername?: string;
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
