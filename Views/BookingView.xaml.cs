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
    private readonly int? _preselectedPtId;
    private List<User> _allPts = new();
    private List<Ptbooking> _allBookings = new();
    private List<Ptbooking> _calendarBookings = new();
    private DateTime _displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedDate = DateTime.Today;
    private bool _isLoading;
    private bool _syncingPtSelection;
    private bool _calendarShowsPtAvailability;
    private int _slotRequestVersion;

    public BookingView(int? preselectedPtId = null)
    {
        _preselectedPtId = preselectedPtId;
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
        => UpdatePricePreview();

    private void UpdatePricePreview()
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

        int? requestedPtId = CalendarPtComboBox.SelectedValue is int calendarPtId
            ? calendarPtId
            : PtComboBox.SelectedValue is int bookingPtId
                ? bookingPtId
                : _preselectedPtId;

        var data = await _bookingService.GetDataAsync(user);
        _isLoading = true;
        try
        {
            MemberComboBox.ItemsSource = data.Members;
            _allPts = data.Pts;
            _allBookings = data.Bookings;
            _calendarBookings = _allBookings;

            SpecialtyComboBox.ItemsSource = _allPts
                .Where(x => !string.IsNullOrWhiteSpace(x.Specialty))
                .Select(x => x.Specialty!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();
            PtComboBox.ItemsSource = _allPts;
            CalendarPtComboBox.ItemsSource = _allPts;

            if (requestedPtId is int selectedPtId
                && _allPts.FirstOrDefault(x => x.Id == selectedPtId) is User selectedPt)
            {
                SpecialtyComboBox.SelectedItem = selectedPt.Specialty;
                PtComboBox.SelectedValue = selectedPt.Id;
                CalendarPtComboBox.SelectedValue = selectedPt.Id;
            }
            else if (user.Role == UserRoles.Member && _allPts.Count > 0)
            {
                PtComboBox.SelectedIndex = 0;
                CalendarPtComboBox.SelectedIndex = 0;
            }

            var canCreate = user.Role is UserRoles.Admin or UserRoles.Receptionist or UserRoles.Member;
            CreatePanel.Visibility = canCreate ? Visibility.Visible : Visibility.Collapsed;
            MemberComboBox.IsEnabled = user.Role is UserRoles.Admin or UserRoles.Receptionist;
            if (user.Role == UserRoles.Member && data.Members.Count == 1)
                MemberComboBox.SelectedIndex = 0;

            var canManage = UserSession.Instance.CanManageBookings;
            CompleteButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            CancelBookingButton.Visibility = canManage ? Visibility.Visible : Visibility.Collapsed;
            RescheduleButton.Visibility = canManage || user.Role == UserRoles.Member
                ? Visibility.Visible
                : Visibility.Collapsed;
            CalendarPtPanel.Visibility = user.Role == UserRoles.Member
                ? Visibility.Visible
                : Visibility.Collapsed;
            MemberColumn.Visibility = user.Role == UserRoles.Member ? Visibility.Collapsed : Visibility.Visible;
            PtColumn.Visibility = user.Role == UserRoles.Pt ? Visibility.Collapsed : Visibility.Visible;
            PageTitleText.Text = user.Role == UserRoles.Pt ? "Lịch dạy của tôi" : "Lịch tập với PT";
        }
        finally
        {
            _isLoading = false;
        }

        if (user.Role == UserRoles.Member && CalendarPtComboBox.SelectedValue is int)
            await LoadSelectedPtScheduleAsync();
        else
            RefreshCalendar();

        UpdatePricePreview();
        await RefreshAvailableStartTimesAsync();
    }

    private void RefreshCalendar()
    {
        var role = UserSession.Instance.CurrentRole ?? string.Empty;
        var selectedPt = CalendarPtComboBox.SelectedItem as User;
        MonthTitleText.Text = _calendarShowsPtAvailability && selectedPt != null
            ? $"{selectedPt.FullName} · {_displayedMonth:MM/yyyy}"
            : $"Tháng {_displayedMonth:MM/yyyy}";
        SelectedDateTitleText.Text = $"Chi tiết ngày {_selectedDate:dd/MM/yyyy}";
        CalendarDaysItemsControl.ItemsSource = _viewModel.BuildMonth(
            _displayedMonth,
            _selectedDate,
            _calendarBookings,
            role,
            _calendarShowsPtAvailability,
            _allBookings.Select(x => x.Id).ToHashSet());
        BookingsGrid.ItemsSource = _allBookings
            .Where(x => x.StartTime.Date == _selectedDate.Date)
            .OrderBy(x => x.StartTime)
            .ToList();
    }

    private async void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(-1);
        await SelectFirstDayOfDisplayedMonthAsync();
    }

    private async void NextMonthButton_Click(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(1);
        await SelectFirstDayOfDisplayedMonthAsync();
    }

    private async void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today;
        _displayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await ReloadDisplayedCalendarAsync();
    }

    private async Task SelectFirstDayOfDisplayedMonthAsync()
    {
        _selectedDate = _displayedMonth;
        await ReloadDisplayedCalendarAsync();
    }

    private async void CalendarDayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DateTime date }) return;

        _selectedDate = date.Date;
        var changedMonth = date.Month != _displayedMonth.Month || date.Year != _displayedMonth.Year;
        if (date.Month != _displayedMonth.Month || date.Year != _displayedMonth.Year)
            _displayedMonth = new DateTime(date.Year, date.Month, 1);

        if (UserSession.Instance.CurrentRole == UserRoles.Member && date.Date >= DateTime.Today)
            BookingDatePicker.SelectedDate = date.Date;

        if (changedMonth)
            await ReloadDisplayedCalendarAsync();
        else
            RefreshCalendar();
    }

    private void SpecialtyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _syncingPtSelection)
            return;

        var specialty = SpecialtyComboBox.SelectedItem as string;
        PtComboBox.ItemsSource = string.IsNullOrWhiteSpace(specialty)
            ? _allPts
            : _allPts.Where(x => string.Equals(x.Specialty, specialty, StringComparison.OrdinalIgnoreCase)).ToList();
        PtComboBox.SelectedIndex = -1;

        if (UserSession.Instance.CurrentRole == UserRoles.Member)
        {
            CalendarPtComboBox.SelectedIndex = -1;
            _calendarBookings = _allBookings;
            _calendarShowsPtAvailability = false;
            RefreshCalendar();
        }
    }

    private async void PtComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePricePreview();
        if (_isLoading || _syncingPtSelection || !IsLoaded)
            return;

        if (UserSession.Instance.CurrentRole == UserRoles.Member
            && PtComboBox.SelectedValue is int ptId)
        {
            _syncingPtSelection = true;
            CalendarPtComboBox.SelectedValue = ptId;
            _syncingPtSelection = false;
            await LoadSelectedPtScheduleAsync();
        }

        await RefreshAvailableStartTimesAsync();
    }

    private async void CalendarPtComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || _syncingPtSelection || !IsLoaded)
            return;

        if (CalendarPtComboBox.SelectedValue is int ptId)
        {
            _syncingPtSelection = true;
            SpecialtyComboBox.SelectedIndex = -1;
            PtComboBox.ItemsSource = _allPts;
            PtComboBox.SelectedValue = ptId;
            _syncingPtSelection = false;
            await LoadSelectedPtScheduleAsync();
            await RefreshAvailableStartTimesAsync();
            return;
        }

        _calendarBookings = _allBookings;
        _calendarShowsPtAvailability = false;
        RefreshCalendar();
    }

    private async void BookingDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded && !_isLoading)
            await RefreshAvailableStartTimesAsync();
    }

    private async void DurationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdatePricePreview();
        if (IsLoaded && !_isLoading)
            await RefreshAvailableStartTimesAsync();
    }

    private async Task ReloadDisplayedCalendarAsync()
    {
        if (UserSession.Instance.CurrentRole == UserRoles.Member
            && CalendarPtComboBox.SelectedValue is int)
        {
            await LoadSelectedPtScheduleAsync();
            return;
        }

        _calendarBookings = _allBookings;
        _calendarShowsPtAvailability = false;
        RefreshCalendar();
    }

    private async Task LoadSelectedPtScheduleAsync()
    {
        if (CalendarPtComboBox.SelectedValue is not int ptId)
        {
            _calendarBookings = _allBookings;
            _calendarShowsPtAvailability = false;
            RefreshCalendar();
            return;
        }

        _calendarBookings = await _bookingService.GetPtScheduleAsync(ptId, _displayedMonth);
        _calendarShowsPtAvailability = true;
        RefreshCalendar();
    }

    private async Task RefreshAvailableStartTimesAsync()
    {
        if (PtComboBox.SelectedValue is not int ptId
            || BookingDatePicker.SelectedDate is not DateTime date
            || DurationComboBox.SelectedItem is not int durationMinutes)
        {
            StartTimeComboBox.ItemsSource = Array.Empty<string>();
            StartTimeComboBox.IsEnabled = false;
            return;
        }

        var requestVersion = ++_slotRequestVersion;
        var previousSelection = StartTimeComboBox.SelectedItem?.ToString();
        StartTimeComboBox.IsEnabled = false;

        var slots = await _bookingService.GetAvailableSlotsAsync(ptId, date, durationMinutes);
        if (requestVersion != _slotRequestVersion)
            return;

        StartTimeComboBox.ItemsSource = slots;
        StartTimeComboBox.SelectedItem = previousSelection != null && slots.Contains(previousSelection)
            ? previousSelection
            : slots.FirstOrDefault();
        StartTimeComboBox.IsEnabled = slots.Count > 0;
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
        MessageBox.Show(
            bookingType == "Package"
                ? "Đặt lịch thành công. Gói tập đã được trừ 1 buổi PT."
                : "Đặt lịch thành công. Vui lòng thanh toán booking trong giỏ hàng.",
            "Đặt lịch PT",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
        => await UpdateStatusAsync("Completed");

    private async void CancelBookingButton_Click(object sender, RoutedEventArgs e)
        => await UpdateStatusAsync("Cancelled");

    private async void RescheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (BookingsGrid.SelectedItem is not Ptbooking booking)
        {
            MessageBox.Show(
                "Vui lòng chọn một lịch trong bảng chi tiết.",
                "Chuyển lịch PT",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var dialog = new RescheduleBookingWindow(booking)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() != true || dialog.SelectedStartTime is not DateTime newStartTime)
            return;

        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;

        var error = await _bookingService.RescheduleAsync(
            user.Id,
            user.Role,
            booking.Id,
            newStartTime);
        if (error != null)
        {
            MessageBox.Show(error, "Chuyển lịch PT", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedDate = newStartTime.Date;
        _displayedMonth = new DateTime(newStartTime.Year, newStartTime.Month, 1);
        await LoadAsync();
        MessageBox.Show(
            "Chuyển lịch thành công.",
            "Chuyển lịch PT",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

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
