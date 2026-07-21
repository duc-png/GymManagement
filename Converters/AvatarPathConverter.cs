using System.Globalization;
using System.Windows.Data;
using GymManagement.Services;

namespace GymManagement.Converters;

public sealed class AvatarPathConverter : IValueConverter
{
    private readonly AvatarService _avatarService = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => _avatarService.LoadImage(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
