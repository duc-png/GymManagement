using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class FeedbackView : UserControl
{
    private readonly FeedbackService _service = new();
    private readonly bool _startWithOwnPtFeedback;
    private List<FeedbackDisplayItem> _feedbacks = new();

    public FeedbackView(bool startWithOwnPtFeedback = false)
    {
        _startWithOwnPtFeedback = startWithOwnPtFeedback;
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync(_startWithOwnPtFeedback);
    }

    private async Task LoadAsync(bool ownPtFeedback)
    {
        var user = UserSession.Instance.CurrentUser;
        var canViewOwn = UserSession.Instance.IsInRole(UserRoles.Pt);
        MyFeedbackButton.Visibility = canViewOwn ? Visibility.Visible : Visibility.Collapsed;

        if (ownPtFeedback && canViewOwn && user != null)
        {
            TitleText.Text = "Đánh giá về tôi";
            _feedbacks = await _service.GetReceivedByPtAsync(user.Id);
        }
        else
        {
            TitleText.Text = "Tất cả đánh giá";
            _feedbacks = await _service.GetAllFeedbackAsync();
        }

        ApplyFilter();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyFilter();

    private void ApplyFilter()
    {
        if (FeedbackGrid == null) return;
        var search = SearchTextBox?.Text.Trim();
        FeedbackGrid.ItemsSource = string.IsNullOrWhiteSpace(search)
            ? _feedbacks
            : _feedbacks.Where(x =>
                x.MemberName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.TargetName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || x.Comment.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
    }

    private async void MyFeedbackButton_Click(object sender, RoutedEventArgs e)
        => await LoadAsync(true);

    private async void AllFeedbackButton_Click(object sender, RoutedEventArgs e)
        => await LoadAsync(false);
}
