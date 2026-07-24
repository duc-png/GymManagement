using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class MyCartView : UserControl
{
    private readonly CartService _cartService = new();
    private readonly PosService _posService = new();
    private List<CartItem> _items = new();

    public MyCartView()
    {
        InitializeComponent();
        PaymentMethodComboBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) return;
        _items = await _cartService.GetAsync(userId.Value);
        CartGrid.ItemsSource = _items;
        TotalText.Text = $"Tổng: {_items.Sum(x => x.UnitPrice * x.Quantity):N0}đ";
        var data = await _posService.GetCatalogAsync();
        PackageComboBox.ItemsSource = data.Packages;
        ProductComboBox.ItemsSource = data.Products.Where(x => (x.StockQuantity ?? 0) > 0).ToList();
        BookingComboBox.ItemsSource = await _cartService.GetMyPendingExtraBookingsAsync(userId.Value);
    }

    private async void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null || ProductComboBox.SelectedValue is not int productId) return;
        if (!int.TryParse(ProductQuantityTextBox.Text, out var quantity) || quantity <= 0)
        {
            MessageBox.Show("Vui lòng nhập số lượng sản phẩm hợp lệ.", "Giỏ hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var error = await _cartService.AddProductAsync(user.Id, productId, quantity);
        if (error != null)
            MessageBox.Show(error, "Giỏ hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
        await LoadAsync();
    }

    private async void AddPackageButton_Click(object sender, RoutedEventArgs e)
    {
        if (PackageComboBox.SelectedValue is not int packageId || UserSession.Instance.CurrentUser == null) return;
        var error = await _cartService.AddPackageAsync(UserSession.Instance.CurrentUser.Id, packageId);
        if (error != null) MessageBox.Show(error, "Giỏ hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
        await LoadAsync();
    }

    private async void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is not CartItem item || UserSession.Instance.CurrentUser == null) return;
        var error = await _cartService.RemoveAsync(UserSession.Instance.CurrentUser.Id, item.Id);
        if (error != null) MessageBox.Show(error, "Giỏ hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
        await LoadAsync();
    }

    private async void AddBookingButton_Click(object sender, RoutedEventArgs e)
    {
        if (BookingComboBox.SelectedValue is not int bookingId || UserSession.Instance.CurrentUser == null) return;
        var error = await _cartService.AddBookingAsync(UserSession.Instance.CurrentUser.Id, bookingId);
        if (error != null) MessageBox.Show(error, "Giỏ hàng", MessageBoxButton.OK, MessageBoxImage.Warning);
        await LoadAsync();
    }

    private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        var method = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        var result = await _cartService.CheckoutAsync(user.Id, method);
        if (result.Error != null)
        {
            MessageBox.Show(result.Error, "Thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var invoice = result.Invoice!;
        if (invoice.PaymentStatus == PaymentStatuses.Paid)
        {
            MessageBox.Show($"Thanh toán thành công: {invoice.InvoiceCode}", "Thanh toán", MessageBoxButton.OK, MessageBoxImage.Information);
            new InvoicePreviewWindow(invoice.Id) { Owner = Window.GetWindow(this) }.ShowDialog();
        }
        else
        {
            MessageBox.Show(
                $"Đã tạo yêu cầu {invoice.InvoiceCode}.\n{invoice.PaymentStatusDisplay}.",
                "Chờ xác nhận thanh toán",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        await LoadAsync();
    }

    private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaymentNoteText == null || CheckoutButton == null)
            return;

        var method = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        PaymentNoteText.Text = method == "Transfer"
            ? "Sau khi chuyển khoản, Receptionist sẽ kiểm tra giao dịch và xác nhận."
            : "Bạn thanh toán tại quầy. Quyền lợi chỉ được kích hoạt sau khi Receptionist nhận tiền.";
        CheckoutButton.Content = method == "Transfer"
            ? "Gửi yêu cầu xác nhận"
            : "Đăng ký trả tại quầy";
    }
}
