using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using JRK.JevRunner.Image;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using SkiaSharp;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>
/// Verifies standalone image preparation, reference preservation, and registration of prepared assets.
/// </summary>
public class ImageExtensionsTest
{
    /// <summary>
    /// Valid images retain their original bytes and detected format rather than trusting the file extension.
    /// </summary>
    /// <param name="format">The format encoded in the source fixture.</param>
    /// <param name="expectedFormat">The format reported by the prepared asset.</param>
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, "png")]
    public void CreateAssetWithAdjustments_ValidImage_PreservesBytesFormatAndReferences(
        SKEncodedImageFormat format, string expectedFormat)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.image");
        var original = CreateImage(format, 280, 280);
        fs.AddFile(path.Value, new MockFileData(original));

        StateReference stateReference = "photo";
        var result = path.CreateAssetWithAdjustments(stateReference, "image-id", TestContext.Current.CancellationToken, fs);

        Assert.Same(stateReference, result.Reference);
        Assert.Equal("image-id", result.Asset.AssetName);
        IImageStreamAsset image = result.Asset switch
        {
            IImageStreamAsset value => value,
            _ => throw new InvalidOperationException("Expected a stream-backed asset.")
        };
        Assert.IsType<FilePathAsset>(image);
        Assert.Equal(expectedFormat, image.Format);
        Assert.Equal(original, result.Asset.ReadAsBytes());
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>
    /// Both resize directions preserve explicit asset references and produce PNG without modifying the source.
    /// </summary>
    /// <param name="width">The source width that triggers the resize.</param>
    /// <param name="height">The source height that triggers the resize.</param>
    /// <param name="expectedWidth">The expected resized width.</param>
    /// <param name="expectedHeight">The expected resized height.</param>
    [Theory]
    [InlineData(100, 200, 280, 560)]
    [InlineData(2000, 1000, 1568, 784)]
    public void CreateAssetWithAdjustments_Resize_PreservesExplicitReference(
        int width, int height, int expectedWidth, int expectedHeight)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.jpg");
        var original = CreateImage(SKEncodedImageFormat.Jpeg, width, height);
        fs.AddFile(path.Value, new MockFileData(original));

        StateReference stateReference = "photo";
        var result = path.CreateAssetWithAdjustments(stateReference, "image-id", TestContext.Current.CancellationToken, fs);

        Assert.Same(stateReference, result.Reference);
        Assert.Equal("image-id", result.Asset.AssetName);
        var image = result.Asset switch
        {
            IImageStreamAsset value => Assert.IsType<ByteArrayAsset>(value),
            _ => throw new InvalidOperationException("Expected a stream-backed asset.")
        };
        Assert.Equal("source.png", image.FileName);
        Assert.Equal("png", image.Format);
        var bytes = result.Asset.ReadAsBytes();
        Assert.Equal(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}, bytes[..8]);
        using var bitmap = SKBitmap.Decode(bytes);
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>
    /// Missing or blank asset names retain generated references for unchanged and resized images.
    /// </summary>
    /// <param name="assetName">The absent or blank requested reference.</param>
    /// <param name="size">The source dimensions, covering unchanged images and both resize directions.</param>
    [Theory]
    [InlineData(null, 280)]
    [InlineData("", 100)]
    [InlineData(" ", 2000)]
    public void CreateAssetWithAdjustments_BlankName_UsesGeneratedReference(string? assetName, int size)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fs.AddFile(path.Value, new MockFileData(CreateImage(SKEncodedImageFormat.Png, size, size)));

        var result = path.CreateAssetWithAdjustments("photo", assetName, TestContext.Current.CancellationToken, fs);

        Assert.Matches("^[0-9a-f]{8}source$", result.Asset.AssetName);
        var state = new AssetState();
        Assert.Empty(state);
        Assert.Same(state, state.AddAssetStateAndReference(result));
        Assert.Equal(result.Asset.AssetName, state["photo"]);
        Assert.Equal(result.Asset, Assert.Single(new Request(state).Assets));
    }

    /// <summary>
    /// Unsupported data and conflicting dimension violations cannot produce a prepared asset.
    /// </summary>
    /// <param name="opposingDimensions">Whether to encode an image with opposing size violations instead of invalid bytes.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateAssetWithAdjustments_InvalidImage_Throws(bool opposingDimensions)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        var bytes = opposingDimensions ? CreateImage(SKEncodedImageFormat.Png, 100, 2000) : new byte[] {1, 2, 3};
        fs.AddFile(path.Value, new MockFileData(bytes));

        Assert.Throws<InvalidOperationException>(() =>
            path.CreateAssetWithAdjustments("photo", "image-id", TestContext.Current.CancellationToken, fs));

        Assert.Equal(bytes, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>
    /// Cancellation checked after validation prevents a prepared asset from being returned.
    /// </summary>
    [Fact]
    public void CreateAssetWithAdjustments_Cancelled_Throws()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fs.AddFile(path.Value, new MockFileData(CreateImage(SKEncodedImageFormat.Png, 280, 280)));

        Assert.ThrowsAny<OperationCanceledException>(() =>
            path.CreateAssetWithAdjustments("photo", "image-id", new CancellationToken(true), fs));
    }

    /// <summary>
    /// Missing source files propagate file access errors without creating files.
    /// </summary>
    [Fact]
    public void CreateAssetWithAdjustments_MissingFile_Throws()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/missing.png");

        Assert.Throws<FileNotFoundException>(() =>
            path.CreateAssetWithAdjustments("photo", "image-id", TestContext.Current.CancellationToken, fs));

        Assert.Empty(fs.AllFiles);
    }

    /// <summary>
    /// Encodes an image fixture entirely in memory for the mock file system.
    /// </summary>
    /// <param name="format">The source encoding.</param>
    /// <param name="width">The fixture width in pixels.</param>
    /// <param name="height">The fixture height in pixels.</param>
    /// <returns>The encoded fixture bytes.</returns>
    private static byte[] CreateImage(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }
}
