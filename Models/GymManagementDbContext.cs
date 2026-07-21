using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GymManagement.Models;

public partial class GymManagementDbContext : DbContext
{
    public GymManagementDbContext()
    {
    }

    public GymManagementDbContext(DbContextOptions<GymManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CheckInHistory> CheckInHistories { get; set; }

    public virtual DbSet<Equipment> Equipments { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    public virtual DbSet<MaintenanceHistory> MaintenanceHistories { get; set; }

    public virtual DbSet<Member> Members { get; set; }

    public virtual DbSet<MemberPackage> MemberPackages { get; set; }

    public virtual DbSet<PackageTemplate> PackageTemplates { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Ptbooking> Ptbookings { get; set; }

    public virtual DbSet<Ptmedium> Ptmedia { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("GYM_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("GYM_DB_CONNECTION environment variable is not configured.");

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CheckInHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CheckInH__3214EC0760801DC2");

            entity.ToTable("CheckInHistory");

            entity.Property(e => e.CheckInTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CheckOutTime).HasColumnType("datetime");

            entity.HasOne(d => d.Member).WithMany(p => p.CheckInHistories)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__CheckInHi__Membe__47DBAE45");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Equipmen__3214EC077F9DDBE8");

            entity.HasIndex(e => e.EquipmentCode, "UQ__Equipmen__09E4417EBA872660").IsUnique();

            entity.Property(e => e.EquipmentCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.EquipmentName).HasMaxLength(100);
            entity.Property(e => e.EquipmentType).HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Operational");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3214EC074B92224D");

            entity.Property(e => e.FeedbackType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.SubmittedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.TargetPtid).HasColumnName("TargetPTId");

            entity.HasOne(d => d.Equipment).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.EquipmentId)
                .HasConstraintName("FK__Feedbacks__Equip__68487DD7");

            entity.HasOne(d => d.Member).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Feedbacks__Membe__656C112C");

            entity.HasOne(d => d.TargetPt).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TargetPtid)
                .HasConstraintName("FK__Feedbacks__Targe__6754599E");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Invoices__3214EC071212A0C5");

            entity.HasIndex(e => e.InvoiceCode, "UQ__Invoices__0D9D7FF35822BE71").IsUnique();

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountPercent).HasDefaultValue(0);
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .HasDefaultValue("Cash");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Member).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK__Invoices__Member__5AEE82B9");

            entity.HasOne(d => d.User).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Invoices__UserId__59FA5E80");
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__InvoiceD__3214EC072A318043");

            entity.Property(e => e.ItemName).HasMaxLength(100);
            entity.Property(e => e.ItemType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Invoice).WithMany(p => p.InvoiceDetails)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__InvoiceDe__Invoi__619B8048");
        });

        modelBuilder.Entity<MaintenanceHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3214EC0735D310BF");

            entity.ToTable("MaintenanceHistory");

            entity.Property(e => e.Cost)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.LogDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.LogType).HasMaxLength(30);
            entity.Property(e => e.PerformedBy).HasMaxLength(100);

            entity.HasOne(d => d.Equipment).WithMany(p => p.MaintenanceHistories)
                .HasForeignKey(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Maintenan__Equip__5070F446");
        });

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Members__3214EC07F510665B");

            entity.HasIndex(e => e.MemberCode, "UQ__Members__84CA6377FCDFDE2E").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UQ__Members__85FB4E38B92C6B5C").IsUnique();

            entity.Property(e => e.Avatar).HasMaxLength(255);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.MemberCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("(getdate())");

            entity.HasIndex(e => e.UserId, "UX_Members_UserId")
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Members_Users");
        });

        modelBuilder.Entity<MemberPackage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MemberPa__3214EC0780AB6F74");

            entity.Property(e => e.RemainingPtsessions)
                .HasDefaultValue(0)
                .HasColumnName("RemainingPTSessions");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Member).WithMany(p => p.MemberPackages)
                .HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__MemberPac__Membe__3A81B327");

            entity.HasOne(d => d.PackageTemplate).WithMany(p => p.MemberPackages)
                .HasForeignKey(d => d.PackageTemplateId)
                .HasConstraintName("FK__MemberPac__Packa__3B75D760");
        });

        modelBuilder.Entity<PackageTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PackageT__3214EC075556759E");

            entity.Property(e => e.HasPt)
                .HasDefaultValue(false)
                .HasColumnName("HasPT");
            entity.Property(e => e.PackageName).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PtminutesPerSession)
                .HasDefaultValue(0)
                .HasColumnName("PTMinutesPerSession");
            entity.Property(e => e.PtSessions)
                .HasColumnName("PTSessions");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__3214EC07907D3F44");

            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
        });

        modelBuilder.Entity<Ptbooking>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PTBookin__3214EC074CFC74D6");

            entity.ToTable("PTBookings");

            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.Ptid).HasColumnName("PTId");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Member).WithMany(p => p.Ptbookings)
                .HasForeignKey(d => d.MemberId)
                .HasConstraintName("FK__PTBooking__Membe__412EB0B6");

            entity.HasOne(d => d.Pt).WithMany(p => p.Ptbookings)
                .HasForeignKey(d => d.Ptid)
                .HasConstraintName("FK__PTBookings__PTId__4222D4EF");
        });

        modelBuilder.Entity<Ptmedium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PTMedia__3214EC0730ED1F66");

            entity.ToTable("PTMedia");

            entity.Property(e => e.Caption).HasMaxLength(255);
            entity.Property(e => e.MediaType).HasMaxLength(20);
            entity.Property(e => e.MediaUrl).HasMaxLength(255);
            entity.Property(e => e.Ptid).HasColumnName("PTId");
            entity.Property(e => e.UploadedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Pt).WithMany(p => p.Ptmedia)
                .HasForeignKey(d => d.Ptid)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__PTMedia__PTId__2C3393D0");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07142A085E");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E47B7DE742").IsUnique();

            entity.HasIndex(e => e.PhoneNumber, "UQ__Users__85FB4E38ECBAD8AB").IsUnique();

            entity.Property(e => e.Avatar).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.Ptstatus)
                .HasMaxLength(20)
                .HasColumnName("PTStatus");
            entity.Property(e => e.Role).HasMaxLength(20);
            entity.Property(e => e.Specialty).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
