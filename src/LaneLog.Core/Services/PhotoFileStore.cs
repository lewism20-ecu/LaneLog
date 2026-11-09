namespace LaneLog.Core.Services;

public sealed class PhotoFileStore
{
    private const string PhotoDirectoryName = "photos";
    private readonly string photoDirectory;

    public PhotoFileStore(string appDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appDataDirectory);
        photoDirectory = Path.Combine(Path.GetFullPath(appDataDirectory), PhotoDirectoryName);
    }

    public async Task<string> CopyAsync(Stream source, string? originalFileName, string? contentType = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        Directory.CreateDirectory(photoDirectory);
        var fileName = $"{Guid.NewGuid():N}{GetExtension(originalFileName, contentType)}";
        var fullPath = Path.Combine(photoDirectory, fileName);
        var created = false;
        var completed = false;

        try
        {
            await using (var destination = new FileStream(
                fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                created = true;
                await source.CopyToAsync(destination);
            }

            completed = true;
            return Path.Combine(PhotoDirectoryName, fileName);
        }
        finally
        {
            if (created && !completed)
            {
                File.Delete(fullPath);
            }
        }
    }

    public string GetFullPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var directory = Path.GetDirectoryName(relativePath);
        var fileName = Path.GetFileName(relativePath);
        var extension = Path.GetExtension(fileName);
        if (Path.IsPathRooted(relativePath)
            || !string.Equals(directory, PhotoDirectoryName, StringComparison.Ordinal)
            || !Guid.TryParseExact(Path.GetFileNameWithoutExtension(fileName), "N", out _)
            || !IsSupportedExtension(extension))
        {
            throw new InvalidDataException("The saved photo path is not an app-owned photo.");
        }

        return Path.Combine(photoDirectory, fileName);
    }

    public void Delete(string relativePath) => File.Delete(GetFullPath(relativePath));

    private static string GetExtension(string? fileName, string? contentType)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (extension is not null && IsSupportedExtension(extension))
        {
            return extension;
        }

        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/heic" => ".heic",
            "image/heif" => ".heif",
            _ => ".img"
        };
    }

    private static bool IsSupportedExtension(string extension) => extension is
        ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" or ".heic" or ".heif" or ".img";
}
