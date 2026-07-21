using System.Windows;
using Microsoft.Win32;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PtProfileWindow : Window
{
    private readonly PtProfileService _service = new();
    private readonly AvatarService _avatarService = new();
    private string? _avatar;

    public bool WasUpdated { get; private set; }

    public PtProfileWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null)
        {
            Close();
            return;
        }

        var user = await _service.GetAsync(userId.Value);
        if (user == null)
        {
            MessageBox.Show("Không tìm thấy hồ sơ PT.", "Tài khoản PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        UsernameTextBox.Text = user.Username;
        FullNameTextBox.Text = user.FullName;
        PhoneTextBox.Text = user.PhoneNumber;
        SpecialtyTextBox.Text = user.Specialty ?? string.Empty;
        StatusComboBox.Text = user.Ptstatus ?? "Available";
        _avatar = user.Avatar;
        AvatarImage.Source = _avatarService.LoadImage(_avatar);
    }

    private void ChooseAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Tệp ảnh|*.png;*.jpg;*.jpeg",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;

        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) return;

        try
        {
            _avatar = _avatarService.SavePtAvatar(userId.Value, dialog.FileName);
            AvatarImage.Source = _avatarService.LoadImage(_avatar);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Ảnh đại diện", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;

        var error = await _service.UpdateAsync(
            user.Id,
            FullNameTextBox.Text,
            PhoneTextBox.Text,
            SpecialtyTextBox.Text,
            StatusComboBox.Text,
            _avatar);
        if (error != null)
        {
            MessageBox.Show(error, "Tài khoản PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        user.FullName = FullNameTextBox.Text.Trim();
        user.PhoneNumber = PhoneTextBox.Text.Trim();
        user.Specialty = SpecialtyTextBox.Text.Trim();
        user.Ptstatus = StatusComboBox.Text;
        user.Avatar = _avatar;
        WasUpdated = true;
        MessageBox.Show("Cập nhật tài khoản thành công.", "Tài khoản PT", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
