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

/// <summary>Verifies automatic image preparation with mock files and correctly labelled encoded bytes.</summary>
public class AssetImageExtensionsTest
{
    /// <summary>
    /// Unchanged files preserve their bytes and detected format, independently of the extension.
    /// </summary>
    /// <param name="format">The format encoded in the source fixture.</param>
    /// <param name="expectedFormat">The detected format expected in the registered asset.</param>
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, "png")]
    public void AddImageAssetWithAutoAdjustment_ValidImage_PreservesBytesAndFormat(
        SKEncodedImageFormat format, string expectedFormat)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.image");
        var original = CreateImage(format, 280, 280);
        fs.AddFile(path.ToString(), new MockFileData(original));
        var state = new AssetState();

        var returned = state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
            token: TestContext.Current.CancellationToken, fileSystem: fs);

        Assert.Same(state, returned);
        Assert.Equal("image-id", state["photo"]);
        var asset = GetImageAsset(state);
        Assert.IsType<FilePathAsset>(asset);
        Assert.Equal("image-id", asset.AssetName);
        Assert.Equal(expectedFormat, asset.Format);
        Assert.Equal(original, ReadBytes(asset));
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>
    /// Both resizing directions produce PNG-labelled PNG data using only the injected file system.
    /// </summary>
    /// <param name="width">The source width triggering resizing.</param>
    /// <param name="height">The source height triggering resizing.</param>
    /// <param name="expectedWidth">The expected resized width.</param>
    /// <param name="expectedHeight">The expected resized height.</param>
    [Theory]
    [InlineData(100, 200, 280, 560)]
    [InlineData(2000, 1000, 1568, 784)]
    public void AddImageAssetWithAutoAdjustment_ResizesJpeg_RegistersPng(
        int width, int height, int expectedWidth, int expectedHeight)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.jpg");
        var original = CreateImage(SKEncodedImageFormat.Jpeg, width, height);
        fs.AddFile(path.ToString(), new MockFileData(original));
        var state = new AssetState();

        state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
            token: TestContext.Current.CancellationToken, fileSystem: fs);

        var asset = GetImageAsset(state);
        Assert.IsType<ByteArrayAsset>(asset);
        Assert.EndsWith("source", state["photo"]);
        Assert.EndsWith("source", asset.AssetName);
        Assert.Equal("source.png", asset.FileName);
        Assert.Equal("png", asset.Format);
        var bytes = ReadBytes(asset);
        Assert.Equal(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}, bytes[..8]);
        using var image = SKBitmap.Decode(bytes);
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>
    /// Unsupported or opposing errors fail without registering an asset.
    /// </summary>
    /// <param name="opposingDimensions">Whether to encode an image with conflicting size violations instead of invalid bytes.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddImageAssetWithAutoAdjustment_InvalidImage_DoesNotRegister(bool opposingDimensions)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.png");
        var bytes = opposingDimensions ? CreateImage(SKEncodedImageFormat.Png, 100, 2000) : new byte[] {1, 2, 3};
        fs.AddFile(path.ToString(), new MockFileData(bytes));
        var state = new AssetState();

        Assert.Throws<InvalidOperationException>(() =>
            state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
                token: TestContext.Current.CancellationToken, fileSystem: fs));

        Assert.Empty(state);
        Assert.Empty(new Request(state).Assets);
    }

    /// <summary>Cancellation does not register a partially prepared image.</summary>
    [Fact]
    public void AddImageAssetWithAutoAdjustment_Cancelled_DoesNotRegister()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.png");
        fs.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png, 280, 280)));
        var state = new AssetState();

        Assert.ThrowsAny<OperationCanceledException>(() => state.AddImageAssetWithAutoAdjustment(
            "photo", "image-id", path, token: new CancellationToken(true), fileSystem: fs));

        Assert.Empty(state);
        Assert.Empty(new Request(state).Assets);
    }

    /// <summary>
    /// Reads the registered stream image through the same request construction used by callers.
    /// </summary>
    /// <param name="state">The state containing the prepared image.</param>
    /// <returns>The image's multipart stream provider.</returns>
    private static IImageStreamAsset GetImageAsset(AssetState state)
    {
        var asset = Assert.Single(new Request(state).Assets);
        return asset switch
        {
            IImageStreamAsset image => image,
            _ => throw new InvalidOperationException("Expected a stream image asset.")
        };
    }

    /// <summary>
    /// Reads prepared image bytes without assuming a file-backed or byte-backed implementation.
    /// </summary>
    /// <param name="asset">The prepared image whose stream is read and disposed.</param>
    /// <returns>The encoded image bytes.</returns>
    private static byte[] ReadBytes(IImageStreamAsset asset)
    {
        using var stream = asset.OpenRead();
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>Encodes a source fixture in memory without touching the physical file system.</summary>
    private static byte[] CreateImage(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }
}
