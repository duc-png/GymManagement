using System.Windows;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class ProfileWindow : Window
{
    private readonly ProfileService _profileService = new();
    private int _memberId;

    public ProfileWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) { Close(); return; }
        var profile = await _profileService.GetMemberProfileAsync(userId.Value);
        if (profile == null)
        {
            MessageBox.Show("Member profile was not found.", "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        _memberId = profile.Member.Id;
        MemberCodeTextBox.Text = profile.Member.MemberCode;
        UsernameTextBox.Text = profile.User.Username;
        FullNameTextBox.Text = profile.Member.FullName;
        PhoneTextBox.Text = profile.Member.PhoneNumber;
        EmailTextBox.Text = profile.Member.Email ?? string.Empty;
        GenderComboBox.Text = profile.Member.Gender ?? string.Empty;
        DateOfBirthPicker.SelectedDate = profile.Member.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        DateOnly? date = DateOfBirthPicker.SelectedDate is DateTime value
            ? DateOnly.FromDateTime(value)
            : null;
        var error = await _profileService.UpdateMemberProfileAsync(
            user.Id, _memberId, FullNameTextBox.Text, PhoneTextBox.Text, date, GenderComboBox.Text);
        if (error != null)
        {
            MessageBox.Show(error, "Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        user.FullName = FullNameTextBox.Text.Trim();
        user.PhoneNumber = PhoneTextBox.Text.Trim();
        MessageBox.Show("Profile updated.", "Profile", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
