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
        BookingComboBox.ItemsSource = await _cartService.GetMyPendingExtraBookingsAsync(userId.Value);
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
        MessageBox.Show($"Thanh toán thành công: {result.Invoice!.InvoiceCode}", "Thanh toán", MessageBoxButton.OK, MessageBoxImage.Information);
        new InvoicePreviewWindow(result.Invoice.Id) { Owner = Window.GetWindow(this) }.ShowDialog();
        await LoadAsync();
    }
}
