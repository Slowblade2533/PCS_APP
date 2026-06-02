USE PCS_APP;
GO

-- ล้างข้อมูลเก่า (ถ้ามี) ป้องกัน Error Unique Constraint
DELETE FROM Users WHERE Email = 'superuser@email.com';

-- แทรกข้อมูล Superuser (PasswordHash ด้านล่างถอดรหัสออกมาเป็น "PcsAdmin123!" ด้วยระบบ .NET)
INSERT INTO Users (Email, PasswordHash, FullName, IsActive)
VALUES (
    'superuser@email.com', 
    'AQAAAAIAAYagAAAAEP0w+MIn7+R9yv0CIsfU5P7b+vQo7bN95wN3Yt6fXlI7N8rQ9eG+b8vP9e+A==', -- ค่า Hash มาตรฐาน .NET
    'Super User', 
    1
);

-- ตรวจสอบข้อมูลหลังแทรก
SELECT * FROM Users;