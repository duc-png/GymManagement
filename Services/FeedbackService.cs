using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class FeedbackService
{
    public async Task<List<User>> GetPtsAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Users.AsNoTracking().Where(x => x.Role == UserRoles.Pt).OrderBy(x => x.FullName).ToListAsync();
    }

    public async Task<string?> SubmitPtAsync(int userId, int ptId, int rating, string comment)
    {
        using var db = new GymManagementDbContext();
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return "Không tìm thấy hồ sơ hội viên.";
        if (rating is < 1 or > 5) return "Số sao phải từ 1 đến 5.";
        if (string.IsNullOrWhiteSpace(comment)) return "Vui lòng nhập nội dung đánh giá.";
        if (!await db.Ptbookings.AnyAsync(x => x.MemberId == memberId && x.Ptid == ptId && x.Status == "Completed")) return "Bạn chỉ có thể đánh giá PT sau khi hoàn thành buổi tập.";
        if (await db.Feedbacks.AnyAsync(x => x.MemberId == memberId && x.TargetPtid == ptId && x.FeedbackType == "PT")) return "Bạn đã đánh giá PT này rồi.";
        db.Feedbacks.Add(new Feedback { MemberId = memberId, FeedbackType = "PT", TargetPtid = ptId, RatingStars = rating, Comment = comment.Trim(), SubmittedDate = DateTime.Now });
        await db.SaveChangesAsync(); return null;
    }

    public async Task<string?> SubmitFacilityAsync(int userId, int rating, string comment)
    {
        using var db = new GymManagementDbContext();
        var memberId = await db.Members.Where(x => x.UserId == userId).Select(x => (int?)x.Id).SingleOrDefaultAsync();
        if (memberId == null) return "Không tìm thấy hồ sơ hội viên.";
        if (rating is < 1 or > 5) return "Số sao phải từ 1 đến 5.";
        if (string.IsNullOrWhiteSpace(comment)) return "Vui lòng nhập nội dung đánh giá.";
        db.Feedbacks.Add(new Feedback { MemberId = memberId, FeedbackType = "Facility", EquipmentId = null, RatingStars = rating, Comment = comment.Trim(), SubmittedDate = DateTime.Now });
        await db.SaveChangesAsync(); return null;
    }
}
