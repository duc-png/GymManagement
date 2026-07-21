using System.IO;
using System.Windows.Media.Imaging;

namespace GymManagement.Services;

public sealed class AvatarService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg"
    };

    public string SavePtAvatar(int userId, string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new InvalidOperationException("Không tìm thấy tệp ảnh đã chọn.");

        var extension = Path.GetExtension(sourcePath);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Ảnh đại diện chỉ hỗ trợ định dạng PNG, JPG hoặc JPEG.");

        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GymManagement",
            "Avatars");
        Directory.CreateDirectory(folder);

        var targetPath = Path.Combine(folder, $"pt_{userId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        File.Copy(sourcePath, targetPath, true);
        return targetPath;
    }

    public BitmapImage? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;

        try
        {
            using var stream = File.OpenRead(path);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }
}
