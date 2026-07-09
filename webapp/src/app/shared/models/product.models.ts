export type ProductType = 'Product' | 'Service' | 'Consumable';
export type ProductStatus = 'Available' | 'Unavailable' | 'Discontinued';
export type InventoryGroup = 'ForSale' | 'Internal';

export interface ProductCreatePayload {
  productNameTh: string;
  productNameEn?: string;
  description?: string;
  brandName?: string;
  categoryId: number;
  productType: ProductType;
  productStatus: ProductStatus;
  isStockTracked: boolean;
  inventoryGroup: InventoryGroup;
  createdBy?: number;
  updatedBy?: number;
  variants: ProductVariantCreatePayload[];
}

export interface ProductCreateResponse {
  message: string;
  productId: number;
}

export interface ProductDetail {
  productId: number;
  productNameTh: string;
  productNameEn?: string;
  description?: string;
  brandName?: string;
  categoryId: number;
  productType: string;
  productStatus: string;
  isStockTracked: boolean;
  inventoryGroup: string;
  variants: ProductVariantDetail[];
}

export interface ProductListItem {
  productId: number;
  productNameTh: string;
  productNameEn?: string;
  brandName?: string;
  categoryName: string;
  productType: ProductType;
  productStatus: ProductStatus;
  isStockTracked: boolean;
  inventoryGroup: InventoryGroup;
  totalVariants: number;
  totalAvailableStock: number;
  minPrice: number;
  imageUrl?: string;
}

export interface ProductSearchParams {
  searchTerm?: string;
  categoryId?: number | null;
  productStatus?: string;
  productType?: string;
  inventoryGroup?: string;
  pageNumber: number;
  pageSize: number;
}

export interface ProductVariantCreatePayload {
  variantId?: number;
  sku: string;
  variantNameTh?: string;
  variantNameEn?: string;
  barcode?: string;
  color?: string;
  sizeLabel?: string;
  stylePattern?: string;
  condition?: string;
  unitOfMeasure: string;
  width: number;
  length: number;
  height: number;
  weight: number | string;
  basePrice: number | string;
  discountPrice: number | string;
  currentQuantity: number;
  reorderPoint: number;
  imageUrl?: string | null;
}

export interface ProductVariantDetail {
  variantId: number;
  productId: number;
  sku: string;
  variantNameTh?: string;
  variantNameEn?: string;
  barcode?: string;
  color?: string;
  sizeLabel?: string;
  stylePattern?: string;
  condition?: string;
  imageUrl?: string;
  unitOfMeasure: string;
  width: number;
  length: number;
  height: number;
  weight: number;
  basePrice: number;
  discountPrice: number;
  currentQuantity: number;
  reorderPoint: number;
}
