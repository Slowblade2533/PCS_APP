export type ProductType = 'Product' | 'Service' | 'Consumable';
export type ProductStatus = 'Available' | 'Unavailable' | 'Discontinued' | 'Internal_Use';

export interface ProductSearchParams {
  searchTerm?: string;
  categoryId?: number | null;
  productStatus?: string;
  pageNumber: number;
  pageSize: number;
}

export interface ProductListItem {
  productId: number;
  productNameTh: string;
  productNameEn?: string;
  brandName?: string;
  categoryName: string;
  productType: ProductType;
  productStatus: ProductStatus;
  totalVariants: number;
  totalAvailableStock: number;
  minPrice: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface ProductVariantCreatePayload {
  sku: string;
  barcode?: string;
  unitOfMeasure: string;
  width: number;
  length: number;
  height: number;
  weight: number;
  basePrice: number;
  discountPrice: number;
  currentQuantity: number;
  reorderPoint: number;
  imageUrl?: string | null;
}

export interface ProductCreatePayload {
  productNameTh: string;
  productNameEn?: string;
  description?: string;
  brandName?: string;
  categoryId: number;
  productType: ProductType;
  productStatus: ProductStatus;
  createdBy?: number;
  variants: ProductVariantCreatePayload[];
}

export interface ProductCreateResponse {
  message: string;
  productId: number;
}
