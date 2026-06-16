export interface CategoryBatchCreateDto {
  level1Name: string;
  level2Name?: string;
  level3Name?: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CategoryCreateDto {
  categoryName: string;
  description?: string;
  parentId?: number;
  sortOrder: number;
  isActive: boolean;
}

export interface CategoryDto {
  categoryId: number;
  categoryName: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  sortOrder: number;
  isActive: boolean;
  fullPath: string;
  level: number;
}

export interface CategoryUpdateDto {
  categoryName: string;
  description?: string;
  parentId?: number;
  sortOrder: number;
  isActive: boolean;
}
