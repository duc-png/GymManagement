using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record DashboardMetric(string Label, decimal Value);
public sealed record DashboardData(int TotalMembers, int ActiveMembers, int PendingBookings, int BrokenEquipment,
    decimal Revenue, List<DashboardMetric> RevenueByPayment, List<DashboardMetric> PackageSales,
    List<DashboardMetric> ProductSales, double AverageTrainingMinutes);

public class DashboardService
{
    public async Task<DashboardData> GetAsync(DateTime from, DateTime to)
    {
        using var db = new GymManagementDbContext();
        var toExclusive = to.Date.AddDays(1);
        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(x => x.PaymentStatus == PaymentStatuses.Paid
                && (x.ConfirmedDate ?? x.CreatedDate) >= from.Date
                && (x.ConfirmedDate ?? x.CreatedDate) < toExclusive)
            .ToListAsync();

        var details = await db.InvoiceDetails
            .AsNoTracking()
            .Where(x => x.Invoice != null
                && x.Invoice.PaymentStatus == PaymentStatuses.Paid
                && (x.Invoice.ConfirmedDate ?? x.Invoice.CreatedDate) >= from.Date
                && (x.Invoice.ConfirmedDate ?? x.Invoice.CreatedDate) < toExclusive)
            .ToListAsync();

        var checkIns = await db.CheckInHistories
            .AsNoTracking()
            .Where(x => x.CheckInTime >= from.Date && x.CheckInTime < toExclusive && x.CheckOutTime != null)
            .ToListAsync();

        var totalMembers = await db.Members
            .CountAsync();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var activeMembers = await db.MemberPackages
        .Where(x => x.Status == "Active" && x.StartDate <= today && x.EndDate >= today)
        .Select(x => x.MemberId)
        .Distinct()
        .CountAsync();

        var pendingBookings = await db.Ptbookings
            .CountAsync(x => x.Status == "Pending");
        var brokenEquipment = await db.Equipments
            .CountAsync(x => x.Status == "Broken");

        var revenueByPayment = invoices
            .GroupBy(x => x.PaymentMethod)
            .Select(x => new DashboardMetric(x.Key, x.Sum(y => y.FinalAmount)))
            .OrderByDescending(x => x.Value)
            .ToList();
        var packageSales = details
            .Where(x => x.ItemType == "Package")
            .GroupBy(x => x.ItemName)
            .Select(x => new DashboardMetric(x.Key, x.Sum(y => y.Quantity)))
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToList();
        var productSales = details
            .Where(x => x.ItemType == "Product")
            .GroupBy(x => x.ItemName)
            .Select(x => new DashboardMetric(x.Key, x.Sum(y => y.Quantity)))
            .OrderByDescending(x => x.Value)
            .ThenBy(x => x.Label)
            .Take(5)
            .ToList();
            
        var averageMinutes = checkIns.Count == 0 ? 0 : checkIns
            .Average(x => (x.CheckOutTime!.Value - x.CheckInTime!.Value).TotalMinutes);
        return new(
            totalMembers,
            activeMembers,
            pendingBookings,
            brokenEquipment,
            invoices.Sum(x => x.FinalAmount),
            revenueByPayment,
            packageSales,
            productSales,
            averageMinutes);
    }
}
