using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PaymentConfirmationView : UserControl
{
    private readonly PaymentService _paymentService = new();

    public PaymentConfirmationView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var payments = await _paymentService.GetPendingAsync();
        PaymentsGrid.ItemsSource = payments;
        PendingCountText.Text = $"{payments.Count} yêu cầu đang chờ";
        DetailsGrid.ItemsSource = null;
        DetailTitleText.Text = "Chi tiết yêu cầu";
    }

    private void PaymentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaymentsGrid.SelectedItem is not Invoice invoice)
        {
            DetailsGrid.ItemsSource = null;
            DetailTitleText.Text = "Chi tiết yêu cầu";
            return;
        }

        DetailsGrid.ItemsSource = invoice.InvoiceDetails;
        DetailTitleText.Text = $"{invoice.InvoiceCode} · {invoice.FinalAmount:N0}đ";
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        => await LoadAsync();

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentsGrid.SelectedItem is not Invoice invoice)
        {
            ShowSelectionRequired();
            return;
        }

        var confirmation = MessageBox.Show(
            invoice.PaymentMethod == "Cash"
                ? $"Xác nhận đã nhận {invoice.FinalAmount:N0}đ tiền mặt từ {invoice.Member?.FullName}?"
                : $"Xác nhận giao dịch chuyển khoản {invoice.FinalAmount:N0}đ đã vào tài khoản?",
            "Xác nhận thanh toán",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes)
            return;

        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) return;

        var error = await _paymentService.ConfirmAsync(invoice.Id, userId.Value);
        if (error != null)
        {
            MessageBox.Show(error, "Xác nhận thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(
            "Đã xác nhận thanh toán và cập nhật quyền lợi của hội viên.",
            "Xác nhận thanh toán",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        new InvoicePreviewWindow(invoice.Id) { Owner = Window.GetWindow(this) }.ShowDialog();
        await LoadAsync();
    }

    private async void RejectButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentsGrid.SelectedItem is not Invoice invoice)
        {
            ShowSelectionRequired();
            return;
        }

        var confirmation = MessageBox.Show(
            $"Từ chối yêu cầu {invoice.InvoiceCode}?",
            "Từ chối thanh toán",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
            return;

        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) return;

        var error = await _paymentService.RejectAsync(invoice.Id, userId.Value);
        if (error != null)
        {
            MessageBox.Show(error, "Từ chối thanh toán", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync();
    }

    private static void ShowSelectionRequired()
        => MessageBox.Show(
            "Vui lòng chọn một yêu cầu thanh toán.",
            "Xác nhận thanh toán",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
}
