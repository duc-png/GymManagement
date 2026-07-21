using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;

namespace GymManagement.Views;

public partial class MemberView : UserControl
{
    private readonly MemberService _memberService = new();
    private readonly ObservableCollection<Member> _members = new();
    private Member? _editingMember;

    public MemberView()
    {
        InitializeComponent();
        MembersGrid.ItemsSource = _members;
        Loaded += async (_, _) => await LoadMembersAsync();
    }

    private async Task LoadMembersAsync()
    {
        _members.Clear();
        foreach (var member in await _memberService.GetMembersAsync())
            _members.Add(member);
        ApplySearchFilter();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplySearchFilter();

    private void ApplySearchFilter()
    {
        var query = SearchTextBox.Text.Trim();
        foreach (var row in MembersGrid.Items)
        {
            if (row is not Member member) continue;
            var match = string.IsNullOrWhiteSpace(query)
                || member.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.PhoneNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.MemberCode.Contains(query, StringComparison.OrdinalIgnoreCase);
            MembersGrid.ItemContainerGenerator.ContainerFromItem(member)?.SetValue(
                VisibilityProperty, match ? Visibility.Visible : Visibility.Collapsed);
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _editingMember = null;
        ClearEditor();
        EditorPanel.Visibility = Visibility.Visible;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (MembersGrid.SelectedItem is not Member member)
        {
            MessageBox.Show("Vui lòng chọn hội viên trước.", "Hội viên", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _editingMember = member;
        FullNameTextBox.Text = member.FullName;
        PhoneTextBox.Text = member.PhoneNumber;
        EmailTextBox.Text = member.Email ?? string.Empty;
        GenderComboBox.Text = member.Gender ?? string.Empty;
        EditorPanel.Visibility = Visibility.Visible;
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (MembersGrid.SelectedItem is not Member selected) return;
        if (MessageBox.Show($"Bạn có chắc muốn xóa {selected.FullName} không?", "Hội viên", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var error = await _memberService.DeleteAsync(selected.Id);
        if (error != null)
        {
            MessageBox.Show(error, "Hội viên", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        await LoadMembersAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var error = await _memberService.SaveAsync(
            _editingMember?.Id,
            FullNameTextBox.Text,
            PhoneTextBox.Text,
            EmailTextBox.Text,
            GenderComboBox.Text);

        if (error != null)
        {
            MessageBox.Show(error, "Hội viên", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        EditorPanel.Visibility = Visibility.Collapsed;
        _editingMember = null;
        await LoadMembersAsync();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        EditorPanel.Visibility = Visibility.Collapsed;
        _editingMember = null;
    }

    private void MembersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void ClearEditor()
    {
        FullNameTextBox.Clear();
        PhoneTextBox.Clear();
        EmailTextBox.Clear();
        GenderComboBox.SelectedIndex = -1;
    }
}
