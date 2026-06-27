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
  createdByUsername?: string;
  createdAt: string;
  updatedByUsername?: string;
  updatedAt: string;
  itemsCount?: number;
  items: VcbShipmentItem[];
}

export interface VcbShipmentCreate {
  deliveryId: number;
  receiptDate?: string | null;
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
