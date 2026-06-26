using PCS_API.DTOs;
using PCS_API.Repositories;
using PCS_API.Models;
using System.Data;

namespace PCS_API.Services;

public class ProductService(
        IProductRepository productRepository,
        IStockRepository stockRepository,
        ISqlConnectionFactory connectionFactory,
        ImageCleanupChannel imageCleanup) : IProductService
{
    public async Task<PagedResultDto<ProductListDto>> GetPagedProductsAsync(ProductSearchParamsDto search, CancellationToken cancellationToken = default)
    {
        return await productRepository.GetPagedProductsAsync(search, cancellationToken);
    }

    public async Task<ProductDetailDto?> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await productRepository.GetProductDetailAsync(productId, cancellationToken);
    }

    public async Task<ResultDto<int>> CreateProductWithVariantsAsync(ProductCreateDto dto, CancellationToken cancellationToken = default)
    {
        var duplicateSkusInPayload = dto.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
            .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        
        if (duplicateSkusInPayload.Any())
        {
            return ResultDto<int>.Failure($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
        }

        var duplicateBarcodesInPayload = dto.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
            .GroupBy(v => v.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateBarcodesInPayload.Any())
        {
            return ResultDto<int>.Failure($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
        }

        using var connection = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Could not create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var barcodesToCheck = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Barcode)).Select(v => v.Barcode).ToList();
            var skusToCheck = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Sku)).Select(v => v.Sku).ToList();

            if (barcodesToCheck.Any() || skusToCheck.Any())
            {
                if (barcodesToCheck.Any())
                {
                    var existingBarcodes = await productRepository.CheckBarcodesExistAsync(barcodesToCheck, null, transaction, cancellationToken);
                    if (existingBarcodes.Any())
                    {
                        return ResultDto<int>.Failure($"รหัสบาร์โค้ด '{string.Join(", ", existingBarcodes)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                    }
                }
                
                if (skusToCheck.Any())
                {
                    var existingSkus = await productRepository.CheckSkusExistAsync(skusToCheck, null, transaction, cancellationToken);
                    if (existingSkus.Any())
                    {
                        return ResultDto<int>.Failure($"รหัส SKU '{string.Join(", ", existingSkus)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                    }
                }
            }

            int productId = await productRepository.InsertProductAsync(dto, transaction, cancellationToken);

            foreach (var v in dto.Variants)
            {
                int variantId = await productRepository.InsertProductVariantAsync(productId, v, transaction, cancellationToken);
                await productRepository.InsertProductPriceAsync(variantId, v.BasePrice, v.DiscountPrice, transaction, cancellationToken);
                await stockRepository.InsertStockAsync(variantId, v.CurrentQuantity, v.ReorderPoint, transaction, cancellationToken);

                if (dto.IsStockTracked && v.CurrentQuantity > 0)
                {
                    var txModel = new PCS_API.Models.StockTransactionModel
                    {
                        VariantId = variantId,
                        TransactionType = "IN",
                        Quantity = v.CurrentQuantity,
                        UnitCost = 0.00m,
                        Notes = "บันทึกยอดตั้งต้นจากการเพิ่มสินค้าใหม่",
                        CreatedBy = dto.CreatedBy
                    };
                    await stockRepository.CreateTransactionAsync(txModel, transaction);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return ResultDto<int>.Success(productId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ResultDto<bool>> UpdateProductWithVariantsAsync(int productId, ProductCreateDto dto, CancellationToken cancellationToken = default)
    {
        var duplicateSkusInPayload = dto.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Sku))
            .GroupBy(v => v.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateSkusInPayload.Any())
        {
            return ResultDto<bool>.Failure($"พบรหัส SKU ซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateSkusInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
        }

        var duplicateBarcodesInPayload = dto.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Barcode))
            .GroupBy(v => v.Barcode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
            
        if (duplicateBarcodesInPayload.Any())
        {
            return ResultDto<bool>.Failure($"พบรหัสบาร์โค้ดซ้ำกันเองภายในรายการที่คุณกรอกเข้ามา: '{string.Join(", ", duplicateBarcodesInPayload)}' กรุณาแก้ไขไม่ให้ซ้ำกัน");
        }

        using var connection = connectionFactory.CreateConnection() as System.Data.Common.DbConnection;
        if (connection == null) throw new InvalidOperationException("Could not create DbConnection.");
        await connection.OpenAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var filesToCheckForDeletion = new List<string>();

            var incomingVariantIds = dto.Variants
                .Where(x => x.VariantId.HasValue && x.VariantId.Value > 0)
                .Select(x => x.VariantId!.Value)
                .ToList();

            var existingVariantIds = await productRepository.GetExistingVariantIdsAsync(productId, transaction, cancellationToken);
            var variantsToDelete = existingVariantIds.Except(incomingVariantIds).ToList();

            if (variantsToDelete.Any())
            {
                var imagesOfDeletedVariants = await productRepository.GetVariantImagesAsync(variantsToDelete, transaction, cancellationToken);
                filesToCheckForDeletion.AddRange(imagesOfDeletedVariants.Select(x => x.ImageUrl).Where(img => img != null)!);

                await productRepository.DeleteProductVariantsAsync(variantsToDelete, transaction, cancellationToken);
            }

            var existingVariantsToUpdate = dto.Variants.Where(v => v.VariantId != null && v.VariantId > 0).Select(v => v.VariantId!.Value).ToList();
            if (!existingVariantsToUpdate.Any())
            {
                existingVariantsToUpdate.Add(-1);
            }

            var barcodes = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Barcode)).Select(v => v.Barcode).ToList();
            var skus = dto.Variants.Where(v => !string.IsNullOrWhiteSpace(v.Sku)).Select(v => v.Sku).ToList();

            if (barcodes.Any() || skus.Any())
            {
                if (barcodes.Any())
                {
                    var existingBarcodes = await productRepository.CheckBarcodesExistAsync(barcodes, existingVariantsToUpdate, transaction, cancellationToken);
                    if (existingBarcodes.Any())
                    {
                        return ResultDto<bool>.Failure($"รหัสบาร์โค้ด '{string.Join(", ", existingBarcodes)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                    }
                }
                
                if (skus.Any())
                {
                    var existingSkus = await productRepository.CheckSkusExistAsync(skus, existingVariantsToUpdate, transaction, cancellationToken);
                    if (existingSkus.Any())
                    {
                        return ResultDto<bool>.Failure($"รหัส SKU '{string.Join(", ", existingSkus)}' มีอยู่ในระบบแล้ว ไม่สามารถใช้ซ้ำได้");
                    }
                }
            }

            int rowsAffected = await productRepository.UpdateProductAsync(productId, dto, transaction, cancellationToken);
            if (rowsAffected == 0)
            {
                return ResultDto<bool>.Failure("ไม่พบสินค้าที่ต้องการแก้ไข");
            }

            var existingVarIdsForUpdate = dto.Variants
                .Where(v => v.VariantId != null && v.VariantId > 0)
                .Select(v => v.VariantId!.Value)
                .ToList();

            Dictionary<int, string?> oldImageMap = new();
            Dictionary<int, byte[]> rowVersionMap = new();
            
            if (existingVarIdsForUpdate.Any())
            {
                var oldImages = await productRepository.GetVariantImagesAsync(existingVarIdsForUpdate, transaction, cancellationToken);
                var stockSnapshots = await productRepository.GetStockRowVersionsAsync(existingVarIdsForUpdate, transaction, cancellationToken);

                oldImageMap = oldImages.ToDictionary(x => x.VariantId, x => x.ImageUrl);
                rowVersionMap = stockSnapshots.ToDictionary(x => x.VariantId, x => x.RowVersion);
            }

            var newVariants = dto.Variants.Where(v => v.VariantId == null || v.VariantId == 0).ToList();
            if (newVariants.Any())
            {
                foreach (var v in newVariants)
                {
                    int vId = await productRepository.InsertProductVariantAsync(productId, v, transaction, cancellationToken);
                    await productRepository.InsertProductPriceAsync(vId, v.BasePrice, v.DiscountPrice, transaction, cancellationToken);
                    await stockRepository.InsertStockAsync(vId, v.CurrentQuantity, v.ReorderPoint, transaction, cancellationToken);

                    if (dto.IsStockTracked && v.CurrentQuantity > 0)
                    {
                        var txModel = new StockTransactionModel
                        {
                            VariantId = vId,
                            TransactionType = "IN",
                            Quantity = v.CurrentQuantity,
                            UnitCost = 0.00m,
                            Notes = "บันทึกยอดตั้งต้นจากการเพิ่ม SKU ใหม่ในโหมดแก้ไข",
                            CreatedBy = dto.UpdatedBy ?? dto.CreatedBy
                        };
                        await stockRepository.CreateTransactionAsync(txModel, transaction);
                    }
                }
            }

            var existingVariants = dto.Variants.Where(v => v.VariantId != null && v.VariantId > 0).ToList();
            if (existingVariants.Any())
            {
                var updateDataList = existingVariants.Select(v => {
                    int variantId = v.VariantId!.Value;
                    if (oldImageMap.TryGetValue(variantId, out var oldImageUrl))
                    {
                        if (!string.Equals(oldImageUrl, v.ImageUrl, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(oldImageUrl))
                        {
                            filesToCheckForDeletion.Add(oldImageUrl);
                        }
                    }
                    return new {
                        UVariantId = variantId,
                        URowVersion = rowVersionMap.GetValueOrDefault(variantId, Array.Empty<byte>()),
                        v.Sku, v.Barcode, v.VariantNameTh, v.VariantNameEn, v.Color, v.SizeLabel, v.StylePattern, v.UnitOfMeasure,
                        v.Width, v.Length, v.Height, v.Weight, v.ImageUrl,
                        v.BasePrice, v.DiscountPrice, v.CurrentQuantity, v.ReorderPoint
                    };
                }).ToList();

                await productRepository.UpdateProductVariantsAndPricesAsync(updateDataList, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            if (filesToCheckForDeletion.Any())
            {
                imageCleanup.Writer.TryWrite(filesToCheckForDeletion.Distinct().ToList());
            }

            return ResultDto<bool>.Success(true);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
