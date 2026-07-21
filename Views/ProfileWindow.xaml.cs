using System.Windows;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Views;

public partial class ProfileWindow : Window
{
    private int _memberId;

    public ProfileWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) { Close(); return; }

        using var db = new GymManagementDbContext();
        var member = await db.Members.AsNoTracking().SingleOrDefaultAsync(m => m.UserId == user.Id);
        if (member == null)
        {
            MessageBox.Show("Member profile was not found.", "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _memberId = member.Id;
        MemberCodeTextBox.Text = member.MemberCode;
        UsernameTextBox.Text = user.Username;
        FullNameTextBox.Text = member.FullName;
        PhoneTextBox.Text = member.PhoneNumber;
        EmailTextBox.Text = member.Email ?? string.Empty;
        GenderComboBox.Text = member.Gender ?? string.Empty;
        DateOfBirthPicker.SelectedDate = member.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var sessionUser = UserSession.Instance.CurrentUser;
        if (sessionUser == null) return;

        var fullName = FullNameTextBox.Text.Trim();
        var phone = PhoneTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("Full name and phone are required.", "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new GymManagementDbContext();
        var duplicate = await db.Users.AnyAsync(u => u.PhoneNumber == phone && u.Id != sessionUser.Id)
            || await db.Members.AnyAsync(m => m.PhoneNumber == phone && m.Id != _memberId);
        if (duplicate)
        {
            MessageBox.Show("Phone number is already in use.", "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var user = await db.Users.FindAsync(sessionUser.Id);
        var member = await db.Members.FindAsync(_memberId);
        if (user == null || member == null) return;

        user.FullName = member.FullName = fullName;
        user.PhoneNumber = member.PhoneNumber = phone;
        member.Gender = string.IsNullOrWhiteSpace(GenderComboBox.Text) ? null : GenderComboBox.Text;
        member.DateOfBirth = DateOfBirthPicker.SelectedDate is DateTime date
            ? DateOnly.FromDateTime(date)
            : null;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        sessionUser.FullName = fullName;
        sessionUser.PhoneNumber = phone;
        MessageBox.Show("Profile updated.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
