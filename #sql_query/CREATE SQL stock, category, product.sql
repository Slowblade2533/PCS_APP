USE PCS_APP
GO

-- 1. ตารางหมวดหมู่สินค้า (Categories)
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    ParentId INT NULL,
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES Categories(CategoryId)
);

-- 2. ตารางข้อมูลหลักสินค้า (Products)
-- เชื่อมต่อกับตาราง Users ของคุณผ่าน CreatedBy -> Users(Id)
CREATE TABLE Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    ProductNameTh NVARCHAR(255) NOT NULL,
    ProductNameEn NVARCHAR(255) NULL,
    Description NVARCHAR(MAX) NULL,
    BrandName NVARCHAR(100) NULL,
    CategoryId INT NOT NULL,
    ProductType NVARCHAR(20) NOT NULL DEFAULT 'Product', 
    IsStockTracked BIT NOT NULL DEFAULT 1,
    ProductStatus NVARCHAR(20) NOT NULL DEFAULT 'Available', 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NULL, -- รหัสผู้ใช้ที่เพิ่มสินค้า (ผูกกับตาราง Users ของคุณ)
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Products_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE SET NULL,
    CONSTRAINT CHK_ProductType CHECK (ProductType IN ('Product', 'Service', 'Consumable')),
    CONSTRAINT CHK_ProductStatus CHECK (ProductStatus IN ('Available', 'Unavailable', 'Discontinued', 'Internal_Use'))
);

-- 3. ตารางข้อมูลสินค้าหน่วยย่อย (ProductVariants)
CREATE TABLE ProductVariants (
    VariantId INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    Sku NVARCHAR(50) NOT NULL UNIQUE,
    Barcode NVARCHAR(50) NULL UNIQUE, 
    ImageUrl NVARCHAR(2048) NULL,
    VariantStatus NVARCHAR(20) NOT NULL DEFAULT 'Active', 
    SizeLabel NVARCHAR(50) NULL DEFAULT '',
    Color NVARCHAR(50) NULL DEFAULT '',
    StylePattern NVARCHAR(50) NULL DEFAULT '',
    UnitOfMeasure NVARCHAR(50) NOT NULL DEFAULT N'อัน',
    
    -- มิติขนาดและน้ำหนัก
    Width DECIMAL(10,2) NOT NULL DEFAULT 0.01,
    Length DECIMAL(10,2) NOT NULL DEFAULT 0.01,
    Height DECIMAL(10,2) NOT NULL DEFAULT 0.01,
    DimensionUnit NVARCHAR(10) NOT NULL DEFAULT 'cm',
    Weight DECIMAL(10,2) NOT NULL DEFAULT 0.01,
    WeightUnit NVARCHAR(10) NOT NULL DEFAULT 'g',
    Capacity DECIMAL(10,2) NULL,
    CapacityUnit NVARCHAR(10) NULL,
    
    -- ข้อมูลการสั่งซื้อควบคุมขั้นต่ำ-สูงสุด
    MinOrderQty INT NOT NULL DEFAULT 1,
    MaxOrderQty INT NULL,
    CONSTRAINT FK_ProductVariants_Products FOREIGN KEY (ProductId) REFERENCES Products(ProductId) ON DELETE CASCADE
);

-- 4. ตารางราคาและต้นทุน (ProductPrices)
CREATE TABLE ProductPrices (
    VariantId INT PRIMARY KEY,
    CurrencyCode NVARCHAR(3) NOT NULL DEFAULT 'THB',
    BasePrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    DiscountPrice DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    
    -- Computed Column คำนวณเปอร์เซ็นต์ส่วนลดให้อัตโนมัติแบบเรียลไทม์
    DiscountPercent AS (
        CASE WHEN BasePrice > 0 
        THEN CAST(((BasePrice - DiscountPrice) / BasePrice) * 100 AS DECIMAL(5,2)) 
        ELSE 0 END
    ) PERSISTED,
    
    EffectiveFrom DATETIME2 NULL,
    EffectiveTo DATETIME2 NULL,
    AvgCost90Days DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MinCost90Days DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    MaxCost90Days DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_ProductPrices_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId) ON DELETE CASCADE
);

-- 5. ตารางจำนวนสต็อกปัจจุบัน (Stocks)
CREATE TABLE Stocks (
    VariantId INT PRIMARY KEY,
    CurrentQuantity INT NOT NULL DEFAULT 0,
    ReservedQuantity INT NOT NULL DEFAULT 0,
    
    -- Computed Column ยอดสต็อกพร้อมขายจริง (ดึงค่านี้นำไปแสดงผลหน้าเว็บ)
    AvailableQuantity AS (CurrentQuantity - ReservedQuantity) PERSISTED,
    
    ReorderPoint INT NOT NULL DEFAULT 0,
    MaxStockLevel INT NULL,
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT FK_Stocks_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId) ON DELETE CASCADE
);

-- 6. ตารางประวัติการเข้า-ออกของสต็อก (StockTransactions)
-- เชื่อมต่อกับตาราง Users ของคุณผ่าน CreatedBy -> Users(Id) เพื่อบันทึกประวัติการกระทำของพนักงาน
CREATE TABLE StockTransactions (
    TransactionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    VariantId INT NOT NULL,
    TransactionType NVARCHAR(20) NOT NULL, -- 'IN', 'OUT', 'ADJUST'
    Quantity INT NOT NULL, 
    UnitCost DECIMAL(18,2) NULL, 
    ReferenceDoc NVARCHAR(50) NULL, 
    Notes NVARCHAR(255) NULL, 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy INT NULL, -- พนักงานที่เป็นคนทำรายการ (ผูกกับตาราง Users ของคุณ)
    CONSTRAINT FK_StockTransactions_Variants FOREIGN KEY (VariantId) REFERENCES ProductVariants(VariantId),
    CONSTRAINT FK_StockTransactions_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Id) ON DELETE SET NULL,
    CONSTRAINT CHK_TransactionType CHECK (TransactionType IN ('IN', 'OUT', 'ADJUST')),
    CONSTRAINT CHK_TransactionQty CHECK (Quantity <> 0)
);
GO