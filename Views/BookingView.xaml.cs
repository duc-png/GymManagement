using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class BookingView : UserControl
{
    private readonly BookingService _bookingService = new();
    private List<User> _allPts = new();

    public BookingView()
    {
        InitializeComponent();
        BookingDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        Loaded += async (_, _) => await LoadAsync();
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
            || !TimeSpan.TryParse(StartTimeTextBox.Text, out var start)
            || !TimeSpan.TryParse(EndTimeTextBox.Text, out var end))
        {
            MessageBox.Show("Select member, PT, date and valid times.", "Booking", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var user = UserSession.Instance.CurrentUser;
        if (user == null) return;
        var error = await _bookingService.CreateAsync(user.Id, user.Role, memberId, ptId, date.Date.Add(start), date.Date.Add(end));
        if (error != null) { MessageBox.Show(error, "Booking", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await LoadAsync();
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e) => await UpdateStatusAsync("Completed");
    private async void CancelBookingButton_Click(object sender, RoutedEventArgs e) => await UpdateStatusAsync("Cancelled");

    private async Task UpdateStatusAsync(string status)
    {
        if (BookingsGrid.SelectedItem is not Ptbooking booking) return;
        var error = await _bookingService.UpdateStatusAsync(booking.Id, status);
        if (error != null) { MessageBox.Show(error, "Booking", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        await LoadAsync();
    }
}
