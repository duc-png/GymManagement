using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;
using GymManagement.ViewModels;

namespace GymManagement.Views;

public sealed record BookingTypeOption(string Value, string Label);

public partial class BookingView : UserControl
{
    private readonly BookingService _bookingService = new();
    private readonly BookingViewModel _viewModel = new();
    private List<User> _allPts = new();
    private List<Ptbooking> _allBookings = new();
    private DateTime _displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today;

    public BookingView()
    {
        InitializeComponent();
        BookingDatePicker.SelectedDate = DateTime.Today.AddDays(1);

        StartTimeComboBox.ItemsSource = Enumerable.Range(6 * 2, (22 - 6) * 2 + 1)
            .Select(x => TimeSpan.FromMinutes(x * 30).ToString(@"hh\:mm"))
            .ToList();
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

        var data = await _bookingService.GetDataAsync(user);
        MemberComboBox.ItemsSource = data.Members;
        _allPts = data.Pts;
        SpecialtyComboBox.ItemsSource = _allPts
            .Where(x => !string.IsNullOrWhiteSpace(x.Specialty))
            .Select(x => x.Specialty!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        PtComboBox.ItemsSource = _allPts;
        _allBookings = data.Bookings;

        var canCreate = user.Role is UserRoles.Admin or UserRoles.Receptionist or UserRoles.Member;
        CreatePanel.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
        MemberComboBox.IsEnabled = user.Role is UserRoles.Admin or UserRoles.Receptionist;
        if (user.Role == UserRoles.Member && data.Members.Count == 1)
            MemberComboBox.SelectedIndex = 0;

        var canManage = UserSession.Instance.CanManageBookings;
        CompleteButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        CancelBookingButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
        MemberColumn.Visibility = user.Role == UserRoles.Member ? Visibility.Collapsed : Visibility.Visible;
        PtColumn.Visibility = user.Role == UserRoles.Pt ? Visibility.Collapsed : Visibility.Visible;
        PageTitleText.Text = user.Role == UserRoles.Pt ? "Lịch dạy của tôi" : "Lịch tập với PT";
        RefreshCalendar();
    }

    private void RefreshCalendar()
    {
        var role = UserSession.Instance.CurrentRole ?? string.Empty;
        MonthTitleText.Text = $"Tháng {_displayedMonth:MM/yyyy}";
        SelectedDateTitleText.Text = $"Chi tiết ngày {_selectedDate:dd/MM/yyyy}";
        CalendarDaysItemsControl.ItemsSource = _viewModel.BuildMonth(
            _displayedMonth,
            _selectedDate,
            _allBookings,
            role);
        BookingsGrid.ItemsSource = _allBookings
            .Where(x => x.StartTime.Date == _selectedDate.Date)
            .OrderBy(x => x.StartTime)
            .ToList();
    }

    private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(-1);
        SelectFirstDayOfDisplayedMonth();
    }

    private void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(1);
        SelectFirstDayOfDisplayedMonth();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today;
        _displayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RefreshCalendar();
    }

    private void SelectFirstDayOfDisplayedMonth()
    {
        _selectedDate = _displayedMonth;
        RefreshCalendar();
    }

    private void CalendarDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date }) return;

        _selectedDate = date.Date;
        if (date.Month != _displayedMonth.Month || date.Year != _displayedMonth.Year)
            _displayedMonth = new DateTime(date.Year, date.Month, 1);
        RefreshCalendar();
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
            MessageBox.Show(
                "Vui lòng chọn hội viên, PT, ngày và thời gian hợp lệ.",
                "Đặt lịch PT",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        var startTime = date.Date.Add(start);
        var error = await _bookingService.CreateAsync(
            user.Id,
            user.Role,
            memberId,
            ptId,
            bookingType,
            durationMinutes,
            startTime);
        if (error != null)
        {
            MessageBox.Show(error, "Đặt lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedDate = startTime.Date;
        _displayedMonth = new DateTime(startTime.Year, startTime.Month, 1);
        await LoadAsync();
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        => await UpdateStatusAsync("Completed");

    private async void CancelBookingButton_Click(object sender, RoutedEventArgs e)
        => await UpdateStatusAsync("Cancelled");

    private async Task UpdateStatusAsync(string status)
    {
        if (BookingsGrid.SelectedItem is not Ptbooking booking)
        {
            MessageBox.Show("Vui lòng chọn một lịch trong bảng chi tiết.", "Lịch PT", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var error = await _bookingService.UpdateStatusAsync(booking.Id, status);
        if (error != null)
        {
            MessageBox.Show(error, "Lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await LoadAsync();
    }
}
