USE [PCS_APP];
GO

PRINT '========================================================================'
PRINT ' เริ่มกระบวนการตรวจสอบและปรับปรุงโครงสร้างฐานข้อมูลระบบ PCS_APP '
PRINT '========================================================================'

-- ----------------------------------------------------------------------
-- 1. แก้ไขข้อจำกัด UNIQUE ของ Barcode ในตาราง ProductVariants (Critical Fix)
-- ----------------------------------------------------------------------
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @ConstraintName NVARCHAR(255) = NULL;

    -- ค้นหาชื่อกุญแจข้อจำกัดที่เป็นระบบสุ่มที่สร้างครอบคอลัมน์ Barcode ไว้
    SELECT @ConstraintName = kc.name
    FROM sys.key_constraints kc
    INNER JOIN sys.indexes i ON kc.parent_object_id = i.object_id AND kc.unique_index_id = i.index_id
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE kc.parent_object_id = OBJECT_ID('dbo.ProductVariants') 
      AND c.name = 'Barcode' AND kc.type = 'UQ';

    IF @ConstraintName IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [dbo].[ProductVariants] DROP CONSTRAINT [' + @ConstraintName + '];');
        PRINT '✔ ดร็อปข้อจำกัดยูนีคเดิมที่พ่นบั๊กของ Barcode สำเร็จ: [' + @ConstraintName + ']';
    END

    -- สร้างดัชนียูนีคแบบติดฟิลเตอร์ (Unique Filtered Index)
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UX_ProductVariants_Barcode' AND object_id = OBJECT_ID('dbo.ProductVariants'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [UX_ProductVariants_Barcode]
        ON [dbo].[ProductVariants]([Barcode] ASC)
        WHERE [Barcode] IS NOT NULL;
        PRINT '✔ สร้างดัชนีแบบพิเศษ UX_ProductVariants_Barcode สำเร็จ (ข้ามค่า NULL อัตโนมัติ)';
    END
COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ เกิดข้อผิดพลาดในขั้นตอนแก้ไขโครงสร้าง Barcode: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ----------------------------------------------------------------------
-- 2. ปรับปรุงดัชนีตาราง UserSessions ให้ทำงานได้ถูกต้องตามสถาปัตยกรรม
-- ----------------------------------------------------------------------
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSessions_ExpiresAt_IsRevoked' AND object_id = OBJECT_ID('dbo.UserSessions'))
BEGIN
    DROP INDEX [IX_UserSessions_ExpiresAt_IsRevoked] ON [dbo].[UserSessions];
    PRINT '✔ ดร็อปดัชนีที่ทำงานซ้ำซ้อนตัวเก่าบนตาราง UserSessions สำเร็จ';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSessions_UserId_ExpiresAt' AND object_id = OBJECT_ID('dbo.UserSessions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserSessions_UserId_ExpiresAt] ON [dbo].[UserSessions]
    (
        [UserId] ASC,
        [ExpiresAt] ASC
    )
    INCLUDE ([IsRevoked]);
    PRINT '✔ สร้างดัชนีประสิทธิภาพสูงชุดใหม่ IX_UserSessions_UserId_ExpiresAt สำเร็จ';
END
GO

-- ----------------------------------------------------------------------
-- 3. แก้ไข Foreign Key ของ StockTransactions ให้เป็นแบบ ON DELETE CASCADE
-- ----------------------------------------------------------------------
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_StockTransactions_Variants')
BEGIN
    ALTER TABLE [dbo].[StockTransactions] DROP CONSTRAINT [FK_StockTransactions_Variants];
    PRINT '✔ ปลดสัญญากุญแจนอกตัวเก่าของ StockTransactions สำเร็จ';
END

ALTER TABLE [dbo].[StockTransactions] WITH CHECK ADD CONSTRAINT [FK_StockTransactions_Variants] 
FOREIGN KEY([VariantId]) REFERENCES [dbo].[ProductVariants] ([VariantId])
ON DELETE CASCADE;

ALTER TABLE [dbo].[StockTransactions] CHECK CONSTRAINT [FK_StockTransactions_Variants];
PRINT '✔ ปรับปรุงเงื่อนไขกุญแจนอกเป็นแบบ ON DELETE CASCADE ให้กับตาราง StockTransactions สำเร็จ';
GO

-- ----------------------------------------------------------------------
-- 4. เพิ่มประสิทธิภาพการทำงานของระบบผ่านการทำ Non-Clustered Index บน Foreign Keys
-- ----------------------------------------------------------------------
-- ดัชนีช่วยเสริมความเร็วในการดึงข้อมูลหมวดหมู่สินค้าหลัก
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_CategoryId' AND object_id = OBJECT_ID('dbo.Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId] ASC);
    PRINT '✔ เพิ่มดัชนีเร่งความเร็วระบบค้นหาข้อมูลสินค้าหลัก (IX_Products_CategoryId)';
END

-- ดัชนีช่วยเร่งประสิทธิภาพการทำงานของการ Join และความเร็วในการ CASCADE DELETE ข้อมูลย่อย
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductVariants_ProductId' AND object_id = OBJECT_ID('dbo.ProductVariants'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_ProductVariants_ProductId] ON [dbo].[ProductVariants]([ProductId] ASC);
    PRINT '✔ เพิ่มดัชนีเร่งความเร็วระบบความสัมพันธ์สินค้าและหน่วยย่อย (IX_ProductVariants_ProductId)';
END

-- ดัชนีช่วยเร่งความเร็วในการดึงรายงานหรือประวัติการเดินสต็อกของหน่วยสินค้านั้นๆ
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StockTransactions_VariantId' AND object_id = OBJECT_ID('dbo.StockTransactions'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_StockTransactions_VariantId] ON [dbo].[StockTransactions]([VariantId] ASC);
    PRINT '✔ เพิ่มดัชนีเร่งความเร็วการเรียกดูสมุดบัญชีประวัติสต็อก (IX_StockTransactions_VariantId)';
END
GO

-- ----------------------------------------------------------------------
-- 5. เพิ่ม Check Constraint ควบคุมขอบเขตสถานะของตาราง ProductVariants
-- ----------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CHK_VariantStatus')
BEGIN
    ALTER TABLE [dbo].[ProductVariants] WITH CHECK ADD CONSTRAINT [CHK_VariantStatus] 
    CHECK ([VariantStatus] = 'Active' OR [VariantStatus] = 'Inactive' OR [VariantStatus] = 'Draft');
    
    ALTER TABLE [dbo].[ProductVariants] CHECK CONSTRAINT [CHK_VariantStatus];
    PRINT '✔ เปิดใช้งานข้อจำกัดข้อมูลเพื่อควบคุมความถูกต้องของสถานะหน่วยย่อย (CHK_VariantStatus)';
END
GO

PRINT '========================================================================'
PRINT ' ปรับปรุงระบบฐานข้อมูลทั้งหมดเสร็จสมบูรณ์เรียบร้อยแล้ว! '
PRINT '========================================================================'