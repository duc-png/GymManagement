using System.Windows;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly UserService _userService = new();

    public ChangePasswordWindow() => InitializeComponent();

    private async void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        var sessionUser = UserSession.Instance.CurrentUser;
        if (sessionUser == null) { Close(); return; }

        var error = await _userService.ChangePasswordAsync(
            sessionUser.Id,
            CurrentPasswordBox.Password,
            NewPasswordBox.Password,
            ConfirmPasswordBox.Password);

        if (error != null)
        {
            MessageBox.Show(error, "Đổi mật khẩu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show("Đổi mật khẩu thành công.", "Đổi mật khẩu", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
    }
}
