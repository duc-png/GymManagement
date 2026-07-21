using System.Windows;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Views;

public partial class PurchaseHistoryWindow : Window
{
    public PurchaseHistoryWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        var userId = UserSession.Instance.CurrentUser?.Id;
        if (userId == null) { Close(); return; }

        using var db = new GymManagementDbContext();
        InvoicesGrid.ItemsSource = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Member != null && i.Member.UserId == userId)
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();
    }
}
