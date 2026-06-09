USE PCS_APP;
GO

-- 1. Drop UserRoles table
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.UserRoles;
END
GO

-- 2. Add RoleId to Users
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'RoleId')
BEGIN
    ALTER TABLE dbo.Users ADD RoleId INT NOT NULL DEFAULT 12; -- Default to Guest
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
END
GO

-- 3. Modify UserPermissions
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserPermissions') AND name = 'ScopeType')
BEGIN
    -- Drop constraint if exists for ScopeId
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('dbo.FK_UserPermissions_Branches'))
    BEGIN
        ALTER TABLE dbo.UserPermissions DROP CONSTRAINT FK_UserPermissions_Branches;
    END

    ALTER TABLE dbo.UserPermissions DROP COLUMN ScopeType;
    ALTER TABLE dbo.UserPermissions DROP COLUMN ScopeId;
END
GO

-- 4. Re-seed Roles
-- First, remove all role permissions since Roles are being reset
DELETE FROM dbo.RolePermissions;

-- Remove existing roles
-- Need to disable FK check if any other table references Roles (e.g. Users just added it)
ALTER TABLE dbo.Users NOCHECK CONSTRAINT FK_Users_Roles;
DELETE FROM dbo.Roles;

DBCC CHECKIDENT ('dbo.Roles', RESEED, 0);

INSERT INTO dbo.Roles (RoleName, Description) VALUES
('Owner', N'เจ้าของกิจการ'),
('Partnership', N'หุ้นส่วนกิจการ'),
('Shareholder', N'ผู้ถือหุ้น'),
('Chairman', N'ประธานกรรมการ'),
('CEO', N'ประธานเจ้าหน้าที่บริหาร'),
('Director', N'ผู้อำนวยการ'),
('Manager', N'ผู้จัดการ'),
('Stock Staff', N'เจ้าหน้าที่คลังสินค้า'),
('Accountant', N'เจ้าหน้าที่บัญชี'),
('General Staff', N'เจ้าหน้าที่ทั่วไป'),
('IT Administrator', N'เจ้าหน้าที่ระบบไอที'),
('Guest', N'ผู้เยี่ยมชมระบบ');

-- Enable FK check again
ALTER TABLE dbo.Users WITH CHECK CHECK CONSTRAINT FK_Users_Roles;
GO

-- 5. Give full permissions to roles 1-6
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE r.Id <= 6;
GO
