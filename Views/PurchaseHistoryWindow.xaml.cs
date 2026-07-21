using System.Windows;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PurchaseHistoryWindow : Window
{
    private readonly InvoiceService _invoiceService = new();

    public PurchaseHistoryWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) { Close(); return; }
        InvoicesGrid.ItemsSource = await _invoiceService.GetMemberPurchaseHistoryAsync(userId.Value);
    }
}
