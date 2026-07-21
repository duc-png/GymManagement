USE GymManagementDB;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Nước suối 500ml')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Nước suối 500ml', 10000, 100);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Nước điện giải')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Nước điện giải', 25000, 60);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Khăn tập gym')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Khăn tập gym', 75000, 30);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Găng tay tập gym')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Găng tay tập gym', 180000, 20);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Dây kháng lực')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Dây kháng lực', 120000, 25);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Whey Protein 1kg')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Whey Protein 1kg', 850000, 15);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductName = N'Bình nước thể thao')
    INSERT INTO dbo.Products (ProductName, Price, StockQuantity)
    VALUES (N'Bình nước thể thao', 150000, 35);
GO

SELECT Id, ProductName, Price, StockQuantity
FROM dbo.Products
ORDER BY Id;
GO
