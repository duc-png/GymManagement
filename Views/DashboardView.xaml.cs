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
        RevenueByPaymentList.ItemsSource = data.RevenueByPayment.Select(x => $"{x.Label}: {x.Value:N0}đ");
        PackageSalesList.ItemsSource = data.PackageSales.Select(x => $"{x.Label}: {x.Value:N0} lượt");
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();
}
