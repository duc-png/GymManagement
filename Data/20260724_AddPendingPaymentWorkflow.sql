USE GymManagementDB;
GO

IF COL_LENGTH('dbo.Invoices', 'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Invoices
    ADD PaymentStatus nvarchar(30) NOT NULL
        CONSTRAINT DF_Invoices_PaymentStatus DEFAULT ('Paid') WITH VALUES;
END;
GO

IF COL_LENGTH('dbo.Invoices', 'ConfirmedDate') IS NULL
    ALTER TABLE dbo.Invoices ADD ConfirmedDate datetime NULL;
GO

IF COL_LENGTH('dbo.Invoices', 'ConfirmedByUserId') IS NULL
    ALTER TABLE dbo.Invoices ADD ConfirmedByUserId int NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Invoices_PaymentStatus'
      AND object_id = OBJECT_ID('dbo.Invoices'))
BEGIN
    CREATE INDEX IX_Invoices_PaymentStatus
        ON dbo.Invoices(PaymentStatus, CreatedDate);
END;
GO
