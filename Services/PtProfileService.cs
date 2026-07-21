using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class PtProfileService
{
    public async Task<User?> GetAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        return await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.Role == UserRoles.Pt);
    }

    public async Task<string?> UpdateAsync(int userId, string fullName, string phone, string? specialty, string? status, string? avatar)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone)) return "Họ tên và số điện thoại là bắt buộc.";
        using var db = new GymManagementDbContext();
        if (await db.Users.AnyAsync(x => x.PhoneNumber == phone && x.Id != userId)) return "Số điện thoại đã được sử dụng.";
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId && x.Role == UserRoles.Pt);
        if (user == null) return "Không tìm thấy tài khoản PT.";
        user.FullName = fullName.Trim(); user.PhoneNumber = phone.Trim(); user.Specialty = string.IsNullOrWhiteSpace(specialty) ? null : specialty.Trim();
        user.Ptstatus = status; if (!string.IsNullOrWhiteSpace(avatar)) user.Avatar = avatar;
        await db.SaveChangesAsync(); return null;
    }
}
