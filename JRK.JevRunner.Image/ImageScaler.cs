using System.IO.Abstractions;
using SkiaSharp;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Scales images proportionally in one direction and returns PNG bytes without changing source files.</summary>
/// <remarks>These operations do not validate API requirements, crop, pad, or apply EXIF orientation.
/// Extreme aspect ratios may leave the other dimension outside the API limits.</remarks>
public sealed class ImageScaler
{
    private readonly IFileSystem _fileSystem;

    /// <summary>Creates a scaler using the local file system.</summary>
    public ImageScaler() : this(new FileSystem())
    {
    }

    /// <summary>Creates a scaler using injectable file access.</summary>
    public ImageScaler(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <summary>Enlarges the image until its shorter side reaches the minimum, using the same scale for both axes.</summary>
    /// <param name="filePath">The source image, which is never overwritten.</param>
    /// <param name="minimumDimension">The desired minimum side length; defaults to 280 pixels.</param>
    /// <returns>PNG bytes. Images already meeting the minimum retain their dimensions.</returns>
    /// <remarks>The other side is rounded up to a whole pixel. An oversized result is returned without
    /// an API-validation exception. This method never downscales.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The target side length is not positive.</exception>
    /// <exception cref="InvalidDataException">The source cannot be decoded as an image.</exception>
    public byte[] Upscale(AbsolutePath filePath, int minimumDimension = ImageValidator.MinimumDimension) =>
        Scale(filePath, minimumDimension, upscale: true);

    /// <summary>Shrinks the image until its longer side reaches the maximum, using the same scale for both axes.</summary>
    /// <param name="filePath">The source image, which is never overwritten.</param>
    /// <param name="maximumDimension">The desired maximum side length; defaults to 1568 pixels.</param>
    /// <returns>PNG bytes. Images already meeting the maximum retain their dimensions.</returns>
    /// <remarks>The other side is rounded down to a whole pixel, with a minimum of one pixel.
    /// An undersized result is returned without an API-validation exception. This method never upscales.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The target side length is not positive.</exception>
    /// <exception cref="InvalidDataException">The source cannot be decoded as an image.</exception>
    public byte[] Downscale(AbsolutePath filePath, int maximumDimension = ImageValidator.MaximumDimension) =>
        Scale(filePath, maximumDimension, upscale: false);

    /// <summary>Selects the constraining axis, computes proportional integer dimensions, and encodes the result.</summary>
    private byte[] Scale(AbsolutePath filePath, int target, bool upscale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(target);
        using var input = filePath.OpenRead(_fileSystem);
        using var source = SKBitmap.Decode(input)
                           ?? throw new InvalidDataException("The source file could not be decoded as an image.");
        var side = upscale ? Math.Min(source.Width, source.Height) : Math.Max(source.Width, source.Height);
        var needsScaling = upscale ? side < target : side > target;
        if (!needsScaling)
            return Encode(source);

        var width = ScaleDimension(source.Width, target, side, upscale);
        var height = ScaleDimension(source.Height, target, side, upscale);
        using var resized = source.Resize(
                                new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul),
                                new SKSamplingOptions(SKCubicResampler.Mitchell))
                            ?? throw new InvalidOperationException("The image could not be resized.");
        return Encode(resized);
    }

    /// <summary>Uses exact rational arithmetic to round in the scaling direction without floating-point drift.</summary>
    private static int ScaleDimension(int dimension, int target, int side, bool upscale)
    {
        var numerator = (long) dimension * target;
        var rounded = upscale ? (numerator + side - 1) / side : numerator / side;
        return checked((int) Math.Max(1, rounded));
    }

    /// <summary>Encodes an image as PNG, including when no resizing was needed.</summary>
    private static byte[] Encode(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100)
                            ?? throw new InvalidOperationException("The image could not be encoded as PNG.");
        return encoded.ToArray();
    }
}