using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class DashboardView : UserControl
{
    private readonly DashboardService _service = new();
    public DashboardView()
    {
        InitializeComponent();
        FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
        ToDatePicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!UserSession.Instance.IsInRole(UserRoles.Admin)) return;
        var from = FromDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);
        var to = ToDatePicker.SelectedDate ?? DateTime.Today;
        var data = await _service.GetAsync(from, to);
        TotalMembersText.Text = data.TotalMembers.ToString("N0");
        ActiveMembersText.Text = data.ActiveMembers.ToString("N0");
        PendingBookingsText.Text = data.PendingBookings.ToString("N0");
        BrokenEquipmentText.Text = data.BrokenEquipment.ToString("N0");
        RevenueText.Text = $"{data.Revenue:N0}đ";
        AverageTrainingText.Text = $"Thời lượng tập trung bình: {data.AverageTrainingMinutes:N0} phút";
        RevenueByPaymentList.ItemsSource = FormatMetrics(data.RevenueByPayment, "đ");
        PackageSalesList.ItemsSource = FormatMetrics(data.PackageSales, "lượt");
        ProductSalesList.ItemsSource = FormatMetrics(data.ProductSales, "sản phẩm");
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private static IEnumerable<string> FormatMetrics(
        IReadOnlyCollection<DashboardMetric> metrics,
        string unit)
        => metrics.Count == 0
            ? new[] { "Chưa có dữ liệu" }
            : metrics.Select(x => $"{x.Label}: {x.Value:N0} {unit}");
}
