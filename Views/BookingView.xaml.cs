using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public sealed record BookingTypeOption(string Value, string Label);

public partial class BookingView : UserControl
{
    private readonly BookingService _bookingService = new();
    private List<User> _allPts = new();

    public BookingView()
    {
        InitializeComponent();
        BookingDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        var timeSlots = Enumerable.Range(6 * 2, (22 - 6) * 2 + 1)
            .Select(x => TimeSpan.FromMinutes(x * 30).ToString(@"hh\:mm"))
            .ToList();
        StartTimeComboBox.ItemsSource = timeSlots;
        StartTimeComboBox.SelectedItem = "09:00";
        DurationComboBox.ItemsSource = new[] { 30, 60, 90, 120 };
        DurationComboBox.SelectedItem = 60;
        BookingTypeComboBox.ItemsSource = new[]
        {
            new BookingTypeOption("Package", "Dùng gói"),
            new BookingTypeOption("Extra", "Mua thêm")
        };
        BookingTypeComboBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
    }

    private void BookingOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var type = BookingTypeComboBox.SelectedValue as string;
        if (type != "Extra")
        {
            PricePreviewText.Text = "Đã bao gồm trong gói";
            return;
        }

        var pt = PtComboBox.SelectedItem as User;
        var minutes = DurationComboBox.SelectedItem as int? ?? 0;
        PricePreviewText.Text = pt?.PthourlyRate is > 0
            ? $"Giá: {pt.PthourlyRate.Value * minutes / 60m:N0}đ"
            : "PT chưa có giá theo giờ";
    }

    private async Task LoadAsync()
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        var data = await _bookingService.GetDataAsync(user.Id, user.Role);
        MemberComboBox.ItemsSource = data.Members;
        _allPts = data.Pts;
        SpecialtyComboBox.ItemsSource = _allPts
            .Where(x => !string.IsNullOrWhiteSpace(x.Specialty))
            .Select(x => x.Specialty!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        PtComboBox.ItemsSource = _allPts;
        BookingsGrid.ItemsSource = data.Bookings;
        var canCreate = user.Role is UserRoles.Admin or UserRoles.Receptionist or UserRoles.Member;
        CreatePanel.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
        MemberComboBox.IsEnabled = user.Role is UserRoles.Admin or UserRoles.Receptionist;
        if (user.Role == UserRoles.Member && data.Members.Count == 1)
            MemberComboBox.SelectedIndex = 0;
        var canManage = UserSession.Instance.CanManageBookings;
        CompleteButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        CancelBookingButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SpecialtyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var specialty = SpecialtyComboBox.SelectedItem as string;
        PtComboBox.ItemsSource = string.IsNullOrWhiteSpace(specialty)
            ? _allPts
            : _allPts.Where(x => string.Equals(x.Specialty, specialty, StringComparison.OrdinalIgnoreCase)).ToList();
        PtComboBox.SelectedIndex = -1;
    }

    private async void BookButton_Click(object sender, RoutedEventArgs e)
    {
        if (MemberComboBox.SelectedValue is not int memberId || PtComboBox.SelectedValue is not int ptId
            || BookingDatePicker.SelectedDate is not DateTime date
            || BookingTypeComboBox.SelectedValue is not string bookingType
            || DurationComboBox.SelectedItem is not int durationMinutes
            || !TimeSpan.TryParse(StartTimeComboBox.SelectedItem?.ToString(), out var start))
        {
            MessageBox.Show("Vui lòng chọn hội viên, PT, ngày và nhập thời gian hợp lệ.", "Đặt lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        var error = await _bookingService.CreateAsync(
            user.Id, user.Role, memberId, ptId, bookingType, durationMinutes, date.Date.Add(start));
        if (error != null) { MessageBox.Show(error, "Đặt lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await LoadAsync();
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e) => await UpdateStatusAsync("Completed");
    private async void CancelBookingButton_Click(object sender, RoutedEventArgs e) => await UpdateStatusAsync("Cancelled");

    private async Task UpdateStatusAsync(string status)
    {
        if (BookingsGrid.SelectedItem is not Ptbooking booking) return;
        var error = await _bookingService.UpdateStatusAsync(booking.Id, status);
        if (error != null) { MessageBox.Show(error, "Đặt lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await LoadAsync();
    }
}
