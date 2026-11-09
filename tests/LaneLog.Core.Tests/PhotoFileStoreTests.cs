using LaneLog.Core.Models;
using LaneLog.Core.Services;

namespace LaneLog.Core.Tests;

public sealed class PhotoFileStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"lanelog-photos-{Guid.NewGuid():N}");

    public PhotoFileStoreTests()
    {
        Directory.CreateDirectory(directory);
    }

    [Fact]
    public async Task CopyUsesUniqueAppOwnedPathsAndLeavesOriginalAlone()
    {
        var originalPath = Path.Combine(directory, "score.png");
        var contents = new byte[] { 1, 2, 3, 4 };
        await File.WriteAllBytesAsync(originalPath, contents);
        var photos = new PhotoFileStore(directory);

        string first;
        string second;
        await using (var original = File.OpenRead(originalPath))
        {
            first = await photos.CopyAsync(original, "score.png");
        }

        await using (var original = File.OpenRead(originalPath))
        {
            second = await photos.CopyAsync(original, "score.png");
        }

        Assert.False(Path.IsPathRooted(first));
        Assert.Equal("photos", Path.GetDirectoryName(first));
        Assert.NotEqual(first, second);
        Assert.Equal(contents, await File.ReadAllBytesAsync(photos.GetFullPath(first)));
        Assert.Equal(contents, await File.ReadAllBytesAsync(photos.GetFullPath(second)));

        photos.Delete(first);
        Assert.False(File.Exists(photos.GetFullPath(first)));
        Assert.True(File.Exists(photos.GetFullPath(second)));
        Assert.Equal(contents, await File.ReadAllBytesAsync(originalPath));
    }

    [Fact]
    public async Task CopyDoesNotUseAnUntrustedFileNameAsAPath()
    {
        var photos = new PhotoFileStore(directory);
        await using var source = new MemoryStream(new byte[] { 7, 8, 9 });

        var relativePath = await photos.CopyAsync(source, Path.Combine("..", "outside.exe"), "image/png");

        Assert.Equal(".png", Path.GetExtension(relativePath));
        Assert.Equal("photos", Path.GetDirectoryName(relativePath));
        Assert.True(File.Exists(photos.GetFullPath(relativePath)));
    }

    [Fact]
    public void InvalidPathsCannotReadOrDeleteOutsidePhotoDirectory()
    {
        var photos = new PhotoFileStore(directory);
        var originalPath = Path.Combine(directory, "original.jpg");
        File.WriteAllText(originalPath, "original");
        var invalidPaths = new[]
        {
            originalPath,
            Path.Combine("photos", "..", "original.jpg"),
            Path.Combine("photos", "original.jpg"),
            Path.Combine("elsewhere", $"{Guid.NewGuid():N}.jpg")
        };

        foreach (var path in invalidPaths)
        {
            Assert.Throws<InvalidDataException>(() => photos.GetFullPath(path));
            Assert.Throws<InvalidDataException>(() => photos.Delete(path));
        }

        Assert.Equal("original", File.ReadAllText(originalPath));
    }

    [Fact]
    public async Task FailedCopyRemovesItsPartialFile()
    {
        var photos = new PhotoFileStore(directory);
        await using var source = new FailingReadStream();

        await Assert.ThrowsAsync<IOException>(() => photos.CopyAsync(source, "score.jpg"));

        Assert.Empty(Directory.GetFiles(Path.Combine(directory, "photos")));
    }

    [Fact]
    public async Task PhotoReferencesSurviveReopenAndSharedPhotosStayInUse()
    {
        var photos = new PhotoFileStore(directory);
        await using var firstSource = new MemoryStream(new byte[] { 1, 2, 3 });
        await using var replacementSource = new MemoryStream(new byte[] { 4, 5, 6 });
        var firstPhoto = await photos.CopyAsync(firstSource, "first.jpg");
        var replacement = await photos.CopyAsync(replacementSource, "replacement.jpg");
        var databasePath = Path.Combine(directory, "games.db3");
        var database = new GameDatabase(databasePath);

        try
        {
            var firstGame = new BowlingGame { TotalScore = 100, PhotoRelativePath = firstPhoto };
            var secondGame = new BowlingGame { TotalScore = 200, PhotoRelativePath = firstPhoto };
            await database.SaveGameAsync(firstGame);
            await database.SaveGameAsync(secondGame);

            firstGame.PhotoRelativePath = replacement;
            Assert.True(File.Exists(photos.GetFullPath(firstPhoto)));
            await database.SaveGameAsync(firstGame);
            Assert.True(await database.IsPhotoInUseAsync(firstPhoto));
            Assert.True(await database.IsPhotoInUseAsync(replacement));

            secondGame.PhotoRelativePath = null;
            await database.SaveGameAsync(secondGame);
            Assert.False(await database.IsPhotoInUseAsync(firstPhoto));
            photos.Delete(firstPhoto);
            Assert.True(File.Exists(photos.GetFullPath(replacement)));

            await database.CloseAsync();
            database = new GameDatabase(databasePath);
            var reopened = await database.GetGameAsync(firstGame.GameId);
            Assert.NotNull(reopened);
            Assert.Equal(replacement, reopened.PhotoRelativePath);
            Assert.NotNull(reopened.PhotoRelativePath);
            Assert.Equal(new byte[] { 4, 5, 6 }, await File.ReadAllBytesAsync(photos.GetFullPath(reopened.PhotoRelativePath)));
            Assert.True(await database.IsPhotoInUseAsync(replacement));
        }
        finally
        {
            await database.CloseAsync();
        }
    }

    public void Dispose()
    {
        var photoDirectory = Path.Combine(directory, "photos");
        if (Directory.Exists(photoDirectory))
        {
            foreach (var file in Directory.GetFiles(photoDirectory))
            {
                File.Delete(file);
            }

            Directory.Delete(photoDirectory);
        }

        foreach (var file in Directory.GetFiles(directory))
        {
            File.Delete(file);
        }

        Directory.Delete(directory);
    }

    private sealed class FailingReadStream : Stream
    {
        private bool hasRead;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (hasRead)
            {
                return ValueTask.FromException<int>(new IOException("Simulated read failure."));
            }

            hasRead = true;
            buffer.Span[0] = 42;
            return ValueTask.FromResult(1);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
