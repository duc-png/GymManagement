using System.Windows;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class RescheduleBookingWindow : Window
{
    private readonly BookingService _bookingService = new();
    private readonly Ptbooking _booking;

    public DateTime? SelectedStartTime { get; private set; }

    public RescheduleBookingWindow(Ptbooking booking)
    {
        _booking = booking;
        InitializeComponent();

        PtNameText.Text = booking.Pt?.FullName ?? "PT";
        CurrentTimeText.Text = $"{booking.StartTime:dd/MM/yyyy HH:mm}";
        NewDatePicker.DisplayDateStart = DateTime.Today;
        NewDatePicker.SelectedDate = booking.StartTime > DateTime.Now
            ? booking.StartTime.Date
            : DateTime.Today.AddDays(1);

        Loaded += async (_, _) => await LoadAvailableSlotsAsync();
    }

    private async void NewDatePicker_SelectedDateChanged(object? sender, RoutedEventArgs e)
    {
        if (IsLoaded)
            await LoadAvailableSlotsAsync();
    }

    private async Task LoadAvailableSlotsAsync()
    {
        if (NewDatePicker.SelectedDate is not DateTime date)
            return;

        NewStartTimeComboBox.IsEnabled = false;
        SaveButton.IsEnabled = false;
        SlotHintText.Text = "Đang kiểm tra lịch trống...";

        var slots = await _bookingService.GetAvailableSlotsAsync(_booking.Id, date);
        NewStartTimeComboBox.ItemsSource = slots;
        NewStartTimeComboBox.SelectedIndex = slots.Count > 0 ? 0 : -1;
        NewStartTimeComboBox.IsEnabled = slots.Count > 0;
        SaveButton.IsEnabled = slots.Count > 0;
        SlotHintText.Text = slots.Count > 0
            ? $"{slots.Count} khung giờ còn trống"
            : "Ngày này không còn khung giờ phù hợp.";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (NewDatePicker.SelectedDate is not DateTime date
            || !TimeSpan.TryParse(NewStartTimeComboBox.SelectedItem?.ToString(), out var time))
        {
            MessageBox.Show(
                "Vui lòng chọn ngày và giờ bắt đầu mới.",
                "Chuyển lịch PT",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var startTime = date.Date.Add(time);
        if (startTime <= DateTime.Now)
        {
            MessageBox.Show(
                "Thời gian mới phải ở trong tương lai.",
                "Chuyển lịch PT",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SelectedStartTime = startTime;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
