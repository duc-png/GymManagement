using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Media;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class CheckInView : UserControl
{
    private readonly CheckInService _checkInService = new();

    public CheckInView() => InitializeComponent();

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        var result = await _checkInService.ToggleAsync(MemberCodeTextBox.Text);
        ResultText.Text = result.Message;
        ResultBorder.Background = result.Success
            ? new SolidColorBrush(Color.FromRgb(220, 252, 231))
            : new SolidColorBrush(Color.FromRgb(254, 226, 226));
        ResultText.Foreground = result.Success
            ? new SolidColorBrush(Color.FromRgb(22, 101, 52))
            : new SolidColorBrush(Color.FromRgb(153, 27, 27));
        if (result.Success) SystemSounds.Beep.Play();
        if (result.Success) MemberCodeTextBox.Clear();
        MemberCodeTextBox.Focus();
    }
}
