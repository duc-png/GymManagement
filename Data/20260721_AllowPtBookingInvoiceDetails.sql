USE GymManagementDB;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_Item_Type')
    ALTER TABLE dbo.InvoiceDetails DROP CONSTRAINT CHK_Item_Type;
GO

ALTER TABLE dbo.InvoiceDetails
ADD CONSTRAINT CHK_Item_Type
CHECK (ItemType IN ('Package', 'Product', 'PTBooking'));
GO
