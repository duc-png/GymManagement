using GymManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var endTime = startTime.AddMinutes(durationMinutes);
        int? memberPackageId = null;
        decimal price = 0;
        var paymentStatus = "Included";

        if (bookingType == "Package")
        {
            var package = await db.MemberPackages
                .Include(x => x.PackageTemplate)
                .Where(x => x.MemberId == memberId && x.Status == "Active"
                    && x.StartDate <= DateOnly.FromDateTime(startTime)
                    && x.EndDate >= DateOnly.FromDateTime(startTime)
                    && (x.RemainingPtsessions ?? 0) > 0)
                .OrderBy(x => x.EndDate)
                .FirstOrDefaultAsync();
            if (package == null) return "Hội viên không có gói tập còn buổi PT trong ngày này.";
            var configuredMinutes = package.PackageTemplate?.PtminutesPerSession ?? 0;
            if (configuredMinutes > 0 && durationMinutes != configuredMinutes)
                return $"Gói tập yêu cầu mỗi buổi PT dài {configuredMinutes} phút.";
            memberPackageId = package.Id;
            package.RemainingPtsessions--;
            paymentStatus = "Pending";
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
        await transaction.CommitAsync();
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
        if (newStatus == "Completed" && DateTime.Now < booking.EndTime)
            return $"Chỉ có thể hoàn thành buổi tập sau {booking.EndTime:HH:mm 'ngày' dd/MM/yyyy}.";
        if (newStatus == "Completed" && booking.BookingType == "Extra" && booking.PaymentStatus != "Paid")
            return "Booking mua thêm chưa được thanh toán.";

        if (booking.BookingType == "Package")
        {
            var package = await db.MemberPackages
                .Include(x => x.PackageTemplate)
                .SingleOrDefaultAsync(x => x.Id == booking.MemberPackageId);
            if (package == null) return "Không tìm thấy gói tập đã dùng để đặt lịch.";

            if (newStatus == "Cancelled" && booking.PaymentStatus == "Pending")
            {
                var maximumSessions = package.PackageTemplate?.PtSessions;
                var restoredSessions = (package.RemainingPtsessions ?? 0) + 1;
                package.RemainingPtsessions = maximumSessions.HasValue
                    ? Math.Min(restoredSessions, maximumSessions.Value)
                    : restoredSessions;
            }
            else if (newStatus == "Completed" && booking.PaymentStatus == "Included")
            {
                if ((package.RemainingPtsessions ?? 0) <= 0)
                    return "Gói tập không còn buổi PT để hoàn thành booking cũ.";
                package.RemainingPtsessions--;
            }

            if (newStatus == "Completed" && booking.PaymentStatus == "Pending")
                booking.PaymentStatus = "Included";
        }

        booking.Status = newStatus;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<string?> RescheduleAsync(
        int currentUserId,
        string role,
        int bookingId,
        DateTime newStartTime)
    {
        if (newStartTime <= DateTime.Now)
            return "Thời gian mới phải ở trong tương lai.";

        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var booking = await db.Ptbookings
            .Include(x => x.Member)
            .Include(x => x.Pt)
            .SingleOrDefaultAsync(x => x.Id == bookingId);

        if (booking == null)
            return "Không tìm thấy lịch đặt.";
        if (booking.Status != "Pending")
            return "Chỉ có thể chuyển lịch đang chờ thực hiện.";
        if (booking.StartTime == newStartTime)
            return "Vui lòng chọn thời gian khác lịch hiện tại.";

        var isMember = string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase);
        var canManage = string.Equals(role, UserRoles.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, UserRoles.Receptionist, StringComparison.OrdinalIgnoreCase);
        if (isMember && booking.Member?.UserId != currentUserId)
            return "Lịch đặt này không thuộc tài khoản của bạn.";
        if (!isMember && !canManage)
            return "Bạn không có quyền chuyển lịch đặt này.";

        var duration = booking.EndTime - booking.StartTime;
        var newEndTime = newStartTime.Add(duration);

        if (booking.BookingType == "Package")
        {
            var package = await db.MemberPackages
                .SingleOrDefaultAsync(x => x.Id == booking.MemberPackageId);
            var newDate = DateOnly.FromDateTime(newStartTime);
            if (package == null || newDate < package.StartDate || newDate > package.EndDate)
                return "Thời gian mới phải nằm trong thời hạn của gói tập.";
        }

        var overlaps = await db.Ptbookings.AnyAsync(x =>
            x.Id != booking.Id
            && x.Ptid == booking.Ptid
            && x.Status != "Cancelled"
            && newStartTime < x.EndTime
            && newEndTime > x.StartTime);
        if (overlaps)
            return "PT đã có lịch trong khoảng thời gian mới.";

        booking.StartTime = newStartTime;
        booking.EndTime = newEndTime;

        var cartItem = await db.CartItems.SingleOrDefaultAsync(x =>
            x.ItemType == "PTBooking" && x.ItemId == booking.Id);
        if (cartItem != null)
            cartItem.ItemName = $"PT {booking.Pt?.FullName} - {newStartTime:g}";

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<List<string>> GetAvailableSlotsAsync(int bookingId, DateTime date)
    {
        using var db = new GymManagementDbContext();
        var booking = await db.Ptbookings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == bookingId);
        if (booking == null || booking.Ptid == null)
            return new List<string>();

        var duration = booking.EndTime - booking.StartTime;
        var dayStart = date.Date.AddHours(6);
        var dayEnd = date.Date.AddHours(22);
        var occupied = await db.Ptbookings.AsNoTracking()
            .Where(x => x.Id != booking.Id
                && x.Ptid == booking.Ptid
                && x.Status != "Cancelled"
                && x.StartTime < dayEnd
                && x.EndTime > dayStart)
            .Select(x => new { x.StartTime, x.EndTime })
            .ToListAsync();

        var slots = new List<string>();
        for (var candidate = dayStart; candidate.Add(duration) <= dayEnd; candidate = candidate.AddMinutes(30))
        {
            var candidateEnd = candidate.Add(duration);
            var isCurrentSlot = candidate == booking.StartTime;
            var overlaps = occupied.Any(x => candidate < x.EndTime && candidateEnd > x.StartTime);
            if (candidate > DateTime.Now && !isCurrentSlot && !overlaps)
                slots.Add(candidate.ToString("HH:mm"));
        }

        return slots;
    }

    public async Task<List<string>> GetAvailableSlotsAsync(
        int ptId,
        DateTime date,
        int durationMinutes)
    {
        if (durationMinutes <= 0)
            return new List<string>();

        using var db = new GymManagementDbContext();
        var dayStart = date.Date.AddHours(6);
        var dayEnd = date.Date.AddHours(22);
        var duration = TimeSpan.FromMinutes(durationMinutes);
        var occupied = await db.Ptbookings.AsNoTracking()
            .Where(x => x.Ptid == ptId
                && x.Status != "Cancelled"
                && x.StartTime < dayEnd
                && x.EndTime > dayStart)
            .Select(x => new { x.StartTime, x.EndTime })
            .ToListAsync();

        var slots = new List<string>();
        for (var candidate = dayStart; candidate.Add(duration) <= dayEnd; candidate = candidate.AddMinutes(30))
        {
            var candidateEnd = candidate.Add(duration);
            var overlaps = occupied.Any(x => candidate < x.EndTime && candidateEnd > x.StartTime);
            if (candidate > DateTime.Now && !overlaps)
                slots.Add(candidate.ToString("HH:mm"));
        }

        return slots;
    }

    public async Task<List<Ptbooking>> GetPtScheduleAsync(int ptId, DateTime displayedMonth)
    {
        var monthStart = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        var daysFromMonday = ((int)monthStart.DayOfWeek + 6) % 7;
        var calendarStart = monthStart.AddDays(-daysFromMonday);
        var calendarEnd = calendarStart.AddDays(42);

        using var db = new GymManagementDbContext();
        return await db.Ptbookings.AsNoTracking()
            .Where(x => x.Ptid == ptId
                && x.Status != "Cancelled"
                && x.StartTime < calendarEnd
                && x.EndTime > calendarStart)
            .Select(x => new Ptbooking
            {
                Id = x.Id,
                Ptid = x.Ptid,
                StartTime = x.StartTime,
                EndTime = x.EndTime,
                Status = x.Status,
                BookingType = x.BookingType,
                PaymentStatus = x.PaymentStatus
            })
            .OrderBy(x => x.StartTime)
            .ToListAsync();
    }
}
