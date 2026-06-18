export interface StockItem {
  variantId: number;
  sku: string;
  barcode?: string;
  variantName: string;
  productName: string;
  currentQuantity: number;
  reservedQuantity: number;
  availableQuantity: number;
  branchId: number;
  branchName: string;
  imageUrl?: string;
  updatedAt: string;
  brandName?: string;
  condition: string;
}

export interface StockListQuery {
  page: number;
  pageSize: number;
  search?: string;
  branchId?: number;
  productStatus?: string;
  productType?: string;
  inventoryGroup?: string;
  condition?: string;
}

export interface StockTransaction {
  id: number;
  variantId: number;
  sku: string;
  barcode?: string;
  productName: string;
  variantName: string;
  transactionType: TransactionType;
  quantity: number;
  quantityBefore: number;
  quantityAfter: number;
  branchId: number;
  branchName: string;
  referenceNo: string | null;
  requestId: string | null;
  note: string | null;
  createdBy: number | null;
  createdByUsername: string | null;
  createdAt: string;
  condition: string;
}

export interface StockTransactionQuery {
  variantId?: number;
  branchId?: number;
  transactionType?: TransactionType;
  dateFrom?: string;
  dateTo?: string;
  page: number;
  pageSize: number;
}

export interface StockTransactionRequest {
  variantId: number;
  branchId: number;
  transactionType: TransactionType;
  quantity: number;
  referenceNo?: string;
  requestId?: string;
  note?: string;
  condition: string;
}

export type TransactionType = 'IN' | 'OUT' | 'ADJUST' | 'RESERVE' | 'UNRESERVE' | 'DAMAGE' | 'LOST';
