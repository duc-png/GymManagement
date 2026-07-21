using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record BookingData(List<Member> Members, List<User> Pts, List<Ptbooking> Bookings);

public class BookingService
{
    public async Task<BookingData> GetDataAsync(User currentUser)
    {
        using var db = new GymManagementDbContext();
        var role = currentUser.Role.Trim();

        if (string.Equals(role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
        {
            var ownTeachingSchedule = await BookingQuery(db)
                .Where(x => x.Ptid == currentUser.Id)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
            return new(new List<Member>(), new List<User>(), ownTeachingSchedule);
        }

        if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            var ownMemberProfiles = await db.Members
                .AsNoTracking()
                .Where(x => x.UserId == currentUser.Id)
                .OrderBy(x => x.FullName)
                .ToListAsync();
            var availablePts = await db.Users
                .AsNoTracking()
                .Where(x => x.Role == UserRoles.Pt)
                .OrderBy(x => x.FullName)
                .ToListAsync();
            var ownBookedSchedule = await BookingQuery(db)
                .Where(x => x.Member != null && x.Member.UserId == currentUser.Id)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
            return new(ownMemberProfiles, availablePts, ownBookedSchedule);
        }

        if (string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, UserRoles.Receptionist, StringComparison.OrdinalIgnoreCase))
        {
            var members = await db.Members.AsNoTracking().OrderBy(x => x.FullName).ToListAsync();
            var pts = await db.Users.AsNoTracking()
                .Where(x => x.Role == UserRoles.Pt)
                .OrderBy(x => x.FullName)
                .ToListAsync();
            var allBookings = await BookingQuery(db)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
            return new(members, pts, allBookings);
        }

        return new(new List<Member>(), new List<User>(), new List<Ptbooking>());
    }

    private static IQueryable<Ptbooking> BookingQuery(GymManagementDbContext db)
        => db.Ptbookings
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.Pt);

    public async Task<string?> CreateAsync(int currentUserId, string role, int memberId, int ptId,
        string bookingType, int durationMinutes, DateTime startTime)
    {
        if (durationMinutes <= 0) return "Thời lượng buổi PT không hợp lệ.";
        if (startTime < DateTime.Now) return "Không thể đặt lịch trong quá khứ.";

        using var db = new GymManagementDbContext();
        if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            var ownMemberId = await db.Members.Where(x => x.UserId == currentUserId)
                .Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (ownMemberId == null) return "Không tìm thấy hồ sơ hội viên.";
            memberId = ownMemberId.Value;
        }
        else if (string.Equals(role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return "Tài khoản PT không thể tự tạo lịch đặt.";
        }

        var endTime = startTime.AddMinutes(durationMinutes);
        int? memberPackageId = null;
        decimal price = 0;
        var paymentStatus = "Included";

        if (bookingType == "Package")
        {
            var package = await db.MemberPackages.Include(x => x.PackageTemplate).FirstOrDefaultAsync(x =>
                x.MemberId == memberId && x.Status == "Active"
                && x.StartDate <= DateOnly.FromDateTime(startTime)
                && x.EndDate >= DateOnly.FromDateTime(startTime)
                && (x.RemainingPtsessions ?? 0) > 0);
            if (package == null) return "Hội viên không có gói tập còn buổi PT trong ngày này.";
            var configuredMinutes = package.PackageTemplate?.PtminutesPerSession ?? 0;
            if (configuredMinutes > 0 && durationMinutes != configuredMinutes)
                return $"Gói tập yêu cầu mỗi buổi PT dài {configuredMinutes} phút.";
            memberPackageId = package.Id;
        }
        else if (bookingType == "Extra")
        {
            var pt = await db.Users.SingleOrDefaultAsync(x => x.Id == ptId && x.Role == UserRoles.Pt);
            if (pt?.PthourlyRate == null || pt.PthourlyRate <= 0)
                return "PT chưa được cấu hình giá thuê theo giờ.";
            price = Math.Round(pt.PthourlyRate.Value * durationMinutes / 60m, 2);
            paymentStatus = "Pending";
        }
        else
        {
            return "Loại booking không hợp lệ.";
        }

        var overlaps = await db.Ptbookings.AnyAsync(x => x.Ptid == ptId && x.Status != "Cancelled"
            && startTime < x.EndTime && endTime > x.StartTime);
        if (overlaps) return "PT đã có lịch trong khoảng thời gian này.";

        db.Ptbookings.Add(new Ptbooking
        {
            MemberId = memberId,
            Ptid = ptId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "Pending",
            BookingType = bookingType,
            Price = price,
            PaymentStatus = paymentStatus,
            MemberPackageId = memberPackageId
        });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UpdateStatusAsync(int bookingId, string newStatus)
    {
        if (newStatus is not ("Completed" or "Cancelled")) return "Trạng thái lịch không hợp lệ.";

        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var booking = await db.Ptbookings.FindAsync(bookingId);
        if (booking == null) return "Không tìm thấy lịch đặt.";
        if (booking.Status != "Pending") return "Chỉ có thể cập nhật lịch đang chờ xử lý.";
        if (newStatus == "Completed" && booking.PaymentStatus != "Included" && booking.PaymentStatus != "Paid")
            return "Booking mua thêm chưa được thanh toán.";

        if (newStatus == "Completed" && booking.BookingType == "Package")
        {
            var package = await db.MemberPackages.FindAsync(booking.MemberPackageId);
            if (package == null || (package.RemainingPtsessions ?? 0) <= 0)
                return "Không tìm thấy gói hoạt động còn buổi PT.";
            package.RemainingPtsessions--;
        }

        booking.Status = newStatus;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }
}
