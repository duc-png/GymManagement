using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class PtPortfolioView : UserControl
{
    private readonly PtService _ptService = new();

    public PtPortfolioView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        var pts = await _ptService.GetPortfolioAsync();
        PtList.ItemsSource = pts;
        PtCountText.Text = pts.Count.ToString();
    }

    private void ViewScheduleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int ptId })
            (Window.GetWindow(this) as MainWindow)?.OpenBookingView(ptId);
    }
}
