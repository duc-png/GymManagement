using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record HomePtCard(
    int Id,
    string FullName,
    string Specialty,
    string Status,
    string? Avatar,
    string HourlyRateText,
    string RatingText);

public sealed record HomeFeedbackCard(
    string MemberName,
    string TargetName,
    string RatingText,
    string Comment,
    string SubmittedDateText);

public sealed record HomeData(
    List<HomePtCard> Pts,
    List<HomeFeedbackCard> Feedbacks,
    int ActiveMemberCount,
    int PtCount,
    string AverageRatingText);

public sealed class HomeService
{
    public async Task<HomeData> GetDataAsync()
    {
        using var db = new GymManagementDbContext();

        var pts = await db.Users
            .AsNoTracking()
            .Where(x => x.Role == UserRoles.Pt)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Specialty,
                x.Ptstatus,
                x.Avatar,
                x.PthourlyRate
            })
            .ToListAsync();

        var ptRatings = await db.Feedbacks
            .AsNoTracking()
            .Where(x => x.FeedbackType == "PT" && x.TargetPtid != null && x.RatingStars != null)
            .GroupBy(x => x.TargetPtid!.Value)
            .Select(group => new
            {
                PtId = group.Key,
                Average = group.Average(x => x.RatingStars!.Value),
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.PtId);

        var ptCards = pts.Select(pt =>
        {
            ptRatings.TryGetValue(pt.Id, out var rating);
            return new HomePtCard(
                pt.Id,
                pt.FullName,
                string.IsNullOrWhiteSpace(pt.Specialty) ? "Chưa cập nhật chuyên môn" : pt.Specialty,
                TranslatePtStatus(pt.Ptstatus),
                pt.Avatar,
                pt.PthourlyRate is > 0 ? $"{pt.PthourlyRate:N0}đ/giờ" : "Liên hệ để biết giá",
                rating == null ? "Chưa có đánh giá" : $"{BuildStars(rating.Average)} {rating.Average:N1}/5 ({rating.Count})");
        }).ToList();

        var recentFeedbacks = await db.Feedbacks
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.TargetPt)
            .Where(x => x.RatingStars != null && x.Comment != "")
            .OrderByDescending(x => x.SubmittedDate)
            .Take(6)
            .ToListAsync();

        var feedbackCards = recentFeedbacks.Select(x => new HomeFeedbackCard(
            x.Member?.FullName ?? "Hội viên",
            x.FeedbackType == "PT"
                ? $"PT {x.TargetPt?.FullName ?? "không xác định"}"
                : "Phòng tập Gym Master",
            $"{BuildStars(x.RatingStars ?? 0)} {x.RatingStars ?? 0}/5",
            x.Comment,
            x.SubmittedDate?.ToString("dd/MM/yyyy") ?? string.Empty)).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var activeMemberCount = await db.MemberPackages
            .AsNoTracking()
            .Where(x => x.Status == "Active" && x.StartDate <= today && x.EndDate >= today)
            .Select(x => x.MemberId)
            .Distinct()
            .CountAsync();

        var averageRating = await db.Feedbacks
            .AsNoTracking()
            .Where(x => x.RatingStars != null)
            .AverageAsync(x => (double?)x.RatingStars) ?? 0;

        return new HomeData(
            ptCards,
            feedbackCards,
            activeMemberCount,
            ptCards.Count,
            averageRating > 0 ? $"{averageRating:N1}/5" : "Chưa có");
    }

    private static string BuildStars(double rating)
    {
        var filled = Math.Clamp((int)Math.Round(rating), 0, 5);
        return new string('★', filled) + new string('☆', 5 - filled);
    }

    private static string TranslatePtStatus(string? status)
        => status switch
        {
            "Available" => "Đang nhận lịch",
            "Busy" => "Đang bận",
            "OnLeave" => "Đang nghỉ",
            _ => "Chưa cập nhật trạng thái"
        };
}
