using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class ProductView : UserControl
{
    private readonly ProductService _service = new();
    private List<Product> _products = new();
    private Product? _editing;

    public ProductView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _products = await _service.GetAsync();
        var isAdmin = UserSession.Instance.IsInRole(UserRoles.Admin);
        AddButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        EditButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        DeleteButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchTextBox.Text.Trim();
        ProductsGrid.ItemsSource = string.IsNullOrWhiteSpace(search)
            ? _products
            : _products.Where(x => x.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _editing = null;
        NameTextBox.Clear();
        PriceTextBox.Clear();
        StockTextBox.Text = "0";
        EditorPanel.Visibility = Visibility.Visible;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product) return;
        _editing = product;
        NameTextBox.Text = product.ProductName;
        PriceTextBox.Text = product.Price.ToString("0.##");
        StockTextBox.Text = (product.StockQuantity ?? 0).ToString();
        EditorPanel.Visibility = Visibility.Visible;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        if (!decimal.TryParse(PriceTextBox.Text, out var price) || !int.TryParse(StockTextBox.Text, out var stock))
        {
            MessageBox.Show("Giá và tồn kho phải là số hợp lệ.", "Sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var error = await _service.SaveAsync(_editing?.Id, NameTextBox.Text, price, stock, user.Role);
        if (error != null)
        {
            MessageBox.Show(error, "Sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        EditorPanel.Visibility = Visibility.Collapsed;
        await LoadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product || UserSession.Instance.CurrentUser == null) return;
        if (MessageBox.Show($"Bạn có chắc muốn xóa {product.ProductName}?", "Sản phẩm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        var error = await _service.DeleteAsync(product.Id, UserSession.Instance.CurrentUser.Role);
        if (error != null) MessageBox.Show(error, "Sản phẩm", MessageBoxButton.OK, MessageBoxImage.Warning);
        await LoadAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => EditorPanel.Visibility = Visibility.Collapsed;
}
