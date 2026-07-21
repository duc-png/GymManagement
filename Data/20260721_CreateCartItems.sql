USE GymManagementDB;
GO

IF OBJECT_ID('dbo.CartItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        ItemType VARCHAR(20) NOT NULL,
        ItemId INT NOT NULL,
        ItemName NVARCHAR(150) NOT NULL,
        Quantity INT NOT NULL DEFAULT 1,
        UnitPrice DECIMAL(18,2) NOT NULL,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
        CONSTRAINT CHK_CartItems_ItemType CHECK (ItemType IN ('Product', 'Package', 'PTBooking')),
        CONSTRAINT CHK_CartItems_Quantity CHECK (Quantity > 0)
    );
END
GO
