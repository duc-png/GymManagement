using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record BookingData(List<Member> Members, List<User> Pts, List<Ptbooking> Bookings);

public class BookingService
{
    public async Task<BookingData> GetDataAsync(int currentUserId, string role)
    {
        using var db = new GymManagementDbContext();
        var membersQuery = db.Members.AsNoTracking();
        if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
            membersQuery = membersQuery.Where(x => x.UserId == currentUserId);
        var members = await membersQuery.OrderBy(x => x.FullName).ToListAsync();
        var pts = await db.Users.AsNoTracking().Where(x => x.Role == UserRoles.Pt).OrderBy(x => x.FullName).ToListAsync();
        var query = db.Ptbookings.AsNoTracking().Include(x => x.Member).Include(x => x.Pt).AsQueryable();
        if (string.Equals(role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Ptid == currentUserId);
        else if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
            query = query.Where(x => x.Member != null && x.Member.UserId == currentUserId);
        var bookings = await query.OrderByDescending(x => x.StartTime).ToListAsync();
        return new(members, pts, bookings);
    }

    public async Task<string?> CreateAsync(int currentUserId, string role, int memberId, int ptId, DateTime startTime, DateTime endTime)
    {
        if (endTime <= startTime) return "End time must be after start time.";
        if (startTime < DateTime.Now) return "Booking time cannot be in the past.";

        using var db = new GymManagementDbContext();
        if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            var ownMemberId = await db.Members.Where(x => x.UserId == currentUserId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
            if (ownMemberId == null) return "Member profile was not found.";
            memberId = ownMemberId.Value;
        }
        else if (string.Equals(role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
        {
            return "PT accounts cannot create bookings.";
        }
        var bookingDate = DateOnly.FromDateTime(startTime);
        var package = await db.MemberPackages.FirstOrDefaultAsync(x =>
            x.MemberId == memberId && x.Status == "Active" && x.StartDate <= bookingDate && x.EndDate >= bookingDate);
        if (package == null) return "Member does not have an active package for this date.";
        if ((package.RemainingPtsessions ?? 0) <= 0) return "Member has no remaining PT sessions.";

        var overlaps = await db.Ptbookings.AnyAsync(x => x.Ptid == ptId && x.Status != "Cancelled"
            && startTime < x.EndTime && endTime > x.StartTime);
        if (overlaps) return "PT already has a booking in this time range.";

        db.Ptbookings.Add(new Ptbooking
        {
            MemberId = memberId,
            Ptid = ptId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "Pending"
        });
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UpdateStatusAsync(int bookingId, string newStatus)
    {
        if (newStatus is not ("Completed" or "Cancelled")) return "Invalid booking status.";

        using var db = new GymManagementDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var booking = await db.Ptbookings.FindAsync(bookingId);
        if (booking == null) return "Booking was not found.";
        if (booking.Status != "Pending") return "Only pending bookings can be updated.";

        if (newStatus == "Completed")
        {
            var bookingDate = DateOnly.FromDateTime(booking.StartTime);
            var package = await db.MemberPackages.FirstOrDefaultAsync(x => x.MemberId == booking.MemberId
                && x.Status == "Active" && x.StartDate <= bookingDate && x.EndDate >= bookingDate
                && (x.RemainingPtsessions ?? 0) > 0);
            if (package == null) return "No active package with remaining PT sessions was found.";
            package.RemainingPtsessions--;
        }

        booking.Status = newStatus;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }
}
