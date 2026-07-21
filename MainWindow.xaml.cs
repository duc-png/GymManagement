using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GymManagement.Services;
using GymManagement.Views;

namespace GymManagement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public void NavigateForRole(string? role)
        {
            UserControl view = role switch
            {
                UserRoles.Admin => new DashboardView(),
                UserRoles.Receptionist => new MemberView(),
                UserRoles.Pt => new BookingView(),
                _ => new HomeView()
            };

            ContentHost.Content = view;
            WorkspaceBar.Visibility = view is HomeView ? Visibility.Collapsed : Visibility.Visible;
            RoleText.Text = role == null ? string.Empty : $"Role: {role}";
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
            => NavigateForRole(null);

        private void PackagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Instance.IsInRole(UserRoles.Member))
            {
                OpenMyPackagesView();
                return;
            }
            if (!UserSession.Instance.CanManageMembers)
            {
                MessageBox.Show("Bạn không có quyền quản lý gói tập.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new PackageView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        public void OpenMyPackagesView()
        {
            ContentHost.Content = new MyPackagesView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        public void OpenPtPortfolioView()
        {
            ContentHost.Content = new PtPortfolioView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void CheckInButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.CanManageMembers)
            {
                MessageBox.Show("Chỉ Admin và Receptionist được sử dụng check-in.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new CheckInView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void PtPortfolioButton_Click(object sender, RoutedEventArgs e)
        {
            ContentHost.Content = new PtPortfolioView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void BookingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.CanViewOwnPtBookings)
            {
                MessageBox.Show("Bạn không có quyền xem lịch PT.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new BookingView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        public void OpenBookingView()
        {
            ContentHost.Content = new BookingView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Instance.Logout();
            NavigateForRole(null);
        }
    }
}
