using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class UserService
{
    public async Task<(User? User, string? Error)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return (null, "Vui lòng nhập đầy đủ thông tin!");

        using var db = new GymManagementDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return (null, "Tên đăng nhập không tồn tại!");

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            return (null, "Mật khẩu không chính xác!");

        if (user.Status != "Active")
            return (null, "Tài khoản đã bị khóa!");

        if (!UserRoles.IsKnown(user.Role))
            return (null, "Tài khoản chưa được cấu hình quyền truy cập.");

        return (user, null);
    }

    public async Task<string?> RegisterAsync(string fullName, string username, string password, string phoneNumber)
    {
        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(phoneNumber))
            return "Vui lòng nhập đầy đủ thông tin!";

        using var db = new GymManagementDbContext();

        if (await db.Users.AnyAsync(u => u.Username == username))
            return "Tên đăng nhập đã tồn tại!";

        if (await db.Users.AnyAsync(u => u.PhoneNumber == phoneNumber))
            return "Số điện thoại đã được đăng ký!";

        if (await db.Members.AnyAsync(m => m.PhoneNumber == phoneNumber))
            return "Số điện thoại đã thuộc về một hội viên.";

        await using var transaction = await db.Database.BeginTransactionAsync();

        var newUser = new User
        {
            Username = username,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Role = "Member",
            Status = "Active"
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        var newMember = new Member
        {
            MemberCode = await GenerateMemberCodeAsync(db),
            FullName = fullName,
            PhoneNumber = phoneNumber,
            RegistrationDate = DateOnly.FromDateTime(DateTime.Today),
            UserId = newUser.Id
        };

        db.Members.Add(newMember);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return null;
    }

    public async Task<string?> ChangePasswordAsync(int userId, string currentPassword, string newPassword, string confirmation)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return "Vui lòng nhập mật khẩu hiện tại và mật khẩu mới.";

        if (newPassword.Length < 6)
            return "Mật khẩu mới phải có ít nhất 6 ký tự.";

        if (newPassword != confirmation)
            return "Mật khẩu xác nhận không khớp.";

        using var db = new GymManagementDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return "Không tìm thấy tài khoản người dùng.";

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            return "Mật khẩu hiện tại không chính xác.";

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync();
        return null;
    }

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
