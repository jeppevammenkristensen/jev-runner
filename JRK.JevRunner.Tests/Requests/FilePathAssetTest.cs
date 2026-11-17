using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using JRK.JevRunner.Requests;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

/// <summary>Verifies file-backed image defaults and fluent stream configuration.</summary>
public class FilePathAssetTest
{
    [Fact]
    public void Defaults_OpenExistingFileForReadingWithoutTruncation()
    {
        var fileSystem = new MockFileSystem();
        var path = new AbsolutePath(@"C:\images\image.png");
        fileSystem.AddFile(path.Value, new MockFileData("image contents"));
        var asset = new FilePathAsset(path, fileSystem);

        Assert.Equal(path, asset.Path);
        Assert.Equal("image", asset.Name);
        Assert.Equal("png", asset.Format);
        Assert.Equal("img-image", asset.PartName);
        Assert.Equal(FileMode.Open, asset.Mode);
        Assert.Equal(FileAccess.Read, asset.FileAccess);
        Assert.Equal(FileShare.Read, asset.FileShare);
        using var stream = asset.OpenRead();
        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        using var reader = new StreamReader(stream);
        Assert.Equal("image contents", reader.ReadToEnd());
    }

    [Fact]
    public void WithMethods_ReturnSameAssetAndConfigureStreamOpening()
    {
        var fileSystem = new MockFileSystem();
        var path = new AbsolutePath(@"C:\images\image.png");
        fileSystem.AddFile(path.Value, new MockFileData("image contents"));
        var asset = new FilePathAsset(path, fileSystem);

        Assert.Same(asset, asset.WithMode(FileMode.Truncate));
        Assert.Same(asset, asset.WithFileAccess(FileAccess.ReadWrite));
        Assert.Same(asset, asset.WithFileShare(FileShare.None));
        Assert.Equal(FileMode.Truncate, asset.Mode);
        Assert.Equal(FileAccess.ReadWrite, asset.FileAccess);
        Assert.Equal(FileShare.None, asset.FileShare);

        using var stream = asset.OpenRead();
        Assert.True(stream.CanRead);
        Assert.True(stream.CanWrite);
        Assert.Equal(0L, stream.Length);
    }

    [Fact]
    public void Defaults_DoNotCreateMissingFile()
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\missing.png"), new MockFileSystem());

        Assert.Throws<FileNotFoundException>(() => asset.OpenRead());
    }

    /// <summary>Repeated openings use independent streams and remain usable after a previous stream is disposed.</summary>
    [Fact]
    public void OpenRead_ReturnsIndependentStreamsAndSupportsReopeningAfterDisposal()
    {
        var fileSystem = new MockFileSystem();
        var path = new AbsolutePath(@"C:\images\photo.jpg");
        fileSystem.AddFile(path.Value, new MockFileData(new byte[] {1, 2, 3}));
        var asset = new FilePathAsset(path, fileSystem);

        using var first = asset.OpenRead();
        using var second = asset.OpenRead();
        Assert.NotSame(first, second);
        Assert.Equal(1, first.ReadByte());
        Assert.Equal(0L, second.Position);
        first.Dispose();
        Assert.Equal(1, second.ReadByte());
        using var reopened = asset.OpenRead();
        Assert.Equal(1, reopened.ReadByte());
    }

    /// <summary>Both convenience constructors preserve file-derived metadata without opening the file.</summary>
    [Fact]
    public void Constructors_DefaultFileSystem_PreserveMetadataWithoutOpeningFile()
    {
        var path = new AbsolutePath(@"C:\images\photo.jpg");
        var defaultAsset = new FilePathAsset(path);
        var nullFileSystemAsset = new FilePathAsset(path, null);

        foreach (var asset in new[] {defaultAsset, nullFileSystemAsset})
        {
            Assert.Equal(path, asset.Path);
            Assert.Equal("photo", asset.Name);
            Assert.Equal("jpg", asset.Format);
            Assert.Equal("img-photo", asset.PartName);
        }
    }

    /// <summary>Overriding the multipart name preserves file-derived asset metadata.</summary>
    [Fact]
    public void WithPathName_ReturnsSameAssetAndOverridesOnlyPartName()
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\images\photo.jpg"), new MockFileSystem());

        Assert.Same(asset, asset.WithPathName("custom-part"));
        Assert.Equal("custom-part", asset.PartName);
        Assert.Equal("photo", asset.Name);
        Assert.Equal("jpg", asset.Format);
    }

    /// <summary>Invalid multipart names are rejected without changing the configured name.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithPathName_RejectsNullOrEmpty(string? partName)
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\images\photo.jpg"), new MockFileSystem());

        Assert.ThrowsAny<ArgumentException>(() => asset.WithPathName(partName!));
        Assert.Equal("img-photo", asset.PartName);
    }
}