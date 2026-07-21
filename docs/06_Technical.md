# Technical Reference

## Architecture Stack

| Item | Value |
|------|-------|
| Platform | C# WPF (.NET) |
| ORM | Entity Framework Core — Database First |
| Database | SQL Server — `GymManagementDB` |
| Pattern | MVVM (`CommunityToolkit.Mvvm`) |
| Security | BCrypt (`BCrypt.Net-Next` NuGet) |
| Charts | LiveCharts2 (WPF) |
| PDF | (chọn: QuestPDF hoặc PdfSharp) |

### Seed password
Raw: `"123"` → BCrypt hash: `$2a$11$wK74iN3rM1wsh7mXm9/MCO0R.hB/0fE2k8vK3/bW3g3R1b0L1bX2a`

---

## Database Schema

```sql
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Password VARCHAR(255) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(15) UNIQUE NOT NULL,
    Avatar NVARCHAR(255),
    Role NVARCHAR(20) NOT NULL CONSTRAINT CHK_User_Role CHECK (Role IN ('Admin','Receptionist','PT','Member')),
    Status NVARCHAR(20) DEFAULT 'Active' CONSTRAINT CHK_User_Status CHECK (Status IN ('Active','Locked')),
    Specialty NVARCHAR(100) NULL,
    PTStatus NVARCHAR(20) NULL CONSTRAINT CHK_PT_Status CHECK (PTStatus IN ('Available','Busy','OnLeave'))
);
CREATE TABLE PTMedia (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PTId INT FOREIGN KEY REFERENCES Users(Id) ON DELETE CASCADE,
    MediaType NVARCHAR(20) NOT NULL CONSTRAINT CHK_PTMedia_Type CHECK (MediaType IN ('Image','Video')),
    MediaUrl NVARCHAR(255) NOT NULL,
    Caption NVARCHAR(255) NULL,
    UploadedDate DATETIME DEFAULT GETDATE()
);
CREATE TABLE PackageTemplates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PackageName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    DurationMonths INT NOT NULL,
    HasPT BIT DEFAULT 0,
    PTMinutesPerSession INT DEFAULT 0
);
CREATE TABLE Members (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberCode VARCHAR(20) UNIQUE NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE,
    Gender NVARCHAR(10) CONSTRAINT CHK_Member_Gender CHECK (Gender IN ('Male','Female','Other')),
    PhoneNumber VARCHAR(15) UNIQUE NOT NULL,
    Email VARCHAR(100) NULL,
    Avatar NVARCHAR(255),
    RegistrationDate DATE DEFAULT GETDATE(),
    UserId INT NULL FOREIGN KEY REFERENCES Users(Id) ON DELETE SET NULL
);
CREATE TABLE MemberPackages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT FOREIGN KEY REFERENCES Members(Id) ON DELETE CASCADE,
    PackageTemplateId INT FOREIGN KEY REFERENCES PackageTemplates(Id),
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    RemainingPTSessions INT DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Active' CONSTRAINT CHK_MemberPackage_Status CHECK (Status IN ('Active','Expiring','Expired'))
);
CREATE TABLE PTBookings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT FOREIGN KEY REFERENCES Members(Id),
    PTId INT FOREIGN KEY REFERENCES Users(Id),
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending' CONSTRAINT CHK_Booking_Status CHECK (Status IN ('Pending','Completed','Cancelled')),
    CONSTRAINT CHK_Booking_Time CHECK (EndTime > StartTime)
);
CREATE TABLE CheckInHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT FOREIGN KEY REFERENCES Members(Id) ON DELETE CASCADE,
    CheckInTime DATETIME DEFAULT GETDATE(),
    CheckOutTime DATETIME NULL
);
CREATE TABLE Equipments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EquipmentCode VARCHAR(30) UNIQUE NOT NULL,
    EquipmentName NVARCHAR(100) NOT NULL,
    EquipmentType NVARCHAR(50) NOT NULL,
    Location NVARCHAR(100),
    PurchaseDate DATE NOT NULL,
    Status NVARCHAR(30) DEFAULT 'Operational'
        CONSTRAINT CHK_Equipment_Status CHECK (Status IN ('Operational','Broken','UnderMaintenance','Disposed'))
);
CREATE TABLE MaintenanceHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    EquipmentId INT FOREIGN KEY REFERENCES Equipments(Id) ON DELETE CASCADE,
    LogDate DATETIME DEFAULT GETDATE(),
    LogType NVARCHAR(30) NOT NULL
        CONSTRAINT CHK_Maintenance_LogType CHECK (LogType IN ('IssueReport','RoutineMaintenance','Repair')),
    Description NVARCHAR(MAX) NOT NULL,
    Cost DECIMAL(18,2) DEFAULT 0,
    PerformedBy NVARCHAR(100),
    Notes NVARCHAR(MAX)
);
CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(100) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    StockQuantity INT DEFAULT 0
);
CREATE TABLE Invoices (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceCode VARCHAR(50) UNIQUE NOT NULL,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    MemberId INT FOREIGN KEY REFERENCES Members(Id),
    CreatedDate DATETIME DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    DiscountPercent INT DEFAULT 0,
    FinalAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL DEFAULT 'Cash'
        CONSTRAINT CHK_Invoice_PaymentMethod CHECK (PaymentMethod IN ('Cash','Card','Transfer','MoMo'))
);
CREATE TABLE InvoiceDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceId INT FOREIGN KEY REFERENCES Invoices(Id) ON DELETE CASCADE,
    ItemType VARCHAR(20) NOT NULL CONSTRAINT CHK_Item_Type CHECK (ItemType IN ('Package','Product')),
    ItemId INT NOT NULL,
    ItemName NVARCHAR(100) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL
);
CREATE TABLE Feedbacks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MemberId INT FOREIGN KEY REFERENCES Members(Id) ON DELETE CASCADE,
    FeedbackType VARCHAR(20) NOT NULL CONSTRAINT CHK_Feedback_Type CHECK (FeedbackType IN ('PT','Facility')),
    TargetPTId INT NULL FOREIGN KEY REFERENCES Users(Id),
    EquipmentId INT NULL FOREIGN KEY REFERENCES Equipments(Id),
    RatingStars INT CONSTRAINT CHK_Rating_Stars CHECK (RatingStars >= 1 AND RatingStars <= 5) NULL,
    Comment NVARCHAR(MAX) NOT NULL,
    SubmittedDate DATETIME DEFAULT GETDATE()
);
```

## Seed Data

```sql
INSERT INTO Users (Username, Password, FullName, PhoneNumber, Role, Specialty, PTStatus) VALUES
('admin',       '$2a$11$wK74iN3rM1wsh7mXm9/MCO0R.hB/0fE2k8vK3/bW3g3R1b0L1bX2a', N'Nguyễn Văn Chủ Tịch', '0912345678', 'Admin',        NULL,                   NULL),
('reception',   '$2a$11$wK74iN3rM1wsh7mXm9/MCO0R.hB/0fE2k8vK3/bW3g3R1b0L1bX2a', N'Trần Thị Lễ Tân',      '0987654321', 'Receptionist', NULL,                   NULL),
('pt_tiendat',  '$2a$11$wK74iN3rM1wsh7mXm9/MCO0R.hB/0fE2k8vK3/bW3g3R1b0L1bX2a', N'Phạm Tiến Đạt (PT)',   '0901112223', 'PT',           N'Thể hình & Tăng cơ', 'Available'),
('pt_hoangyen', '$2a$11$wK74iN3rM1wsh7mXm9/MCO0R.hB/0fE2k8vK3/bW3g3R1b0L1bX2a', N'Hoàng Yến (PT)',       '0904445556', 'PT',           N'Yoga & Giảm cân',    'Available');
```
