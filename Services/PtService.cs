using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record PtPortfolioCard(
    int Id,
    string FullName,
    string Specialty,
    string Status,
    string StatusBackground,
    string? Avatar,
    string HourlyRate,
    string Rating,
    int MediaCount);

public class PtService
{
    public async Task<List<PtPortfolioCard>> GetPortfolioAsync()
    {
        using var db = new GymManagementDbContext();
        var pts = await db.Users.AsNoTracking()
            .Where(x => x.Role == UserRoles.Pt)
            .OrderBy(x => x.FullName)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Specialty,
                x.Ptstatus,
                x.Avatar,
                x.PthourlyRate,
                MediaCount = x.Ptmedia.Count
            })
            .ToListAsync();

        var ratings = await db.Feedbacks.AsNoTracking()
            .Where(x => x.FeedbackType == "PT" && x.TargetPtid != null && x.RatingStars != null)
            .GroupBy(x => x.TargetPtid!.Value)
            .Select(group => new
            {
                PtId = group.Key,
                Average = group.Average(x => x.RatingStars!.Value),
                Count = group.Count()
            })
            .ToDictionaryAsync(x => x.PtId);

        return pts.Select(pt =>
        {
            ratings.TryGetValue(pt.Id, out var rating);
            var (status, statusBackground) = GetStatusDisplay(pt.Ptstatus);

            return new PtPortfolioCard(
                pt.Id,
                pt.FullName,
                string.IsNullOrWhiteSpace(pt.Specialty) ? "Chưa cập nhật chuyên môn" : pt.Specialty,
                status,
                statusBackground,
                pt.Avatar,
                pt.PthourlyRate is > 0 ? $"{pt.PthourlyRate:N0}đ / giờ" : "Chưa cập nhật mức giá",
                rating == null
                    ? "Chưa có đánh giá"
                    : $"{BuildStars(rating.Average)}  {rating.Average:N1}/5 · {rating.Count} đánh giá",
                pt.MediaCount);
        }).ToList();
    }

    private static (string Label, string Background) GetStatusDisplay(string? status)
        => status switch
        {
            "Available" => ("Đang nhận lịch", "#E7F6EC"),
            "Busy" => ("Lịch đang bận", "#FFF4E5"),
            "OnLeave" => ("Đang nghỉ", "#FDECEC"),
            _ => ("Chưa cập nhật", "#EEF1F5")
        };

    private static string BuildStars(double rating)
    {
        var filled = Math.Clamp((int)Math.Round(rating), 0, 5);
        return new string('★', filled) + new string('☆', 5 - filled);
    }
}
