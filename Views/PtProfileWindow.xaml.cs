using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PtProfileWindow : Window
{
    private readonly PtProfileService _service = new();
    private string? _avatar;
    public PtProfileWindow() { InitializeComponent(); Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id; if (userId == null) { Close(); return; }
        var user = await _service.GetAsync(userId.Value); if (user == null) { Close(); return; }
        UsernameTextBox.Text = user.Username; FullNameTextBox.Text = user.FullName; PhoneTextBox.Text = user.PhoneNumber; SpecialtyTextBox.Text = user.Specialty ?? string.Empty; StatusComboBox.Text = user.Ptstatus ?? "Available"; _avatar = user.Avatar; LoadAvatar(_avatar);
    }
    private void ChooseAvatarButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg|All files|*.*" }; if (dialog.ShowDialog() != true) return;
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GymManagement", "Avatars"); Directory.CreateDirectory(folder);
        var fileName = $"pt_{UserSession.Instance.CurrentUser!.Id}_{Guid.NewGuid():N}{Path.GetExtension(dialog.FileName)}"; var target = Path.Combine(folder, fileName); File.Copy(dialog.FileName, target, true); _avatar = target; LoadAvatar(target);
    }
    private void LoadAvatar(string? path) { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) AvatarImage.Source = new BitmapImage(new Uri(path)); }
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser; if (user == null) return;
        var error = await _service.UpdateAsync(user.Id, FullNameTextBox.Text, PhoneTextBox.Text, SpecialtyTextBox.Text, StatusComboBox.Text, _avatar);
        if (error != null) { MessageBox.Show(error, "Tài khoản PT", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        user.FullName = FullNameTextBox.Text.Trim(); user.PhoneNumber = PhoneTextBox.Text.Trim(); user.Specialty = SpecialtyTextBox.Text.Trim(); user.Ptstatus = StatusComboBox.Text; user.Avatar = _avatar;
        MessageBox.Show("Cập nhật tài khoản thành công.", "Tài khoản PT", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
