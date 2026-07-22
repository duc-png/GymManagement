using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class HomeView : UserControl
{
    private readonly HomeService _homeService = new();

    public HomeView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadHomeAsync();
    }

    private async Task LoadHomeAsync()
    {
        var user = UserSession.Instance.CurrentUser;
        LoginText.Text = user?.FullName ?? "Đăng nhập";
        FeedbackMenuItem.Visibility = user?.Role == UserRoles.Member ? Visibility.Visible : Visibility.Collapsed;

        try
        {
            var data = await _homeService.GetDataAsync();
            PtItemsControl.ItemsSource = data.Pts;
            FeedbackItemsControl.ItemsSource = data.Feedbacks;
            PtCountText.Text = data.PtCount.ToString();
            ActiveMemberCountText.Text = data.ActiveMemberCount.ToString();
            AverageRatingText.Text = data.AverageRatingText;
        }
        catch
        {
            PtItemsControl.ItemsSource = Array.Empty<HomePtCard>();
            FeedbackItemsControl.ItemsSource = Array.Empty<HomeFeedbackCard>();
            PtCountText.Text = "--";
            ActiveMemberCountText.Text = "--";
            AverageRatingText.Text = "--";
        }
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
        FeedbackMenuItem.Visibility = user.Role == UserRoles.Member ? Visibility.Visible : Visibility.Collapsed;

        if (user.Role is UserRoles.Admin or UserRoles.Receptionist or UserRoles.Pt)
            (Window.GetWindow(this) as MainWindow)?.NavigateForRole(user.Role);
    }

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        var user = UserSession.Instance.CurrentUser;
        if (user == null)
        {
            MessageBox.Show("Vui lòng đăng nhập để xem thông tin cá nhân.", "Thông tin cá nhân", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.Equals(user.Role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
        {
            var ptWindow = new PtProfileWindow { Owner = owner };
            ptWindow.ShowDialog();
        }
        else if (string.Equals(user.Role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
        {
            var memberWindow = new ProfileWindow { Owner = owner };
            memberWindow.ShowDialog();
        }
        else
        {
            MessageBox.Show("Tài khoản quản trị không có hồ sơ hội viên để chỉnh sửa.", "Thông tin cá nhân", MessageBoxButton.OK, MessageBoxImage.Information);
        }

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
    {
        if (!UserSession.Instance.IsInRole(UserRoles.Member))
        {
            MessageBox.Show("Chỉ hội viên được gửi đánh giá.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        (Window.GetWindow(this) as MainWindow)?.OpenFeedbackView();
    }

    private void PtSectionButton_Click(object sender, RoutedEventArgs e)
        => PtSection.BringIntoView();

    private void FeedbackSectionButton_Click(object sender, RoutedEventArgs e)
        => FeedbackSection.BringIntoView();

    private void PtCardBookButton_Click(object sender, RoutedEventArgs e)
    {
        if (!UserSession.Instance.IsLoggedIn)
        {
            MessageBox.Show("Vui lòng đăng nhập để xem lịch và đặt lịch PT.", "Lịch PT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        (Window.GetWindow(this) as MainWindow)?.OpenBookingView();
    }

    private void BookPtMenuItem_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as MainWindow)?.OpenBookingView();

    private void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        UserSession.Instance.Logout();
        LoginText.Text = "Đăng nhập";
        FeedbackMenuItem.Visibility = Visibility.Collapsed;
    }
}
