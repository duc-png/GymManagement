using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PosView : UserControl
{
    private readonly PosService _posService = new();
    private readonly ObservableCollection<PosItem> _cart = new();
    private List<Product> _products = new();
    private List<PackageTemplate> _packages = new();
    private List<Ptbooking> _extraBookings = new();

    public PosView()
    {
        InitializeComponent();
        CartGrid.ItemsSource = _cart;
        PaymentMethodComboBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var data = await _posService.GetCatalogAsync();
        _products = data.Products;
        _packages = data.Packages;
        ProductComboBox.ItemsSource = _products;
        PackageComboBox.ItemsSource = _packages;
        MemberComboBox.ItemsSource = data.Members;
        _extraBookings = data.ExtraBookings;
        FilterExtraBookings();
    }

    private void AddProductButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is not Product product) return;
        _cart.Add(new PosItem("Product", product.Id, product.ProductName, product.Price));
        UpdateTotal();
    }

    private void AddPackageButton_Click(object sender, RoutedEventArgs e)
    {
        if (PackageComboBox.SelectedItem is not PackageTemplate package) return;
        _cart.Add(new PosItem("Package", package.Id, package.PackageName, package.Price));
        UpdateTotal();
    }

    private void MemberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => FilterExtraBookings();

    private void FilterExtraBookings()
    {
        var memberId = MemberComboBox?.SelectedValue as int?;
        ExtraBookingComboBox.ItemsSource = memberId == null
            ? _extraBookings
            : _extraBookings.Where(x => x.MemberId == memberId).ToList();
    }

    private void AddBookingButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExtraBookingComboBox.SelectedItem is not Ptbooking booking) return;
        _cart.Add(new PosItem("PTBooking", booking.Id,
            $"PT {booking.Pt?.FullName} - {booking.StartTime:g}", booking.Price));
        UpdateTotal();
    }

    private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateTotal();

    private void UpdateTotal()
    {
        var total = _cart.Sum(x => x.UnitPrice);
        _ = int.TryParse(DiscountTextBox?.Text, out var discount);
        discount = Math.Clamp(discount, 0, 100);
        TotalText.Text = $"Tổng: {total * (1 - discount / 100m):N0}đ";
    }

    private async void CheckoutButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        var method = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Cash";
        if (user == null) return;
        int.TryParse(DiscountTextBox.Text, out var discount);
        var memberId = MemberComboBox.SelectedValue as int?;
        var result = await _posService.CheckoutAsync(user.Id, memberId, _cart, discount, method);
        if (result.Error != null)
        {
            MessageBox.Show(result.Error, "Thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show($"Tạo hóa đơn thành công: {result.Invoice!.InvoiceCode}", "Thanh toán", MessageBoxButton.OK, MessageBoxImage.Information);
        new InvoicePreviewWindow(result.Invoice.Id) { Owner = Window.GetWindow(this) }.ShowDialog();
        _cart.Clear();
        UpdateTotal();
        await LoadAsync();
    }
}
