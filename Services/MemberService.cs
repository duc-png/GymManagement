using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class MemberService
{
    public async Task<List<Member>> GetMembersAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Members.AsNoTracking().OrderBy(m => m.FullName).ToListAsync();
    }

    public async Task<string?> SaveAsync(int? memberId, string fullName, string phone, string? email, string? gender)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
            return "Full name and phone are required.";

        using var db = new GymManagementDbContext();
        if (await db.Members.AnyAsync(m => m.PhoneNumber == phone && m.Id != memberId))
            return "Phone number already belongs to another member.";

        if (memberId == null)
        {
            db.Members.Add(new Member
            {
                MemberCode = await GenerateMemberCodeAsync(db),
                FullName = fullName.Trim(),
                PhoneNumber = phone.Trim(),
                Email = NullIfEmpty(email),
                Gender = NullIfEmpty(gender),
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }
        else
        {
            var member = await db.Members.FindAsync(memberId.Value);
            if (member == null) return "Member was not found.";

            member.FullName = fullName.Trim();
            member.PhoneNumber = phone.Trim();
            member.Email = NullIfEmpty(email);
            member.Gender = NullIfEmpty(gender);
        }

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> DeleteAsync(int memberId)
    {
        using var db = new GymManagementDbContext();
        var member = await db.Members.FindAsync(memberId);
        if (member == null) return "Member was not found.";

        db.Members.Remove(member);
        await db.SaveChangesAsync();
        return null;
    }

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<string> GenerateMemberCodeAsync(GymManagementDbContext db)
    {
        string code;
        do
        {
            code = $"MB{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        }
        while (await db.Members.AnyAsync(m => m.MemberCode == code));
        return code;
    }
}
