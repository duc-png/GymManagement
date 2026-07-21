using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GymManagement.Models;
using GymManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Views;

public partial class MemberView : UserControl
{
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
        using var db = new GymManagementDbContext();
        var members = await db.Members.AsNoTracking().OrderBy(m => m.FullName).ToListAsync();
        foreach (var member in members)
            _members.Add(member);
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchTextBox.Text.Trim();
        foreach (var row in MembersGrid.Items)
        {
            if (row is not Member member) continue;
            var match = string.IsNullOrWhiteSpace(query)
                || member.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.PhoneNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                || member.MemberCode.Contains(query, StringComparison.OrdinalIgnoreCase);
            MembersGrid.ItemContainerGenerator.ContainerFromItem(member)?.SetValue(VisibilityProperty,
                match ? Visibility.Visible : Visibility.Collapsed);
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
            MessageBox.Show("Select a member first.", "Members", MessageBoxButton.OK, MessageBoxImage.Information);
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
        if (MessageBox.Show($"Delete {selected.FullName}?", "Members", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        using var db = new GymManagementDbContext();
        var member = await db.Members.FindAsync(selected.Id);
        if (member == null) return;
        db.Members.Remove(member);
        await db.SaveChangesAsync();
        await LoadMembersAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var fullName = FullNameTextBox.Text.Trim();
        var phone = PhoneTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("Full name and phone are required.", "Members", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = new GymManagementDbContext();
        if (await db.Members.AnyAsync(m => m.PhoneNumber == phone && (_editingMember == null || m.Id != _editingMember.Id)))
        {
            MessageBox.Show("Phone number already belongs to another member.", "Members", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_editingMember == null)
        {
            db.Members.Add(new Member
            {
                MemberCode = await GenerateMemberCodeAsync(db),
                FullName = fullName,
                PhoneNumber = phone,
                Email = NullIfEmpty(EmailTextBox.Text),
                Gender = NullIfEmpty(GenderComboBox.Text),
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today)
            });
        }
        else
        {
            var member = await db.Members.FindAsync(_editingMember.Id);
            if (member == null) return;
            member.FullName = fullName;
            member.PhoneNumber = phone;
            member.Email = NullIfEmpty(EmailTextBox.Text);
            member.Gender = NullIfEmpty(GenderComboBox.Text);
        }

        await db.SaveChangesAsync();
        EditorPanel.Visibility = Visibility.Collapsed;
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

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<string> GenerateMemberCodeAsync(GymManagementDbContext db)
    {
        string code;
        do
        {
            code = $"MB{Guid.NewGuid():N}"[..14].ToUpperInvariant();
        }
        while (await db.Members.AnyAsync(m => m.MemberCode == code));
        return code;
    }
}
