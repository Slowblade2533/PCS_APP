USE PCS_APP
GO

-- 1. ตารางเก็บข้อมูลผู้ใช้งาน
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL, -- เก็บแบบ Hashed เสมอ
    FullName NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    IsActive BIT DEFAULT 1
);

-- 2. ตารางเก็บ Session การ Login บน Server (ห้ามเก็บฝั่ง Client)
CREATE TABLE UserSessions (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId INT NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsRevoked BIT DEFAULT 0,
    CONSTRAINT FK_UserSessions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- สร้าง Index เพื่อความเร็วในการตรวจสอบ Session
CREATE NONCLUSTERED INDEX IX_UserSessions_ExpiresAt_IsRevoked 
ON UserSessions(SessionId) INCLUDE (UserId, ExpiresAt, IsRevoked);