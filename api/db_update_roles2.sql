USE PCS_APP;
GO

-- 1. Create Roles first so FK won't fail
-- We might have failed to create FK before.
DELETE FROM dbo.RolePermissions;
-- Remove existing roles safely
EXEC sp_MSforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT ALL";
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
EXEC sp_MSforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL";
GO

-- Give full permissions to roles 1-6
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE r.Id <= 6;
GO

-- 2. Add RoleId to Users and set FK
-- If it exists, we skip adding it.
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'RoleId')
BEGIN
    ALTER TABLE dbo.Users ADD RoleId INT NOT NULL DEFAULT 12;
END
GO
-- Update all existing users to have a valid RoleId
UPDATE dbo.Users SET RoleId = 1 WHERE RoleId NOT IN (SELECT Id FROM dbo.Roles);
GO

-- Create FK if not exists
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('dbo.FK_Users_Roles'))
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
END
GO

-- 3. Modify UserPermissions
-- Drop constraints
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'DF_UserPermissions_ScopeType' AND type = 'D')
BEGIN
    ALTER TABLE dbo.UserPermissions DROP CONSTRAINT DF_UserPermissions_ScopeType;
END
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'CK_UserPermissions_ScopeType' AND type = 'C')
BEGIN
    ALTER TABLE dbo.UserPermissions DROP CONSTRAINT CK_UserPermissions_ScopeType;
END
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ_UserPermissions_Assignment' AND type = 'UQ')
BEGIN
    ALTER TABLE dbo.UserPermissions DROP CONSTRAINT UQ_UserPermissions_Assignment;
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserPermissions') AND name = 'ScopeType')
BEGIN
    ALTER TABLE dbo.UserPermissions DROP COLUMN ScopeType;
END
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserPermissions') AND name = 'ScopeId')
BEGIN
    ALTER TABLE dbo.UserPermissions DROP COLUMN ScopeId;
END

-- Add new Unique constraint for UserId + PermissionId
IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ_UserPermissions_UserId_PermissionId' AND type = 'UQ')
BEGIN
    ALTER TABLE dbo.UserPermissions ADD CONSTRAINT UQ_UserPermissions_UserId_PermissionId UNIQUE (UserId, PermissionId);
END
GO
