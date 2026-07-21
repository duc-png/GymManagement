using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class InvoicePreviewWindow : Window
{
    private readonly InvoiceService _invoiceService = new();
    private readonly int _invoiceId;

    public InvoicePreviewWindow(int invoiceId)
    {
        InitializeComponent();
        _invoiceId = invoiceId;
        Loaded += async (_, _) => await LoadInvoiceAsync();
    }

    private async Task LoadInvoiceAsync()
    {
        var invoice = await _invoiceService.GetInvoiceAsync(_invoiceId);
        if (invoice == null)
        {
            MessageBox.Show("Không tìm thấy hóa đơn.", "Hóa đơn", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        InvoiceCodeText.Text = invoice.InvoiceCode;
        MemberText.Text = invoice.Member?.FullName ?? "Khách vãng lai";
        CashierText.Text = invoice.User?.FullName ?? "Không xác định";
        CreatedDateText.Text = invoice.CreatedDate?.ToString("dd/MM/yyyy HH:mm");
        DetailsGrid.ItemsSource = invoice.InvoiceDetails;
        TotalText.Text = $"Tổng tiền: {invoice.TotalAmount:N0}đ";
        DiscountText.Text = $"Giảm giá: {invoice.DiscountPercent ?? 0}%";
        FinalAmountText.Text = $"Thanh toán: {invoice.FinalAmount:N0}đ";
        PaymentMethodText.Text = $"Phương thức: {invoice.PaymentMethod}";
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() == true)
            dialog.PrintVisual(InvoicePanel, $"Hóa đơn {_invoiceId}");
    }
}
