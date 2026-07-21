using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public sealed record MemberProfile(User User, Member Member);

public class ProfileService
{
    public async Task<MemberProfile?> GetMemberProfileAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        var user = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId);
        var member = await db.Members
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId);
        return user == null || member == null ? null : new MemberProfile(user, member);
    }

    public async Task<string?> UpdateMemberProfileAsync(int userId, int memberId, string fullName, string phone, DateOnly? dateOfBirth, string? gender)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            return "Họ tên và số điện thoại là bắt buộc.";

        using var db = new GymManagementDbContext();
        if (await db.Users.AnyAsync(x => x.PhoneNumber == phone && x.Id != userId)
            || await db.Members.AnyAsync(x => x.PhoneNumber == phone && x.Id != memberId))
            return "Số điện thoại đã được sử dụng.";

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FindAsync(userId);
        var member = await db.Members.FindAsync(memberId);
        if (user == null || member == null) return "Không tìm thấy hồ sơ hội viên.";

        user.FullName = member.FullName = fullName.Trim();
        user.PhoneNumber = member.PhoneNumber = phone.Trim();
        member.DateOfBirth = dateOfBirth;
        member.Gender = string.IsNullOrWhiteSpace(gender) ? null : gender.Trim();
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }
}
