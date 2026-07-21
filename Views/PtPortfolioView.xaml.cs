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
        => PtList.ItemsSource = await _ptService.GetPortfolioAsync();
}
