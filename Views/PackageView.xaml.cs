using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PackageView : UserControl
{
    private readonly PackageService _packageService = new();
    private List<PackageTemplate> _packages = new();

    public PackageView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var data = await _packageService.GetAssignmentDataAsync();
        MemberComboBox.ItemsSource = data.Members;
        _packages = data.Packages;
        PackageComboBox.ItemsSource = _packages;
        AssignmentsGrid.ItemsSource = data.Assignments;
    }

    private void PackageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PackageComboBox.SelectedItem is not PackageTemplate package)
        {
            PackageDetailsText.Text = string.Empty;
            return;
        }

        var ptDetails = package.HasPt == true
            ? $"PT: {package.PtSessions ?? 0} buổi, {package.PtminutesPerSession ?? 0} phút/buổi"
            : "PT: Không bao gồm";
        PackageDetailsText.Text = $"Giá: {package.Price:N0}đ | Thời hạn: {package.DurationMonths} tháng | {ptDetails}";
    }

    private async void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        if (MemberComboBox.SelectedValue is not int memberId || PackageComboBox.SelectedValue is not int packageId)
        {
            MessageBox.Show("Vui lòng chọn hội viên và gói tập.", "Gán gói tập", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var error = await _packageService.AssignAsync(memberId, packageId);
        if (error != null)
        {
            MessageBox.Show(error, "Gán gói tập", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync();
        MessageBox.Show("Gán gói tập thành công.", "Gán gói tập", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
