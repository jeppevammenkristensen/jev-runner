using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using System.Threading.Tasks;
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
    /// <summary>Unchanged files preserve their bytes and detected format, independently of the extension.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, "png")]
    public async Task AddImageAssetWithAutoAdjustment_ValidImage_PreservesBytesAndFormat(
        SKEncodedImageFormat format, string expectedFormat)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.image");
        var original = CreateImage(format, 280, 280);
        fs.AddFile(path.ToString(), new MockFileData(original));
        var state = new AssetState();

        var returned = await state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
            token: TestContext.Current.CancellationToken, fileSystem: fs);

        Assert.Same(state, returned);
        Assert.Equal("image-id", state["photo"]);
        var asset = GetImageAsset(state);
        Assert.Equal("image-id", asset.Name);
        Assert.Equal(expectedFormat, asset.Format);
        Assert.Equal(original, Convert.FromBase64String(asset.Base64));
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>Both resizing directions produce PNG-labelled PNG data using only the injected file system.</summary>
    [Theory]
    [InlineData(100, 200, 280, 560)]
    [InlineData(2000, 1000, 1568, 784)]
    public async Task AddImageAssetWithAutoAdjustment_ResizesJpeg_RegistersPng(
        int width, int height, int expectedWidth, int expectedHeight)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.jpg");
        var original = CreateImage(SKEncodedImageFormat.Jpeg, width, height);
        fs.AddFile(path.ToString(), new MockFileData(original));
        var state = new AssetState();

        await state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
            token: TestContext.Current.CancellationToken, fileSystem: fs);

        var asset = GetImageAsset(state);
        Assert.Equal("png", asset.Format);
        var bytes = Convert.FromBase64String(asset.Base64);
        Assert.Equal(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}, bytes[..8]);
        using var image = SKBitmap.Decode(bytes);
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>Unsupported or opposing errors fail without registering an asset.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddImageAssetWithAutoAdjustment_InvalidImage_DoesNotRegister(bool opposingDimensions)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.png");
        var bytes = opposingDimensions ? CreateImage(SKEncodedImageFormat.Png, 100, 2000) : new byte[] {1, 2, 3};
        fs.AddFile(path.ToString(), new MockFileData(bytes));
        var state = new AssetState();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            state.AddImageAssetWithAutoAdjustment("photo", "image-id", path,
                token: TestContext.Current.CancellationToken, fileSystem: fs));

        Assert.Empty(state);
        Assert.Empty(new Request(state).Assets);
    }

    /// <summary>Cancellation does not register a partially prepared image.</summary>
    [Fact]
    public async Task AddImageAssetWithAutoAdjustment_Cancelled_DoesNotRegister()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/mock-images/source.png");
        fs.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png, 280, 280)));
        var state = new AssetState();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => state.AddImageAssetWithAutoAdjustment(
            "photo", "image-id", path, token: new CancellationToken(true), fileSystem: fs));

        Assert.Empty(state);
        Assert.Empty(new Request(state).Assets);
    }

    /// <summary>Reads the registered image through the same request construction used by callers.</summary>
    private static ImageAsset GetImageAsset(AssetState state)
    {
        var asset = Assert.Single(new Request(state).Assets);
        Assert.True(asset is ImageAsset);
        return asset switch
        {
            ImageAsset image => image,
            _ => throw new InvalidOperationException("Expected an image asset.")
        };
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
