using LaneLog.Core.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace LaneLog.App.Services;

public sealed class PhotoService(IMediaPicker mediaPicker, PhotoFileStore photoFiles)
{
    public async Task<string?> PickAsync()
    {
        var photos = await MainThread.InvokeOnMainThreadAsync(() =>
            mediaPicker.PickPhotosAsync(new MediaPickerOptions { SelectionLimit = 1 }));

        if (photos.Count > 1)
        {
            throw new PhotoSelectionLimitException();
        }

        return photos.Count == 0 ? null : await CopyAsync(photos[0]);
    }

    public async Task<string?> CaptureAsync()
    {
        if (!mediaPicker.IsCaptureSupported)
        {
            throw new FeatureNotSupportedException("A camera is not available on this device.");
        }

        var photo = await MainThread.InvokeOnMainThreadAsync(() => mediaPicker.CapturePhotoAsync());
        return photo is null ? null : await CopyAsync(photo);
    }

    private async Task<string> CopyAsync(FileResult photo)
    {
        await using var source = await photo.OpenReadAsync();
        return await photoFiles.CopyAsync(source, photo.FileName, photo.ContentType);
    }
}

public sealed class PhotoSelectionLimitException : Exception
{
}
