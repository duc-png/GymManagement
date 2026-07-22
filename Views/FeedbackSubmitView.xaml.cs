using System.Windows;
using System.Windows.Controls;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class FeedbackSubmitView : UserControl
{
    private readonly FeedbackService _service = new();
    public FeedbackSubmitView()
    {
        InitializeComponent(); TypeComboBox.SelectedIndex = 0; RatingComboBox.SelectedIndex = 4;
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

        PtComboBox.ItemsSource = await _service.GetEligiblePtsAsync(user.Id);
    }
    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PtPanel == null || FacilityRequirementText == null) return;
        var isPtFeedback = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "PT";
        PtPanel.Visibility = isPtFeedback ? Visibility.Visible : Visibility.Collapsed;
        FacilityRequirementText.Visibility = isPtFeedback ? Visibility.Collapsed : Visibility.Visible;
    }
    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        var user = UserSession.Instance.CurrentUser; if (user == null) return;
        if (!int.TryParse((RatingComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(), out var rating)) return;
        var type = (TypeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        string? error;
        if (type == "PT")
        {
            if (PtComboBox.SelectedValue is not int ptId) { MessageBox.Show("Vui lòng chọn PT.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            error = await _service.SubmitPtAsync(user.Id, ptId, rating, CommentTextBox.Text);
        }
        else error = await _service.SubmitFacilityAsync(user.Id, rating, CommentTextBox.Text);
        if (error != null) { MessageBox.Show(error, "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        MessageBox.Show("Gửi đánh giá thành công.", "Đánh giá", MessageBoxButton.OK, MessageBoxImage.Information); CommentTextBox.Clear();
    }
}
