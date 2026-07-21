using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.ViewModels;

public sealed record BookingCalendarEntry(
    int BookingId,
    string Time,
    string Participant,
    string Status);

public sealed record BookingCalendarDay(
    DateTime Date,
    int DayNumber,
    bool IsCurrentMonth,
    bool IsToday,
    bool IsSelected,
    IReadOnlyList<BookingCalendarEntry> Entries,
    int MoreCount);

public sealed class BookingViewModel
{
    private const int VisibleEntriesPerDay = 2;

    public IReadOnlyList<BookingCalendarDay> BuildMonth(
        DateTime displayedMonth,
        DateTime selectedDate,
        IEnumerable<Ptbooking> bookings,
        string role)
    {
        var monthStart = new DateTime(displayedMonth.Year, displayedMonth.Month, 1);
        var daysFromMonday = ((int)monthStart.DayOfWeek + 6) % 7;
        var calendarStart = monthStart.AddDays(-daysFromMonday);
        var bookingsByDate = bookings
            .Where(x => x.Status != "Cancelled")
            .GroupBy(x => x.StartTime.Date)
            .ToDictionary(x => x.Key, x => x.OrderBy(b => b.StartTime).ToList());

        return Enumerable.Range(0, 42)
            .Select(offset => CreateDay(
                calendarStart.AddDays(offset),
                monthStart,
                selectedDate,
                bookingsByDate,
                role))
            .ToList();
    }

    private static BookingCalendarDay CreateDay(
        DateTime date,
        DateTime monthStart,
        DateTime selectedDate,
        IReadOnlyDictionary<DateTime, List<Ptbooking>> bookingsByDate,
        string role)
    {
        bookingsByDate.TryGetValue(date.Date, out var dayBookings);
        dayBookings ??= new List<Ptbooking>();

        var entries = dayBookings
            .Take(VisibleEntriesPerDay)
            .Select(x => new BookingCalendarEntry(
                x.Id,
                $"{x.StartTime:HH:mm}-{x.EndTime:HH:mm}",
                GetParticipantName(x, role),
                x.Status ?? "Pending"))
            .ToList();

        return new BookingCalendarDay(
            date,
            date.Day,
            date.Month == monthStart.Month && date.Year == monthStart.Year,
            date.Date == DateTime.Today,
            date.Date == selectedDate.Date,
            entries,
            Math.Max(0, dayBookings.Count - VisibleEntriesPerDay));
    }

    private static string GetParticipantName(Ptbooking booking, string role)
    {
        if (string.Equals(role, UserRoles.Pt, StringComparison.OrdinalIgnoreCase))
            return booking.Member?.FullName ?? "Hội viên";
        if (string.Equals(role, UserRoles.Member, StringComparison.OrdinalIgnoreCase))
            return booking.Pt?.FullName ?? "PT";

        var member = booking.Member?.FullName ?? "Hội viên";
        var pt = booking.Pt?.FullName ?? "PT";
        return $"{member} - {pt}";
    }
}
