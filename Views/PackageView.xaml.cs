using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Views;

public partial class PackageView : UserControl
{
    public PackageView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        using var db = new GymManagementDbContext();
        MemberComboBox.ItemsSource = await db.Members.AsNoTracking().OrderBy(m => m.FullName).ToListAsync();
        PackageComboBox.ItemsSource = await db.PackageTemplates.AsNoTracking().OrderBy(p => p.PackageName).ToListAsync();
        AssignmentsGrid.ItemsSource = await db.MemberPackages
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.PackageTemplate)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    private async void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        if (MemberComboBox.SelectedValue is not int memberId ||
            PackageComboBox.SelectedValue is not int packageId)
        {
            MessageBox.Show("Please select a member and a package.", "Membership", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new GymManagementDbContext();
        var package = await db.PackageTemplates.FindAsync(packageId);
        if (package == null) return;

        var startDate = DateOnly.FromDateTime(DateTime.Today);
        var endDate = startDate.AddMonths(package.DurationMonths);

        var hasActivePackage = await db.MemberPackages.AnyAsync(x =>
            x.MemberId == memberId && x.Status == "Active" && x.EndDate >= startDate);
        if (hasActivePackage)
        {
            MessageBox.Show("This member already has an active package.", "Membership", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        db.MemberPackages.Add(new MemberPackage
        {
            MemberId = memberId,
            PackageTemplateId = packageId,
            StartDate = startDate,
            EndDate = endDate,
            RemainingPtsessions = package.HasPt == true ? 1 : 0,
            Status = "Active"
        });

        await db.SaveChangesAsync();
        await LoadAsync();
        MessageBox.Show("Package assigned successfully.", "Membership", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
