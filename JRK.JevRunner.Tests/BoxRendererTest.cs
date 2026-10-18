using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JRK.JevRunner.Image;
using JRK.JevRunner.Responses;
using SkiaSharp;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies bounding-box rendering and validation without reading or writing real files.</summary>
public class BoxRendererTest
{
    /// <summary>Both PNG and JPEG inputs produce a same-size PNG with correctly scaled edges and an unchanged source.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Png)]
    [InlineData(SKEncodedImageFormat.Jpeg)]
    public void DrawBox_ValidImage_DrawsNormalizedRectangleAndPreservesSource(SKEncodedImageFormat format)
    {
        var fileSystem = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.image");
        var original = CreateImage(format);
        fileSystem.AddFile(path.ToString(), new MockFileData(original));
        var renderer = new BoxRenderer(fileSystem);

        var result = renderer.DrawBox(path, new BoxAnswer(0.25, 0.75, 0.25, 0.75, 0.9));

        Assert.Equal(new byte[] {137, 80, 78, 71, 13, 10, 26, 10}, result[..8]);
        using var bitmap = SKBitmap.Decode(result);
        Assert.Equal(120, bitmap.Width);
        Assert.Equal(80, bitmap.Height);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(30, 40));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(90, 40));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(60, 20));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(60, 60));
        Assert.Equal(SKColors.White, bitmap.GetPixel(60, 40));
        Assert.Equal(SKColors.White, bitmap.GetPixel(5, 5));
        Assert.Equal(original, path.ReadAllBytes(fileSystem));
        Assert.Single(fileSystem.AllFiles);
    }

    /// <summary>Callers can customize the outline color and thickness without filling its interior.</summary>
    [Fact]
    public void DrawBox_CustomStyle_UsesRequestedColorAndWidth()
    {
        var fileSystem = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fileSystem.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png)));

        var result = new BoxRenderer(fileSystem).DrawBox(
            path, new BoxAnswer(0.25, 0.75, 0.25, 0.75, 0), SKColors.Blue, 6);

        using var bitmap = SKBitmap.Decode(result);
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(32, 40));
        Assert.Equal(SKColors.White, bitmap.GetPixel(36, 40));
        Assert.Equal(SKColors.White, bitmap.GetPixel(60, 40));
    }

    /// <summary>Boxes spanning the full image retain visible outlines along each image boundary.</summary>
    [Fact]
    public void DrawBox_FullImageBox_DrawsClippedBoundaryStroke()
    {
        var fileSystem = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fileSystem.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png)));

        var result = new BoxRenderer(fileSystem).DrawBox(path, new BoxAnswer(0, 1, 0, 1, 1), strokeWidth: 4);

        using var bitmap = SKBitmap.Decode(result);
        Assert.Equal(SKColors.Red, bitmap.GetPixel(0, 40));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(119, 40));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(60, 0));
        Assert.Equal(SKColors.Red, bitmap.GetPixel(60, 79));
    }

    /// <summary>Invalid edges are rejected before trying to open the source file.</summary>
    [Theory]
    [InlineData(-0.1, 1, 0, 1)]
    [InlineData(0, 1.1, 0, 1)]
    [InlineData(0, 1, -0.1, 1)]
    [InlineData(0, 1, 0, 1.1)]
    [InlineData(0.5, 0.5, 0, 1)]
    [InlineData(0.75, 0.25, 0, 1)]
    [InlineData(0, 1, 0.5, 0.5)]
    [InlineData(0, 1, 0.75, 0.25)]
    [InlineData(double.NaN, 1, 0, 1)]
    [InlineData(0, double.PositiveInfinity, 0, 1)]
    [InlineData(0, 1, double.NegativeInfinity, 1)]
    [InlineData(0, 1, 0, double.NaN)]
    public void DrawBox_InvalidBox_Throws(double top, double bottom, double left, double right)
    {
        var renderer = new BoxRenderer(new MockFileSystem());

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.DrawBox(
            AbsolutePath.Create("C:/images/missing.png"), new BoxAnswer(top, bottom, left, right, 0.9)));
    }

    /// <summary>Nonpositive and nonfinite stroke widths are rejected before reading an image.</summary>
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void DrawBox_InvalidStrokeWidth_Throws(float strokeWidth)
    {
        var renderer = new BoxRenderer(new MockFileSystem());

        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.DrawBox(
            AbsolutePath.Create("C:/images/missing.png"), new BoxAnswer(0, 1, 0, 1, 1), strokeWidth: strokeWidth));
    }

    /// <summary>A missing source file retains the file system's missing-file exception.</summary>
    [Fact]
    public void DrawBox_MissingImage_Throws()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(AbsolutePath.Create("C:/images").ToString());
        var renderer = new BoxRenderer(fileSystem);

        Assert.Throws<FileNotFoundException>(() => renderer.DrawBox(
            AbsolutePath.Create("C:/images/missing.png"), new BoxAnswer(0, 1, 0, 1, 1)));
    }

    /// <summary>Unreadable image contents are reported without returning an invalid PNG.</summary>
    [Fact]
    public void DrawBox_InvalidImage_Throws()
    {
        var fileSystem = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/invalid.png");
        fileSystem.AddFile(path.ToString(), new MockFileData("not an image"));

        Assert.Throws<InvalidDataException>(() => new BoxRenderer(fileSystem).DrawBox(
            path, new BoxAnswer(0, 1, 0, 1, 1)));
    }

    /// <summary>A null bounding box is rejected before reading an image.</summary>
    [Fact]
    public void DrawBox_NullAnswer_Throws()
    {
        var renderer = new BoxRenderer(new MockFileSystem());

        Assert.Throws<ArgumentNullException>(() => renderer.DrawBox(AbsolutePath.Create("C:/images/missing.png"), null!));
    }

    /// <summary>A renderer requires a file system for its image input.</summary>
    [Fact]
    public void Constructor_NullFileSystem_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new BoxRenderer(null!));

    /// <summary>Creates a white non-square source image for coordinate and output-format assertions.</summary>
    private static byte[] CreateImage(SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(120, 80);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);
        return data.ToArray();
    }
}
