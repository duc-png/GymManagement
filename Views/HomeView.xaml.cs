using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (UserSession.Instance.IsLoggedIn)
        {
            AccountMenu.PlacementTarget = LoginButton;
            AccountMenu.IsOpen = true;
            return;
        }

        var loginWindow = new LoginWindow { Owner = Window.GetWindow(this) };
        if (loginWindow.ShowDialog() != true) return;

        var user = UserSession.Instance.CurrentUser!;
        LoginText.Text = user.FullName;

        if (user.Role is UserRoles.Admin or UserRoles.Receptionist or UserRoles.Pt)
            (Window.GetWindow(this) as MainWindow)?.NavigateForRole(user.Role);
    }

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProfileWindow { Owner = Window.GetWindow(this) };
        window.ShowDialog();
        if (UserSession.Instance.CurrentUser != null)
            LoginText.Text = UserSession.Instance.CurrentUser.FullName;
    }

    private void ChangePasswordMenuItem_Click(object sender, RoutedEventArgs e)
        => new ChangePasswordWindow { Owner = Window.GetWindow(this) }.ShowDialog();

    private void PurchaseHistoryMenuItem_Click(object sender, RoutedEventArgs e)
        => new PurchaseHistoryWindow { Owner = Window.GetWindow(this) }.ShowDialog();

    private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        UserSession.Instance.Logout();
        LoginText.Text = "Login";
    }
}
