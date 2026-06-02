USE master;
GO

-- 1. เคลียร์ Session ที่เชื่อมต่อค้างอยู่ด้วยชื่อ 'PcsWebApi' ทั้งหมดออกไปก่อน
DECLARE @kill varchar(8000) = '';  
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'  
FROM sys.dm_exec_sessions  
WHERE login_name = 'PcsWebApi';  

EXEC(@kill);
GO

-- 2. เมื่อสายหลุดแล้ว ให้ทำการเปลี่ยนรหัสผ่านและเปิดใช้งานทันที (โดยไม่ต้องลบตัว Login)
-- เปลี่ยน 'YourSecurePassword123!' เป็นรหัสผ่านที่คุณต้องการ
ALTER LOGIN PcsWebApi WITH PASSWORD = 'PCSAdmin123!', CHECK_POLICY = OFF;
ALTER LOGIN PcsWebApi ENABLE;
GO



-- 3. ย้ายมาจัดการระดับฐานข้อมูล PCS_APP
USE PCS_APP;
GO

-- ถ้ามี User ชื่อ pcs_admin ค้างอยู่ (จากเออร์เรอร์รอบที่แล้ว) ให้เคลียร์ออก
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'pcs_admin')
BEGIN
    DROP USER pcs_admin;
END
GO

-- ตรวจสอบและซ่อมแซมสิทธิ์ของ User PcsWebApi ในก้อนฐานข้อมูลนี้
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'PcsWebApi')
BEGIN
    CREATE USER PcsWebApi FOR LOGIN PcsWebApi;
END
GO

-- มอบสิทธิ์ระดับสูงสุด (db_owner) เพื่อให้ Dapper ทำงานได้อย่างไม่มีสะดุด
ALTER ROLE db_owner ADD MEMBER PcsWebApi;
GO