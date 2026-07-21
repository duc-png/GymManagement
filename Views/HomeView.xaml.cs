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

    private void MyPackagesMenuItem_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenMyPackagesView();

    private void MyCartMenuItem_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenMyCartView();

    private void FeedbackMenuItem_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenFeedbackView();

    private void PtButton_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenPtPortfolioView();

    private void GymInfoButton_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Gym Master mở cửa từ 06:00 đến 22:00 mỗi ngày.", "Thông tin phòng tập", MessageBoxButton.OK, MessageBoxImage.Information);

    private void ToolsButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.Instance.IsLoggedIn)
        {
            MessageBox.Show("Vui lòng đăng nhập để sử dụng chức năng này.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        (Window.GetWindow(this) as MainWindow)?.OpenBookingView();
    }

    private void ProductsButton_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Chức năng sản phẩm bổ sung đang được hoàn thiện.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

    private void BookPtMenuItem_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenBookingView();

    private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        UserSession.Instance.Logout();
        LoginText.Text = "Login";
    }
}
