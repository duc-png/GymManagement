using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PackageView : UserControl
{
    private readonly PackageService _packageService = new();

    public PackageView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var data = await _packageService.GetAssignmentDataAsync();
        MemberComboBox.ItemsSource = data.Members;
        PackageComboBox.ItemsSource = data.Packages;
        AssignmentsGrid.ItemsSource = data.Assignments;
    }

    private async void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        if (MemberComboBox.SelectedValue is not int memberId || PackageComboBox.SelectedValue is not int packageId)
        {
            MessageBox.Show("Please select a member and a package.", "Membership", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var error = await _packageService.AssignAsync(memberId, packageId);
        if (error != null)
        {
            MessageBox.Show(error, "Membership", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync();
        MessageBox.Show("Package assigned successfully.", "Membership", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
