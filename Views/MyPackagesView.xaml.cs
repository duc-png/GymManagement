using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class MyPackagesView : UserControl
{
    private readonly MemberPackageService _packageService = new();
    private List<MemberPackage> _packages = new();

    public MyPackagesView()
    {
        InitializeComponent();
        StatusFilterComboBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) return;
        _packages = await _packageService.GetMyPackagesAsync(userId.Value);
        ApplyFilter();
    }

    private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ApplyFilter();

    private void ApplyFilter()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var package in _packages)
            package.Status = package.EndDate >= today && package.StartDate <= today ? "Còn hạn" : "Hết hạn";

        var tag = (StatusFilterComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        PackagesGrid.ItemsSource = tag switch
        {
            "Active" => _packages.Where(x => x.Status == "Còn hạn").ToList(),
            "Expired" => _packages.Where(x => x.Status == "Hết hạn").ToList(),
            _ => _packages
        };
    }
}
