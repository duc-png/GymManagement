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
            ConfigureWorkspace(role);
        }

        private void ConfigureWorkspace(string? role)
        {
            var admin = role == UserRoles.Admin;
            var receptionist = role == UserRoles.Receptionist;
            var pt = role == UserRoles.Pt;
            var member = role == UserRoles.Member;

            HomeNavButton.Content = admin ? "Dashboard" : "Home";
            HomeNavButton.Visibility = Visibility.Visible;
            PackagesNavButton.Content = member ? "Gói tập của tôi" : "Gói tập";
            PackagesNavButton.Visibility = receptionist || member ? Visibility.Visible : Visibility.Collapsed;
            CheckInNavButton.Visibility = receptionist ? Visibility.Visible : Visibility.Collapsed;
            PtPortfolioNavButton.Visibility = pt || member ? Visibility.Visible : Visibility.Collapsed;
            BookingsNavButton.Visibility = receptionist || pt || member ? Visibility.Visible : Visibility.Collapsed;
            CartNavButton.Visibility = member ? Visibility.Visible : Visibility.Collapsed;
            PurchaseHistoryNavButton.Visibility = member ? Visibility.Visible : Visibility.Collapsed;
            FeedbackNavButton.Visibility = member ? Visibility.Visible : Visibility.Collapsed;
            PtReceivedFeedbackNavButton.Visibility = pt ? Visibility.Visible : Visibility.Collapsed;
            PosNavButton.Visibility = receptionist ? Visibility.Visible : Visibility.Collapsed;
            PaymentConfirmationNavButton.Visibility = receptionist ? Visibility.Visible : Visibility.Collapsed;
            EquipmentNavButton.Visibility = admin || receptionist ? Visibility.Visible : Visibility.Collapsed;
            ProductsNavButton.Visibility = admin || receptionist ? Visibility.Visible : Visibility.Collapsed;
            AccountNavButton.Visibility = pt || member ? Visibility.Visible : Visibility.Collapsed;
            PasswordNavButton.Visibility = pt || member ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Instance.IsInRole(UserRoles.Admin))
            {
                ContentHost.Content = new DashboardView();
                WorkspaceBar.Visibility = Visibility.Visible;
                ConfigureWorkspace(UserRoles.Admin);
                return;
            }

            NavigateForRole(null);
        }

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
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
        }

        public void OpenMyCartView()
        {
            ContentHost.Content = new MyCartView();
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
        }

        public void OpenFeedbackView()
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Member))
            {
                MessageBox.Show("Chỉ hội viên được gửi đánh giá.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ContentHost.Content = new FeedbackSubmitView();
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
        }

        public void OpenAllFeedbackView()
        {
            ContentHost.Content = new FeedbackView();
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
        }

        public void OpenPtPortfolioView()
        {
            ContentHost.Content = new PtPortfolioView();
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
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

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Member)) return;
            OpenMyCartView();
        }

        private void PurchaseHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Member)) return;
            new PurchaseHistoryWindow { Owner = this }.ShowDialog();
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Member)) return;
            OpenFeedbackView();
        }

        private void PtReceivedFeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Pt)) return;
            ContentHost.Content = new FeedbackView(startWithOwnPtFeedback: true);
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserRoles.Pt);
        }

        private void PosButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.CanManagePos)
            {
                MessageBox.Show("Chỉ Admin và Receptionist được sử dụng POS.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new PosView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void PaymentConfirmationButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.IsInRole(UserRoles.Receptionist))
            {
                MessageBox.Show(
                    "Chỉ Receptionist được xác nhận thanh toán.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ContentHost.Content = new PaymentConfirmationView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void EquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.CanManageMembers)
            {
                MessageBox.Show("Chỉ Admin và Receptionist được sử dụng quản lý thiết bị.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new EquipmentView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!UserSession.Instance.CanManagePos)
            {
                MessageBox.Show("Chỉ Admin và Receptionist được xem danh sách sản phẩm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ContentHost.Content = new ProductView();
            WorkspaceBar.Visibility = Visibility.Visible;
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Instance.CurrentRole == UserRoles.Pt)
            {
                var profileWindow = new PtProfileWindow { Owner = this };
                profileWindow.ShowDialog();
                if (profileWindow.WasUpdated && ContentHost.Content is PtPortfolioView)
                    ContentHost.Content = new PtPortfolioView();
            }
            else if (UserSession.Instance.CurrentRole == UserRoles.Member)
                new ProfileWindow { Owner = this }.ShowDialog();
            else
                MessageBox.Show("Chức năng tài khoản này dành cho PT và Member.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (UserSession.Instance.IsLoggedIn)
                new ChangePasswordWindow { Owner = this }.ShowDialog();
        }

        public void OpenBookingView(int? selectedPtId = null)
        {
            ContentHost.Content = new BookingView(selectedPtId);
            WorkspaceBar.Visibility = Visibility.Visible;
            ConfigureWorkspace(UserSession.Instance.CurrentRole);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Instance.Logout();
            NavigateForRole(null);
        }
    }
}
