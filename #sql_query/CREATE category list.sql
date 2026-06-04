USE PCS_APP
GO

INSERT INTO Categories
(
    CategoryName,
    Description
)
VALUES
(N'เตาปิ้งย่าง', N'เตาปิ้งย่างทุกประเภท'),
(N'ตะแกรงปิ้งย่าง', N'ตะแกรงปิ้งย่าง'),
(N'กระทะย่าง', N'กระทะย่าง');

SELECT CategoryId, CategoryName
FROM Categories