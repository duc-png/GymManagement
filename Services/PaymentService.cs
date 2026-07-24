using System.Data;
using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public static class PaymentStatuses
{
    public const string Paid = "Paid";
    public const string PendingCash = "PendingCash";
    public const string PendingTransfer = "PendingTransfer";
    public const string Rejected = "Rejected";

    public static bool IsPending(string? status)
        => status is PendingCash or PendingTransfer;
}

public sealed class PaymentService
{
    public async Task<(Invoice? Invoice, string? Error)> CreatePendingAsync(
        int memberUserId,
        int memberId,
        IReadOnlyCollection<PosItem> items,
        string paymentMethod)
    {
        if (items.Count == 0)
            return (null, "Giỏ hàng đang trống.");
        if (paymentMethod is not ("Cash" or "Transfer"))
            return (null, "Phương thức này không hỗ trợ yêu cầu xác nhận.");
        if (items.Count(x => x.ItemType == "Package") > 1)
            return (null, "Mỗi yêu cầu chỉ được mua một gói tập.");

        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var memberBelongsToUser = await db.Members.AnyAsync(x =>
            x.Id == memberId && x.UserId == memberUserId);
        if (!memberBelongsToUser)
            return (null, "Không tìm thấy hồ sơ hội viên của tài khoản.");

        var invoice = new Invoice
        {
            InvoiceCode = BuildCode("REQ"),
            UserId = memberUserId,
            MemberId = memberId,
            CreatedDate = DateTime.Now,
            DiscountPercent = 0,
            PaymentMethod = paymentMethod,
            PaymentStatus = paymentMethod == "Cash"
                ? PaymentStatuses.PendingCash
                : PaymentStatuses.PendingTransfer
        };

        decimal total = 0;
        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return (null, "Số lượng hàng hóa không hợp lệ.");

            var validation = await GetCurrentItemAsync(db, memberId, item);
            if (validation.Error != null)
                return (null, validation.Error);

            var unitPrice = validation.UnitPrice;
            total += unitPrice * item.Quantity;
            invoice.InvoiceDetails.Add(new InvoiceDetail
            {
                ItemType = item.ItemType,
                ItemId = item.ItemId,
                ItemName = validation.ItemName,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                TotalPrice = unitPrice * item.Quantity
            });
        }

        invoice.TotalAmount = total;
        invoice.FinalAmount = total;
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (invoice, null);
    }

    public async Task<List<Invoice>> GetPendingAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Invoices.AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.InvoiceDetails)
            .Where(x => x.PaymentStatus == PaymentStatuses.PendingCash
                || x.PaymentStatus == PaymentStatuses.PendingTransfer)
            .OrderBy(x => x.CreatedDate)
            .ToListAsync();
    }

    public async Task<string?> ConfirmAsync(int invoiceId, int confirmerUserId)
    {
        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (!await CanConfirmAsync(db, confirmerUserId))
            return "Chỉ Receptionist hoặc Admin được xác nhận thanh toán.";

        var invoice = await db.Invoices
            .Include(x => x.InvoiceDetails)
            .SingleOrDefaultAsync(x => x.Id == invoiceId);
        if (invoice == null)
            return "Không tìm thấy yêu cầu thanh toán.";
        if (!PaymentStatuses.IsPending(invoice.PaymentStatus))
            return "Yêu cầu này không còn chờ xác nhận.";
        if (invoice.MemberId == null)
            return "Yêu cầu thanh toán không có thông tin hội viên.";

        foreach (var detail in invoice.InvoiceDetails)
        {
            var error = await ApplyItemAsync(db, invoice.MemberId.Value, detail);
            if (error != null)
                return error;
        }

        invoice.InvoiceCode = BuildCode("INV");
        invoice.PaymentStatus = PaymentStatuses.Paid;
        invoice.ConfirmedByUserId = confirmerUserId;
        invoice.ConfirmedDate = DateTime.Now;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<string?> RejectAsync(int invoiceId, int confirmerUserId)
    {
        using var db = new GymManagementDbContext();
        if (!await CanConfirmAsync(db, confirmerUserId))
            return "Chỉ Receptionist hoặc Admin được từ chối thanh toán.";

        var invoice = await db.Invoices.SingleOrDefaultAsync(x => x.Id == invoiceId);
        if (invoice == null)
            return "Không tìm thấy yêu cầu thanh toán.";
        if (!PaymentStatuses.IsPending(invoice.PaymentStatus))
            return "Yêu cầu này không còn chờ xác nhận.";

        invoice.PaymentStatus = PaymentStatuses.Rejected;
        invoice.ConfirmedByUserId = confirmerUserId;
        invoice.ConfirmedDate = DateTime.Now;
        await db.SaveChangesAsync();
        return null;
    }

    private static async Task<(string ItemName, decimal UnitPrice, string? Error)> GetCurrentItemAsync(
        GymManagementDbContext db,
        int memberId,
        PosItem item)
    {
        if (item.ItemType == "Product")
        {
            var product = await db.Products.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.ItemId);
            if (product == null)
                return (string.Empty, 0, "Không tìm thấy sản phẩm trong giỏ hàng.");
            if ((product.StockQuantity ?? 0) < item.Quantity)
                return (string.Empty, 0, $"Sản phẩm '{product.ProductName}' không đủ tồn kho.");
            return (product.ProductName, product.Price, null);
        }

        if (item.ItemType == "Package")
        {
            var package = await db.PackageTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.ItemId);
            if (package == null)
                return (string.Empty, 0, "Không tìm thấy gói tập.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await db.MemberPackages.AnyAsync(x => x.MemberId == memberId
                && x.Status == "Active" && x.StartDate <= today && x.EndDate >= today))
                return (string.Empty, 0, "Hội viên đang có gói tập còn hạn.");

            if (await HasPendingItemAsync(db, memberId, "Package", item.ItemId))
                return (string.Empty, 0, "Gói tập này đã có yêu cầu thanh toán chờ xác nhận.");

            return (package.PackageName, package.Price, null);
        }

        if (item.ItemType == "PTBooking")
        {
            var booking = await db.Ptbookings.AsNoTracking()
                .Include(x => x.Pt)
                .SingleOrDefaultAsync(x => x.Id == item.ItemId);
            if (booking == null || booking.MemberId != memberId
                || booking.BookingType != "Extra" || booking.PaymentStatus != "Pending")
                return (string.Empty, 0, "Booking PT không còn chờ thanh toán.");

            if (await HasPendingItemAsync(db, memberId, "PTBooking", item.ItemId))
                return (string.Empty, 0, "Booking PT này đã có yêu cầu thanh toán chờ xác nhận.");

            return ($"PT {booking.Pt?.FullName} - {booking.StartTime:g}", booking.Price, null);
        }

        return (string.Empty, 0, "Loại hàng hóa không hợp lệ.");
    }

    private static async Task<string?> ApplyItemAsync(
        GymManagementDbContext db,
        int memberId,
        InvoiceDetail detail)
    {
        if (detail.ItemType == "Product")
        {
            var product = await db.Products.SingleOrDefaultAsync(x => x.Id == detail.ItemId);
            if (product == null)
                return $"Không tìm thấy sản phẩm '{detail.ItemName}'.";
            if ((product.StockQuantity ?? 0) < detail.Quantity)
                return $"Sản phẩm '{product.ProductName}' không đủ tồn kho để xác nhận.";

            product.StockQuantity -= detail.Quantity;
            return null;
        }

        if (detail.ItemType == "Package")
        {
            var package = await db.PackageTemplates.SingleOrDefaultAsync(x => x.Id == detail.ItemId);
            if (package == null)
                return $"Không tìm thấy gói tập '{detail.ItemName}'.";

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (await db.MemberPackages.AnyAsync(x => x.MemberId == memberId
                && x.Status == "Active" && x.StartDate <= today && x.EndDate >= today))
                return "Hội viên đang có gói tập còn hạn, không thể xác nhận gói mới.";

            db.MemberPackages.Add(new MemberPackage
            {
                MemberId = memberId,
                PackageTemplateId = package.Id,
                StartDate = today,
                EndDate = today.AddMonths(package.DurationMonths),
                RemainingPtsessions = package.HasPt == true ? package.PtSessions ?? 0 : 0,
                Status = "Active"
            });
            return null;
        }

        if (detail.ItemType == "PTBooking")
        {
            var booking = await db.Ptbookings.SingleOrDefaultAsync(x => x.Id == detail.ItemId);
            if (booking == null || booking.MemberId != memberId
                || booking.BookingType != "Extra" || booking.PaymentStatus != "Pending")
                return $"Booking '{detail.ItemName}' không còn chờ thanh toán.";

            booking.PaymentStatus = "Paid";
            return null;
        }

        return $"Loại hàng hóa '{detail.ItemType}' không hợp lệ.";
    }

    private static Task<bool> HasPendingItemAsync(
        GymManagementDbContext db,
        int memberId,
        string itemType,
        int itemId)
        => db.InvoiceDetails.AnyAsync(x =>
            x.ItemType == itemType
            && x.ItemId == itemId
            && x.Invoice != null
            && x.Invoice.MemberId == memberId
            && (x.Invoice.PaymentStatus == PaymentStatuses.PendingCash
                || x.Invoice.PaymentStatus == PaymentStatuses.PendingTransfer));

    private static Task<bool> CanConfirmAsync(GymManagementDbContext db, int userId)
        => db.Users.AnyAsync(x => x.Id == userId
            && (x.Role == UserRoles.Receptionist || x.Role == UserRoles.Admin));

    private static string BuildCode(string prefix)
        => $"{prefix}-{DateTime.Now:yyyyMMddHHmmssfff}-{Random.Shared.Next(10, 99)}";
}
