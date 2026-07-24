using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class FeedbackSubmitView : UserControl
{
    private readonly FeedbackService _service = new();
    private int _rating = 5;

    public FeedbackSubmitView()
    {
        InitializeComponent();
        TypeComboBox.SelectedIndex = 0;
        Star5Button.IsChecked = true;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var user = UserSession.Instance.CurrentUser;
        if (user == null || !UserSession.Instance.IsInRole(UserRoles.Member))
        {
            IsEnabled = false;
            MessageBox.Show("Chỉ hội viên được gửi đánh giá.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var pts = await _service.GetEligiblePtsAsync(user.Id);
        PtComboBox.ItemsSource = pts;
        PtComboBox.SelectedIndex = pts.Count > 0 ? 0 : -1;
        PtComboBox.IsEnabled = pts.Count > 0;
        PtEligibilityText.Text = pts.Count > 0
            ? "Chỉ hiển thị PT bạn đã hoàn thành buổi tập và chưa đánh giá."
            : "Hiện chưa có PT đủ điều kiện để đánh giá.";
        EligiblePtCountText.Text = pts.Count > 0
            ? $"{pts.Count} PT đang chờ đánh giá"
            : "Chưa có PT chờ đánh giá";
    }
    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PtPanel == null || FacilityRequirementText == null) return;
        var isPtFeedback = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "PT";
        PtPanel.Visibility = isPtFeedback ? Visibility.Visible : Visibility.Collapsed;
        FacilityRequirementText.Visibility = isPtFeedback ? Visibility.Collapsed : Visibility.Visible;
    }

    private void RatingButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton button && int.TryParse(button.Tag?.ToString(), out var rating))
        {
            _rating = rating;
            if (RatingText != null)
                RatingText.Text = $"{rating}/5";

            var buttons = new[]
            {
                Star1Button,
                Star2Button,
                Star3Button,
                Star4Button,
                Star5Button
            };
            for (var index = 0; index < buttons.Length; index++)
                buttons[index].Foreground = index < rating
                    ? System.Windows.Media.Brushes.Goldenrod
                    : new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(199, 204, 211));
        }
    }

    private void CommentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (CommentCountText != null)
            CommentCountText.Text = $"{CommentTextBox.Text.Length} ký tự";
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser; if (user == null) return;
        var type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        SubmitButton.IsEnabled = false;
        string? error;
        try
        {
            if (type == "PT")
            {
                if (PtComboBox.SelectedValue is not int ptId)
                {
                    MessageBox.Show("Vui lòng chọn PT.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                error = await _service.SubmitPtAsync(user.Id, ptId, _rating, CommentTextBox.Text);
            }
            else
            {
                error = await _service.SubmitFacilityAsync(user.Id, _rating, CommentTextBox.Text);
            }

            if (error != null)
            {
                MessageBox.Show(error, "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Gửi đánh giá thành công.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Information);
            CommentTextBox.Clear();
            Star5Button.IsChecked = true;
            if (type == "PT")
                await LoadAsync();
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }
}
