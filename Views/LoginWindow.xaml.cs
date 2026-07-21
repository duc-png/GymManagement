using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class LoginWindow : Window
{
    private readonly UserService _service = new();

    public LoginWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        LoginErrorText.Visibility = Visibility.Collapsed;

        var username = UsernameTextBox.Text.Trim();
        var password = PasswordBox.Password;

        var (user, error) = await _service.LoginAsync(username, password);

        if (error != null)
        {
            LoginErrorText.Text = error;
            LoginErrorText.Visibility = Visibility.Visible;
            return;
        }

        UserSession.Instance.Login(user!);
        DialogResult = true;
        Close();
    }

    private async void RegisterButton_Click(object sender, RoutedEventArgs e)
    {
        RegErrorText.Visibility = Visibility.Collapsed;

        var fullName = RegFullNameTextBox.Text.Trim();
        var username = RegUsernameTextBox.Text.Trim();
        var password = RegPasswordBox.Password;
        var phone = RegPhoneTextBox.Text.Trim();

        var error = await _service.RegisterAsync(fullName, username, password, phone);

        if (error != null)
        {
            RegErrorText.Text = error;
            RegErrorText.Visibility = Visibility.Visible;
            return;
        }

        MessageBox.Show("Đăng ký thành công! Bạn có thể đăng nhập ngay.", "Thành công",
            MessageBoxButton.OK, MessageBoxImage.Information);

        RegFullNameTextBox.Text = "";
        RegUsernameTextBox.Text = "";
        RegPasswordBox.Clear();
        RegPhoneTextBox.Text = "";
        TabControl.SelectedIndex = 0;
    }
}
