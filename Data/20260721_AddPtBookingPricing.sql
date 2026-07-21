USE GymManagementDB;
GO

IF COL_LENGTH('dbo.Users', 'PTHourlyRate') IS NULL
    ALTER TABLE dbo.Users ADD PTHourlyRate DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.PTBookings', 'BookingType') IS NULL
    ALTER TABLE dbo.PTBookings ADD BookingType VARCHAR(20) NOT NULL CONSTRAINT DF_PTBookings_BookingType DEFAULT 'Package';
GO

IF COL_LENGTH('dbo.PTBookings', 'Price') IS NULL
    ALTER TABLE dbo.PTBookings ADD Price DECIMAL(18,2) NOT NULL CONSTRAINT DF_PTBookings_Price DEFAULT 0;
GO

IF COL_LENGTH('dbo.PTBookings', 'PaymentStatus') IS NULL
    ALTER TABLE dbo.PTBookings ADD PaymentStatus VARCHAR(20) NOT NULL CONSTRAINT DF_PTBookings_PaymentStatus DEFAULT 'Included';
GO

IF COL_LENGTH('dbo.PTBookings', 'MemberPackageId') IS NULL
    ALTER TABLE dbo.PTBookings ADD MemberPackageId INT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PTBookings_MemberPackages')
    ALTER TABLE dbo.PTBookings ADD CONSTRAINT FK_PTBookings_MemberPackages FOREIGN KEY (MemberPackageId) REFERENCES dbo.MemberPackages(Id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PTBookings_BookingType')
    ALTER TABLE dbo.PTBookings ADD CONSTRAINT CHK_PTBookings_BookingType CHECK (BookingType IN ('Package', 'Extra'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CHK_PTBookings_PaymentStatus')
    ALTER TABLE dbo.PTBookings ADD CONSTRAINT CHK_PTBookings_PaymentStatus CHECK (PaymentStatus IN ('Included', 'Pending', 'Paid'));
GO
