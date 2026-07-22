using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record PosItem(string ItemType, int ItemId, string ItemName, decimal UnitPrice, int Quantity = 1);

public class PosService
{
    public async Task<(List<Product> Products, List<PackageTemplate> Packages, List<Member> Members, List<Ptbooking> ExtraBookings)> GetCatalogAsync()
    {
        using var db = new GymManagementDbContext();
        return (
            await db.Products.AsNoTracking().OrderBy(x => x.ProductName).ToListAsync(),
            await db.PackageTemplates.AsNoTracking().OrderBy(x => x.PackageName).ToListAsync(),
            await db.Members.AsNoTracking().OrderBy(x => x.FullName).ToListAsync(),
            await db.Ptbookings.AsNoTracking().Include(x => x.Member).Include(x => x.Pt)
                .Where(x => x.BookingType == "Extra" && x.PaymentStatus == "Pending" && x.Status == "Pending")
                .OrderBy(x => x.StartTime).ToListAsync());
    }

    public async Task<(Invoice? Invoice, string? Error)> CheckoutAsync(int operatorUserId, int? memberId,
        IReadOnlyCollection<PosItem> items, int discountPercent, string paymentMethod)
    {
        if (items.Count == 0) return (null, "Giỏ hàng đang trống.");
        if (items.Count(x => x.ItemType == "Package") > 1)
            return (null, "Mỗi hóa đơn chỉ được mua một gói tập cho hội viên.");
        if (discountPercent is < 0 or > 100) return (null, "Mức giảm giá phải từ 0 đến 100%.");
        if (paymentMethod is not ("Cash" or "Card" or "Transfer" or "MoMo")) return (null, "Phương thức thanh toán không hợp lệ.");
        if (items.Any(x => x.ItemType == "Package") && memberId == null)
            return (null, "Vui lòng chọn hội viên khi mua gói tập.");

        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var invoice = new Invoice
        {
            InvoiceCode = $"INV-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            UserId = operatorUserId,
            MemberId = memberId,
            CreatedDate = DateTime.Now,
            DiscountPercent = discountPercent,
            PaymentMethod = paymentMethod
        };

        decimal total = 0;
        var packageIdsInCheckout = new HashSet<int>();
        foreach (var item in items)
        {
            if (item.Quantity <= 0) return (null, "Số lượng hàng hóa không hợp lệ.");
            var currentUnitPrice = item.UnitPrice;

            if (item.ItemType == "Product")
            {
                var product = await db.Products.FindAsync(item.ItemId);
                if (product == null) return (null, "Không tìm thấy sản phẩm trong giỏ hàng.");
                if ((product.StockQuantity ?? 0) < item.Quantity) return (null, $"Sản phẩm '{product.ProductName}' không đủ tồn kho.");
                product.StockQuantity -= item.Quantity;
                currentUnitPrice = product.Price;
            }
            else if (item.ItemType == "Package")
            {
                var package = await db.PackageTemplates.FindAsync(item.ItemId);
                if (package == null || memberId == null) return (null, "Không tìm thấy gói tập.");
                currentUnitPrice = package.Price;
                if (!packageIdsInCheckout.Add(package.Id))
                    return (null, "Không thể mua trùng một gói tập trong cùng hóa đơn.");
                var today = DateOnly.FromDateTime(DateTime.Today);
                if (await db.MemberPackages.AnyAsync(x => x.MemberId == memberId && x.Status == "Active"
                    && x.StartDate <= today && x.EndDate >= today))
                    return (null, "Hội viên đang có gói tập còn hạn, chưa thể mua thêm gói mới.");
                var start = DateOnly.FromDateTime(DateTime.Today);
                db.MemberPackages.Add(new MemberPackage
                {
                    MemberId = memberId,
                    PackageTemplateId = package.Id,
                    StartDate = start,
                    EndDate = start.AddMonths(package.DurationMonths),
                    RemainingPtsessions = package.HasPt == true ? package.PtSessions ?? 0 : 0,
                    Status = "Active"
                });
            }
            else if (item.ItemType == "PTBooking")
            {
                var booking = await db.Ptbookings.FindAsync(item.ItemId);
                if (booking == null || booking.BookingType != "Extra" || booking.PaymentStatus != "Pending")
                    return (null, "Booking PT mua thêm không còn chờ thanh toán.");
                if (memberId == null || booking.MemberId != memberId)
                    return (null, "Booking PT không thuộc hội viên đã chọn.");
                booking.PaymentStatus = "Paid";
                currentUnitPrice = booking.Price;
            }
            else return (null, "Loại hàng hóa không hợp lệ.");

            total += currentUnitPrice * item.Quantity;
            invoice.InvoiceDetails.Add(new InvoiceDetail
            {
                ItemType = item.ItemType,
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = currentUnitPrice,
                TotalPrice = currentUnitPrice * item.Quantity
            });
        }

        invoice.TotalAmount = total;
        invoice.FinalAmount = Math.Round(total * (1 - discountPercent / 100m), 2);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (invoice, null);
    }
}
