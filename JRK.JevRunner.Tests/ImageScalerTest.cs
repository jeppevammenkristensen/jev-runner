using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JRK.JevRunner.Image;
using SkiaSharp;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies proportional, directional scaling using mock files and real encoded images.</summary>
public class ImageScalerTest
{
    /// <summary>The shorter side drives upscaling; already-large images retain their dimensions.</summary>
    [Theory]
    [InlineData(100, 200, 280, 560)]
    [InlineData(200, 100, 560, 280)]
    [InlineData(100, 100, 280, 280)]
    [InlineData(101, 200, 280, 555)]
    [InlineData(280, 560, 280, 560)]
    [InlineData(400, 600, 400, 600)]
    [InlineData(200, 1600, 280, 2240)]
    public void Upscale_UsesShorterSide(int width, int height, int expectedWidth, int expectedHeight) =>
        AssertScale(width, height, expectedWidth, expectedHeight, upscale: true);

    /// <summary>The longer side drives downscaling, even if the shorter side ends up below the API minimum.</summary>
    [Theory]
    [InlineData(2000, 1000, 1568, 784)]
    [InlineData(1000, 2000, 784, 1568)]
    [InlineData(2000, 2000, 1568, 1568)]
    [InlineData(2000, 333, 1568, 261)]
    [InlineData(1568, 280, 1568, 280)]
    [InlineData(100, 200, 100, 200)]
    [InlineData(2000, 200, 1568, 156)]
    [InlineData(2000, 1, 1568, 1)]
    public void Downscale_UsesLongerSide(int width, int height, int expectedWidth, int expectedHeight) =>
        AssertScale(width, height, expectedWidth, expectedHeight, upscale: false);

    /// <summary>Callers can choose a different bound without changing the proportional rule.</summary>
    [Theory]
    [InlineData(true, 400, 400, 800)]
    [InlineData(false, 100, 50, 100)]
    public void Scale_CustomBound_IsApplied(bool upscale, int target, int expectedWidth, int expectedHeight)
    {
        var (scaler, path, _) = CreateScaler(200, 400, SKEncodedImageFormat.Jpeg);
        var bytes = upscale ? scaler.Upscale(path, target) : scaler.Downscale(path, target);
        using var bitmap = SKBitmap.Decode(bytes);
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);
    }

    /// <summary>Invalid targets are rejected before attempting file access.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Scale_InvalidBound_Throws(int target)
    {
        var scaler = new ImageScaler(new MockFileSystem());
        var path = AbsolutePath.Create("C:/images/missing.png");
        Assert.Throws<ArgumentOutOfRangeException>(() => scaler.Upscale(path, target));
        Assert.Throws<ArgumentOutOfRangeException>(() => scaler.Downscale(path, target));
    }

    /// <summary>Unreadable content is distinguished from a still-out-of-bounds scaling result.</summary>
    [Fact]
    public void Scale_InvalidImage_Throws()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/invalid.png");
        fs.AddFile(path.ToString(), new MockFileData("not an image"));
        var scaler = new ImageScaler(fs);
        Assert.Throws<InvalidDataException>(() => scaler.Upscale(path));
        Assert.Throws<InvalidDataException>(() => scaler.Downscale(path));
    }

    /// <summary>A file system must be supplied for injectable file access.</summary>
    [Fact]
    public void Constructor_NullFileSystem_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ImageScaler(null!));

    /// <summary>Checks PNG output dimensions and that the original file is unchanged.</summary>
    private static void AssertScale(int width, int height, int expectedWidth, int expectedHeight, bool upscale)
    {
        var (scaler, path, fs) = CreateScaler(width, height, SKEncodedImageFormat.Png);
        var original = path.ReadAllBytes(fs);
        var bytes = upscale ? scaler.Upscale(path) : scaler.Downscale(path);
        using var bitmap = SKBitmap.Decode(bytes);
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);
        Assert.Equal(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}, bytes[..8]);
        Assert.Equal(original, path.ReadAllBytes(fs));
        Assert.Single(fs.AllFiles);
    }

    /// <summary>Creates a source image in memory and exposes it through the injectable file system.</summary>
    private static (ImageScaler Scaler, AbsolutePath Path, MockFileSystem FileSystem) CreateScaler(
        int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.image");
        fs.AddFile(path.ToString(), new MockFileData(data.ToArray()));
        return (new ImageScaler(fs), path, fs);
    }
}