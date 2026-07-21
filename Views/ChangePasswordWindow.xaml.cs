using System.Windows;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class ChangePasswordWindow : Window
{
    public ChangePasswordWindow() => InitializeComponent();

    private async void ChangeButton_Click(object sender, RoutedEventArgs e)
    {
        var sessionUser = UserSession.Instance.CurrentUser;
        if (sessionUser == null) { Close(); return; }

        if (NewPasswordBox.Password.Length < 6)
        {
            MessageBox.Show("New password must contain at least 6 characters.", "Password", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
        {
            MessageBox.Show("Password confirmation does not match.", "Password", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new GymManagementDbContext();
        var user = await db.Users.FindAsync(sessionUser.Id);
        if (user == null || !BCrypt.Net.BCrypt.Verify(CurrentPasswordBox.Password, user.Password))
        {
            MessageBox.Show("Current password is incorrect.", "Password", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(NewPasswordBox.Password);
        await db.SaveChangesAsync();
        MessageBox.Show("Password changed successfully.", "Password", MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
    }
}
