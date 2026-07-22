using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class FeedbackService
{
    public async Task<List<User>> GetEligiblePtsAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        var memberId = await db.Members
            .Where(x => x.UserId == userId)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync();
        if (memberId == null) return new List<User>();

        var reviewedPtIds = db.Feedbacks
            .Where(x => x.MemberId == memberId && x.FeedbackType == "PT" && x.TargetPtid != null)
            .Select(x => x.TargetPtid!.Value);

        var eligiblePtIds = await db.Ptbookings
            .AsNoTracking()
            .Where(x => x.MemberId == memberId && x.Ptid != null && x.Status == "Completed"
                && (x.BookingType == "Package" || x.PaymentStatus == "Paid" || x.PaymentStatus == "Included")
                && !reviewedPtIds.Contains(x.Ptid.Value))
            .Select(x => x.Ptid!.Value)
            .Distinct()
            .ToListAsync();

        return await db.Users
            .AsNoTracking()
            .Where(x => eligiblePtIds.Contains(x.Id) && x.Role == UserRoles.Pt)
            .OrderBy(x => x.FullName)
            .ToListAsync();
    }

    public async Task<string?> SubmitPtAsync(int userId, int ptId, int rating, string comment)
    {
        using var db = new GymManagementDbContext();
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.Role == UserRoles.Member))
            return "Chỉ hội viên được đánh giá PT.";
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return "Không tìm thấy hồ sơ hội viên.";
        if (rating is < 1 or > 5) return "Số sao phải từ 1 đến 5.";
        if (string.IsNullOrWhiteSpace(comment)) return "Vui lòng nhập nội dung đánh giá.";
        if (!await db.Ptbookings.AnyAsync(x => x.MemberId == memberId && x.Ptid == ptId
            && x.Status == "Completed"
            && (x.BookingType == "Package" || x.PaymentStatus == "Paid" || x.PaymentStatus == "Included")))
            return "Bạn chỉ có thể đánh giá PT sau khi đã thuê và hoàn thành buổi tập.";
        if (await db.Feedbacks.AnyAsync(x => x.MemberId == memberId && x.TargetPtid == ptId && x.FeedbackType == "PT")) return "Bạn đã đánh giá PT này rồi.";
        db.Feedbacks.Add(new Feedback { MemberId = memberId, FeedbackType = "PT", TargetPtid = ptId, RatingStars = rating, Comment = comment.Trim(), SubmittedDate = DateTime.Now });
        await db.SaveChangesAsync(); return null;
    }

    public async Task<string?> SubmitFacilityAsync(int userId, int rating, string comment)
    {
        using var db = new GymManagementDbContext();
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.Role == UserRoles.Member))
            return "Chỉ hội viên được đánh giá phòng tập.";
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return "Không tìm thấy hồ sơ hội viên.";
        if (!await db.MemberPackages.AnyAsync(x => x.MemberId == memberId))
            return "Bạn cần mua ít nhất một gói tập trước khi đánh giá phòng tập.";
        if (rating is < 1 or > 5) return "Số sao phải từ 1 đến 5.";
        if (string.IsNullOrWhiteSpace(comment)) return "Vui lòng nhập nội dung đánh giá.";
        db.Feedbacks.Add(new Feedback { MemberId = memberId, FeedbackType = "Facility", EquipmentId = null, RatingStars = rating, Comment = comment.Trim(), SubmittedDate = DateTime.Now });
        await db.SaveChangesAsync(); return null;
    }
}
