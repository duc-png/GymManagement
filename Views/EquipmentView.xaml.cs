using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class EquipmentView : UserControl
{
    private readonly EquipmentService _service = new();
    private List<Equipment> _items = new();
    private Equipment? _editing;

    public EquipmentView()
    {
        InitializeComponent();
        PurchaseDatePicker.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _items = await _service.GetAsync();
        var isAdmin = UserSession.Instance.IsInRole(UserRoles.Admin);
        var allowed = UserSession.Instance.IsInRole(UserRoles.Admin, UserRoles.Receptionist);
        AddButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        EditButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        MaintainButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        CompleteRepairButton.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
        ReportButton.Visibility = allowed ? Visibility.Visible : Visibility.Collapsed;

        SetFilterItems(StatusFilterComboBox, "Trạng thái", _items.Select(x => x.Status));
        SetFilterItems(TypeFilterComboBox, "Loại thiết bị", _items.Select(x => x.EquipmentType));
        SetFilterItems(LocationFilterComboBox, "Vị trí", _items.Select(x => x.Location));
        ApplyFilter();
    }

    private static void SetFilterItems(ComboBox comboBox, string allLabel, IEnumerable<string?> values)
    {
        var items = new List<string> { allLabel };
        items.AddRange(values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x));
        comboBox.ItemsSource = items;
        comboBox.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        var search = SearchTextBox.Text.Trim();
        var status = StatusFilterComboBox.SelectedIndex > 0 ? StatusFilterComboBox.SelectedItem?.ToString() : null;
        var type = TypeFilterComboBox.SelectedIndex > 0 ? TypeFilterComboBox.SelectedItem?.ToString() : null;
        var location = LocationFilterComboBox.SelectedIndex > 0 ? LocationFilterComboBox.SelectedItem?.ToString() : null;

        EquipmentGrid.ItemsSource = _items.Where(x =>
            (string.IsNullOrWhiteSpace(search) || $"{x.EquipmentCode} {x.EquipmentName} {x.EquipmentType} {x.Location}".Contains(search, StringComparison.OrdinalIgnoreCase))
            && (status == null || x.Status == status)
            && (type == null || x.EquipmentType == type)
            && (location == null || x.Location == location)).ToList();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (IsLoaded) ApplyFilter(); }
    private void AddButton_Click(object sender, RoutedEventArgs e) { _editing = null; CodeTextBox.Clear(); NameTextBox.Clear(); TypeTextBox.Clear(); LocationTextBox.Clear(); PurchaseDatePicker.SelectedDate = DateTime.Today; EditorPanel.Visibility = Visibility.Visible; }
    private void EditButton_Click(object sender, RoutedEventArgs e) { if (EquipmentGrid.SelectedItem is not Equipment x) return; _editing = x; CodeTextBox.Text = x.EquipmentCode; NameTextBox.Text = x.EquipmentName; TypeTextBox.Text = x.EquipmentType; LocationTextBox.Text = x.Location; PurchaseDatePicker.SelectedDate = x.PurchaseDate.ToDateTime(TimeOnly.MinValue); EditorPanel.Visibility = Visibility.Visible; }
    private async void SaveButton_Click(object sender, RoutedEventArgs e) { var user = UserSession.Instance.CurrentUser; if (user == null) return; var date = PurchaseDatePicker.SelectedDate is DateTime d ? DateOnly.FromDateTime(d) : DateOnly.FromDateTime(DateTime.Today); var error = await _service.SaveAsync(_editing?.Id, CodeTextBox.Text, NameTextBox.Text, TypeTextBox.Text, LocationTextBox.Text, date, user.Role); if (error != null) { MessageBox.Show(error, "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning); return; } EditorPanel.Visibility = Visibility.Collapsed; await LoadAsync(); }
    private async void ReportButton_Click(object sender, RoutedEventArgs e) { if (EquipmentGrid.SelectedItem is not Equipment x) return; var role = UserSession.Instance.CurrentUser?.Role ?? ""; var error = await _service.ReportIssueAsync(x.Id, "Báo hỏng từ hệ thống", role); if (error != null) MessageBox.Show(error, "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning); await LoadAsync(); }
    private async void MaintainButton_Click(object sender, RoutedEventArgs e) { if (EquipmentGrid.SelectedItem is not Equipment x) return; var role = UserSession.Instance.CurrentUser?.Role ?? ""; var error = await _service.StartMaintenanceAsync(x.Id, role); if (error != null) MessageBox.Show(error, "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning); await LoadAsync(); }
    private async void CompleteRepairButton_Click(object sender, RoutedEventArgs e) { if (EquipmentGrid.SelectedItem is not Equipment x) return; var user = UserSession.Instance.CurrentUser; if (user == null) return; var error = await _service.CompleteRepairAsync(x.Id, 0, user.FullName, "Hoàn tất sửa chữa từ hệ thống", user.Role); if (error != null) MessageBox.Show(error, "Thiết bị", MessageBoxButton.OK, MessageBoxImage.Warning); await LoadAsync(); }
}
