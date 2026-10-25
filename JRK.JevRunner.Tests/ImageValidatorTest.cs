using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JRK.JevRunner.Image;
using SkiaSharp;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Checks local upload requirements using in-memory files and real encoded image contents.</summary>
public class ImageValidatorTest
{
    /// <summary>Supported formats and inclusive dimension boundaries are detected independently of extensions.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Png, 280, 280, "png")]
    [InlineData(SKEncodedImageFormat.Jpeg, 1568, 1568, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, 280, 1568, "png")]
    public void Validate_ValidImage_ReturnsProperties(SKEncodedImageFormat format, int width, int height,
        string expected)
    {
        var bytes = CreateImage(format, width, height);
        var result = Validate(bytes);
        Assert.True(result.IsValid);
        var valid = GetValidResult(result);
        Assert.Equal(expected, valid.Format);
        Assert.Equal(width, valid.Width);
        Assert.Equal(height, valid.Height);
        Assert.Equal(bytes.LongLength, valid.FileSizeBytes);
        Assert.Equal(bytes, valid.Bytes);
    }

    /// <summary>Each axis is checked independently.</summary>
    [Theory]
    [InlineData(279, 280)]
    [InlineData(280, 279)]
    [InlineData(1569, 280)]
    [InlineData(280, 1569)]
    public void Validate_InvalidDimensions_ReturnsFailure(int width, int height)
    {
        var result = Validate(CreateImage(SKEncodedImageFormat.Png, width, height));
        Assert.False(result.IsValid);
        var error = Assert.Single(GetInvalidResult(result).Errors);
        Assert.True(error is ImageDimensionError);
        if (error is ImageDimensionError details)
        {
            var expectedDimension = width is < 280 or > 1568 ? ImageDimension.Width : ImageDimension.Height;
            var actual = expectedDimension == ImageDimension.Width ? width : height;
            Assert.Equal(new ImageDimensionError(expectedDimension,
                actual < 280 ? ImageDimensionViolation.ToSmall : ImageDimensionViolation.ToHigh,
                actual < 280 ? 280 : 1568, actual), details);
        }
    }

    /// <summary>An image filename cannot make invalid contents acceptable.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("not an image")]
    public void Validate_InvalidContents_ReturnsFailure(string contents) =>
        Assert.False(Validate(System.Text.Encoding.UTF8.GetBytes(contents)).IsValid);

    /// <summary>Other decodable image formats remain unsupported.</summary>
    [Fact]
    public void Validate_Webp_ReturnsFailure() =>
        Assert.False(Validate(CreateImage(SKEncodedImageFormat.Webp, 280, 280)).IsValid);

    /// <summary>Files above the byte limit are rejected before decoding.</summary>
    [Fact]
    public void Validate_OversizedFile_ReturnsFailure()
    {
        var result = Validate(new byte[checked((int) ImageValidator.MaximumFileSizeBytes + 1)]);
        Assert.Contains(GetInvalidResult(result).Errors, error => error is ImageSizeExceededError);
    }

    /// <summary>The inclusive file size boundary proceeds to content validation.</summary>
    [Fact]
    public void Validate_ExactFileSizeLimit_DoesNotReportOversize()
    {
        var result = Validate(new byte[checked((int) ImageValidator.MaximumFileSizeBytes)]);
        Assert.DoesNotContain(GetInvalidResult(result).Errors, error => error is ImageSizeExceededError);
    }

    /// <summary>A truncated PNG cannot pass solely because its dimensions can be read.</summary>
    [Fact]
    public void Validate_TruncatedPng_ReturnsFailure()
    {
        var bytes = CreateImage(SKEncodedImageFormat.Png, 280, 280);
        Assert.False(Validate(bytes[..^12]).IsValid);
    }

    /// <summary>Incomplete JPEG pixel data is not accepted as a successful decode.</summary>
    [Fact]
    public void Validate_TruncatedJpeg_ReturnsFailure()
    {
        var bytes = CreateImage(SKEncodedImageFormat.Jpeg, 280, 280);
        Assert.False(Validate(bytes[..(bytes.Length / 2)]).IsValid);
    }

    /// <summary>Files requiring EXIF rotation must have that rotation applied before upload.</summary>
    [Fact]
    public void Validate_ExifRotation_ReturnsOrientationFailure()
    {
        var jpeg = CreateImage(SKEncodedImageFormat.Jpeg, 280, 280);
        // APP1: Exif, little-endian TIFF, one SHORT orientation tag with value 6 (90 degrees).
        byte[] exif =
        [
            255, 225, 0, 34, 69, 120, 105, 102, 0, 0,
            73, 73, 42, 0, 8, 0, 0, 0, 1, 0,
            18, 1, 3, 0, 1, 0, 0, 0, 6, 0, 0, 0, 0, 0, 0, 0
        ];
        byte[] oriented = [.. jpeg[..2], .. exif, .. jpeg[2..]];
        var result = Validate(oriented);
        Assert.Contains(GetInvalidResult(result).Errors, error => error is string message &&
            message == "Apply EXIF orientation to the pixels and reset orientation before uploading.");
    }

    /// <summary>An animation control chunk is rejected even with one declared frame.</summary>
    [Fact]
    public void Validate_Apng_ReturnsFailure()
    {
        var png = CreateImage(SKEncodedImageFormat.Png, 280, 280);
        // One frame, infinite loops; the default IDAT image is also the animation frame.
        var animation = PngChunk("acTL"u8, [0, 0, 0, 1, 0, 0, 0, 0]);
        var frameData = new byte[26];
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(4), 280);
        BinaryPrimitives.WriteUInt32BigEndian(frameData.AsSpan(8), 280);
        var frame = PngChunk("fcTL"u8, frameData);
        byte[] apng = [.. png[..33], .. animation, .. frame, .. png[33..]];
        Assert.Contains(GetInvalidResult(Validate(apng)).Errors, error => error is string message &&
            message == "Animated PNG (APNG) images are not supported; export a still frame.");
    }

    /// <summary>Missing files preserve normal file-access exceptions.</summary>
    [Fact]
    public void Validate_MissingFile_Throws()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("C:/images");
        Assert.Throws<FileNotFoundException>(() =>
            new ImageValidator(fs).Validate(AbsolutePath.Create("C:/images/missing.png")));
    }

    /// <summary>Constructor rejects a missing file-system dependency.</summary>
    [Fact]
    public void Constructor_NullFileSystem_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ImageValidator(null!));

    /// <summary>Result-level suggestions group dimension violations by scaling direction.</summary>
    [Theory]
    [InlineData(280, 1568, ImageSuggestedScaling.None)]
    [InlineData(1568, 280, ImageSuggestedScaling.None)]
    [InlineData(100, 200, ImageSuggestedScaling.Upscale)]
    [InlineData(200, 100, ImageSuggestedScaling.Upscale)]
    [InlineData(2000, 1000, ImageSuggestedScaling.Downscale)]
    [InlineData(1000, 2000, ImageSuggestedScaling.Downscale)]
    [InlineData(100, 560, ImageSuggestedScaling.Upscale)]
    [InlineData(100, 561, ImageSuggestedScaling.Upscale)]
    [InlineData(560, 3136, ImageSuggestedScaling.Downscale)]
    [InlineData(559, 3136, ImageSuggestedScaling.Downscale)]
    [InlineData(200, 1600, ImageSuggestedScaling.ScalingNotPossible)]
    [InlineData(1600, 200, ImageSuggestedScaling.ScalingNotPossible)]
    public void SuggestedScaling_GroupsDimensionViolations(int width, int height, ImageSuggestedScaling expected)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fs.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png, width, height)));
        var validator = new ImageValidator(fs);
        var result = validator.Validate(path);
        if (expected == ImageSuggestedScaling.None)
            GetValidResult(result);
        else
            Assert.Equal(expected, GetInvalidResult(result).SuggestedScaling);
    }

    /// <summary>Suggestions describe dimension errors only; IsValid must still be checked independently.</summary>
    [Theory]
    [InlineData(SKEncodedImageFormat.Webp, false, ImageSuggestedScaling.None)]
    [InlineData(SKEncodedImageFormat.Png, true, ImageSuggestedScaling.Upscale)]
    public void SuggestedScaling_NonDimensionFailure_DoesNotDetermineDirection(SKEncodedImageFormat format,
        bool truncate, ImageSuggestedScaling expected)
    {
        var bytes = CreateImage(format, 100, 200);
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fs.AddFile(path.ToString(), new MockFileData(truncate ? bytes[..^12] : bytes));
        var validator = new ImageValidator(fs);
        var result = validator.Validate(path);
        Assert.False(result.IsValid);
        var invalid = GetInvalidResult(result);
        if (format == SKEncodedImageFormat.Webp)
            Assert.Contains(invalid.Errors, error => error is NotASupportedImageError);
        else
            Assert.Contains(invalid.Errors, error => error is string);
        Assert.Equal(expected, invalid.SuggestedScaling);
    }

    /// <summary>Each returned result retains its own suggestion across subsequent validations and access failures.</summary>
    [Fact]
    public void SuggestedScaling_BelongsToEachResult()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        var validator = new ImageValidator(fs);
        fs.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png, 100, 200)));
        var small = GetInvalidResult(validator.Validate(path));
        Assert.Equal(ImageSuggestedScaling.Upscale, small.SuggestedScaling);

        fs.AddFile(path.ToString(), new MockFileData(CreateImage(SKEncodedImageFormat.Png, 280, 280)));
        var valid = validator.Validate(path);
        Assert.True(valid.IsValid);
        GetValidResult(valid);

        fs.AddFile(path.ToString(), new MockFileData("invalid"));
        var invalid = validator.Validate(path);
        Assert.False(invalid.IsValid);
        var invalidDetails = GetInvalidResult(invalid);
        Assert.Equal(ImageSuggestedScaling.None, invalidDetails.SuggestedScaling);
        Assert.Throws<FileNotFoundException>(() => validator.Validate(AbsolutePath.Create("C:/images/missing.png")));
        Assert.Equal(ImageSuggestedScaling.Upscale, small.SuggestedScaling);
        GetValidResult(valid);
        Assert.Equal(ImageSuggestedScaling.None, invalidDetails.SuggestedScaling);
    }

    /// <summary>Unwraps the valid union case or fails with the unexpected result.</summary>
    private static ValidImageResult GetValidResult(ImageValidationResult result) => result switch
    {
        ValidImageResult valid => valid,
        _ => throw new Xunit.Sdk.XunitException("Expected a valid image result.")
    };

    /// <summary>Unwraps the invalid union case or fails with the unexpected result.</summary>
    private static InvalidImageResult GetInvalidResult(ImageValidationResult result) => result switch
    {
        InvalidImageResult invalid => invalid,
        _ => throw new Xunit.Sdk.XunitException("Expected an invalid image result.")
    };

    /// <summary>Validates a mock file and verifies validation leaves its contents untouched.</summary>
    private static ImageValidationResult Validate(byte[] bytes)
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/source.png");
        fs.AddFile(path.ToString(), new MockFileData(bytes));
        var result = new ImageValidator(fs).Validate(path);
        Assert.Equal(bytes, path.ReadAllBytes(fs));
        return result;
    }

    /// <summary>Produces encoded test images without disk access.</summary>
    private static byte[] CreateImage(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.White);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    /// <summary>Builds a PNG animation chunk with the required length and CRC fields.</summary>
    private static byte[] PngChunk(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var chunk = new byte[data.Length + 12];
        BinaryPrimitives.WriteUInt32BigEndian(chunk, (uint) data.Length);
        type.CopyTo(chunk.AsSpan(4));
        data.CopyTo(chunk.AsSpan(8));
        uint crc = uint.MaxValue;
        foreach (var value in chunk.AsSpan(4, data.Length + 4))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }

        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(chunk.Length - 4), ~crc);
        return chunk;
    }
}
