using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record CheckInResult(bool Success, bool CheckedOut, string Message);

public class CheckInService
{
    public async Task<CheckInResult> ToggleAsync(string memberCode)
    {
        if (string.IsNullOrWhiteSpace(memberCode))
            return new(false, false, "Vui lòng nhập mã hội viên.");

        using var db = new GymManagementDbContext();
        var member = await db.Members.SingleOrDefaultAsync(x => x.MemberCode == memberCode.Trim());
        if (member == null)
            return new(false, false, "Không tìm thấy hội viên.");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var activePackage = await db.MemberPackages.AnyAsync(x =>
            x.MemberId == member.Id && x.Status == "Active" && x.EndDate >= today && x.StartDate <= today);
        if (!activePackage)
            return new(false, false, "Hội viên chưa có gói tập đang hoạt động.");

        var openHistory = await db.CheckInHistories
            .Where(x => x.MemberId == member.Id && x.CheckInTime.HasValue && x.CheckInTime.Value.Date == DateTime.Today && !x.CheckOutTime.HasValue)
            .OrderByDescending(x => x.CheckInTime)
            .FirstOrDefaultAsync();

        if (openHistory != null)
        {
            openHistory.CheckOutTime = DateTime.Now;
            await db.SaveChangesAsync();
            return new(true, true, $"Check-out thành công: {member.FullName}");
        }

        db.CheckInHistories.Add(new CheckInHistory
        {
            MemberId = member.Id,
            CheckInTime = DateTime.Now
        });
        await db.SaveChangesAsync();
        return new(true, false, $"Check-in thành công: {member.FullName}");
    }
}
