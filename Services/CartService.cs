using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class CartService
{
    private readonly PosService _posService = new();
    private readonly PaymentService _paymentService = new();

    public async Task<List<CartItem>> GetAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        return await db.CartItems.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedDate).ToListAsync();
    }

    public async Task<List<Ptbooking>> GetMyPendingExtraBookingsAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        var bookingsAwaitingConfirmation = db.InvoiceDetails
            .Where(x => x.ItemType == "PTBooking"
                && x.Invoice != null
                && x.Invoice.Member != null
                && x.Invoice.Member.UserId == userId
                && (x.Invoice.PaymentStatus == PaymentStatuses.PendingCash
                    || x.Invoice.PaymentStatus == PaymentStatuses.PendingTransfer))
            .Select(x => x.ItemId);

        return await db.Ptbookings.AsNoTracking().Include(x => x.Member).Include(x => x.Pt)
            .Where(x => x.Member != null && x.Member.UserId == userId
                && x.BookingType == "Extra" && x.PaymentStatus == "Pending" && x.Status == "Pending"
                && !bookingsAwaitingConfirmation.Contains(x.Id))
            .OrderBy(x => x.StartTime).ToListAsync();
    }

    public async Task<string?> AddProductAsync(int userId, int productId, int quantity)
    {
        if (quantity <= 0) return "Số lượng sản phẩm phải lớn hơn 0.";

        using var db = new GymManagementDbContext();
        if (!await db.Members.AnyAsync(x => x.UserId == userId))
            return "Chỉ tài khoản hội viên được mua sản phẩm qua giỏ hàng.";

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId);
        if (product == null) return "Không tìm thấy sản phẩm.";
        if ((product.StockQuantity ?? 0) <= 0) return "Sản phẩm đã hết hàng.";

        var existingItem = await db.CartItems.SingleOrDefaultAsync(x =>
            x.UserId == userId && x.ItemType == "Product" && x.ItemId == productId);
        var newQuantity = (existingItem?.Quantity ?? 0) + quantity;
        if (newQuantity > product.StockQuantity)
            return $"Sản phẩm chỉ còn {product.StockQuantity} trong kho.";

        if (existingItem == null)
        {
            db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ItemType = "Product",
                ItemId = product.Id,
                ItemName = product.ProductName,
                Quantity = quantity,
                UnitPrice = product.Price,
                CreatedDate = DateTime.Now
            });
        }
        else
        {
            existingItem.Quantity = newQuantity;
            existingItem.ItemName = product.ProductName;
            existingItem.UnitPrice = product.Price;
        }

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> AddBookingAsync(int userId, int bookingId)
    {
        using var db = new GymManagementDbContext();
        var booking = await db.Ptbookings.Include(x => x.Member).Include(x => x.Pt).SingleOrDefaultAsync(x => x.Id == bookingId);
        if (booking == null || booking.BookingType != "Extra" || booking.PaymentStatus != "Pending")
            return "Không tìm thấy booking PT mua thêm đang chờ thanh toán.";
        if (booking.Member?.UserId != userId)
            return "Booking PT không thuộc tài khoản của bạn.";
        if (await db.CartItems.AnyAsync(x => x.UserId == userId && x.ItemType == "PTBooking" && x.ItemId == bookingId))
            return "Booking PT này đã có trong giỏ hàng.";
        db.CartItems.Add(new CartItem
        {
            UserId = userId,
            ItemType = "PTBooking",
            ItemId = booking.Id,
            ItemName = $"PT {booking.Pt?.FullName} - {booking.StartTime:g}",
            Quantity = 1,
            UnitPrice = booking.Price,
            CreatedDate = DateTime.Now
        });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> AddPackageAsync(int userId, int packageId)
    {
        using var db = new GymManagementDbContext();
        var package = await db.PackageTemplates.FindAsync(packageId);
        if (package == null) return "Không tìm thấy gói tập.";
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return "Không tìm thấy hồ sơ hội viên.";
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (await db.MemberPackages.AnyAsync(x => x.MemberId == memberId && x.Status == "Active"
            && x.StartDate <= today && x.EndDate >= today))
            return "Bạn đang có gói tập còn hạn, chưa thể mua thêm gói mới.";
        if (await db.CartItems.AnyAsync(x => x.UserId == userId && x.ItemType == "Package" && x.ItemId == packageId))
            return "Gói tập này đã có trong giỏ hàng.";
        db.CartItems.Add(new CartItem { UserId = userId, ItemType = "Package", ItemId = package.Id, ItemName = package.PackageName, Quantity = 1, UnitPrice = package.Price, CreatedDate = DateTime.Now });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> RemoveAsync(int userId, int cartItemId)
    {
        using var db = new GymManagementDbContext();
        var item = await db.CartItems.SingleOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId);
        if (item == null) return "Không tìm thấy sản phẩm trong giỏ hàng.";
        db.CartItems.Remove(item);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<(Invoice? Invoice, string? Error)> CheckoutAsync(int userId, string paymentMethod)
    {
        using var db = new GymManagementDbContext();
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return (null, "Không tìm thấy hồ sơ hội viên.");
        var items = await db.CartItems.Where(x => x.UserId == userId).ToListAsync();
        if (items.Count == 0) return (null, "Giỏ hàng đang trống.");
        var posItems = items.Select(x => new PosItem(x.ItemType, x.ItemId, x.ItemName, x.UnitPrice, x.Quantity)).ToList();
        var result = paymentMethod is "Cash" or "Transfer"
            ? await _paymentService.CreatePendingAsync(userId, memberId.Value, posItems, paymentMethod)
            : await _posService.CheckoutAsync(userId, memberId, posItems, 0, paymentMethod);
        if (result.Error == null)
        {
            db.CartItems.RemoveRange(items);
            await db.SaveChangesAsync();
        }
        return result;
    }
}
