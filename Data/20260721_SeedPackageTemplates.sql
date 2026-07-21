USE GymManagementDB;
GO

-- Package catalog used by the membership assignment screen.
IF NOT EXISTS (SELECT 1 FROM dbo.PackageTemplates WHERE PackageName = 'Basic 1 Month')
    INSERT INTO dbo.PackageTemplates (PackageName, Price, DurationMonths, HasPT, PTMinutesPerSession, PTSessions)
    VALUES ('Basic 1 Month', 500000, 1, 0, 0, 0);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PackageTemplates WHERE PackageName = 'Standard 3 Months')
    INSERT INTO dbo.PackageTemplates (PackageName, Price, DurationMonths, HasPT, PTMinutesPerSession, PTSessions)
    VALUES ('Standard 3 Months', 1200000, 3, 0, 0, 0);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PackageTemplates WHERE PackageName = 'Premium 1 Month')
    INSERT INTO dbo.PackageTemplates (PackageName, Price, DurationMonths, HasPT, PTMinutesPerSession, PTSessions)
    VALUES ('Premium 1 Month', 1000000, 1, 1, 60, 6);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PackageTemplates WHERE PackageName = 'Premium 3 Months')
    INSERT INTO dbo.PackageTemplates (PackageName, Price, DurationMonths, HasPT, PTMinutesPerSession, PTSessions)
    VALUES ('Premium 3 Months', 2500000, 3, 1, 60, 12);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PackageTemplates WHERE PackageName = 'VIP 6 Months')
    INSERT INTO dbo.PackageTemplates (PackageName, Price, DurationMonths, HasPT, PTMinutesPerSession, PTSessions)
    VALUES ('VIP 6 Months', 4800000, 6, 1, 60, 24);
GO

SELECT Id, PackageName, Price, DurationMonths, HasPT, PTSessions, PTMinutesPerSession
FROM dbo.PackageTemplates
ORDER BY Price;
GO
