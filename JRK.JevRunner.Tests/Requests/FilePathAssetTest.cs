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
        Assert.Matches("^[0-9a-f]{8}image$", asset.Name);
        Assert.Equal(asset.Name, asset.AssetName);
        Assert.Equal("image.png", asset.FileName);
        Assert.Equal("png", asset.Format);
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
            Assert.Matches("^[0-9a-f]{8}photo$", asset.Name);
            Assert.Equal(asset.Name, asset.AssetName);
            Assert.Equal("photo.jpg", asset.FileName);
            Assert.Equal("jpg", asset.Format);
        }
    }

    /// <summary>
    /// Overriding the multipart name preserves the state reference and file-derived metadata.
    /// </summary>
    [Fact]
    public void WithName_ReturnsSameAssetAndOverridesOnlyMultipartName()
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\images\photo.jpg"), new MockFileSystem());

        var assetName = asset.AssetName;
        Assert.Same(asset, asset.WithName("custom-part"));
        Assert.Equal("custom-part", asset.Name);
        Assert.Equal(assetName, asset.AssetName);
        Assert.Equal("photo.jpg", asset.FileName);
        Assert.Equal("jpg", asset.Format);
    }

    /// <summary>
    /// Changing the upload file name leaves the source path, format, and references unchanged.
    /// </summary>
    [Fact]
    public void WithFileName_ReturnsSameAssetAndOverridesOnlyUploadFileName()
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\images\photo.jpg"), new MockFileSystem());

        var name = asset.Name;
        var assetName = asset.AssetName;

        Assert.Same(asset, asset.WithFileName("upload.png"));
        Assert.Equal("upload.png", asset.FileName);
        Assert.Equal(new AbsolutePath(@"C:\images\photo.jpg"), asset.Path);
        Assert.Equal("jpg", asset.Format);
        Assert.Equal(name, asset.Name);
        Assert.Equal(assetName, asset.AssetName);
    }
    
    /// <summary>
    /// Changing the upload file name leaves the source path, format, and references unchanged.
    /// </summary>
    [Fact]
    public void WithAssetName_ReturnsSameAssetAndOverridesOnlyUploadFileName()
    {
        var asset = new FilePathAsset(new AbsolutePath(@"C:\images\photo.jpg"), new MockFileSystem());

        Assert.Same(asset, asset.WithAssetName("changed_assetname"));
        Assert.Equal("changed_assetname", asset.AssetName);
    }
}
