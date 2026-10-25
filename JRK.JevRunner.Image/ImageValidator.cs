using System.Buffers.Binary;
using System.IO.Abstractions;
using SkiaSharp;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Wraps an image validation result.</summary>
public union ImageValidation(ImageValidationResult);


/// <summary>Checks image files against TypeSafe's per-image upload requirements without modifying them.</summary>
/// <remarks>Inspects actual contents independently of the filename. Request body size, processed-image
/// size, expanded references, and model context limits require separate request-level checks.
/// Pixel orientation cannot be inferred visually; non-default EXIF orientation is rejected instead.</remarks>
public sealed class ImageValidator
{
    /// <summary>Inclusive minimum width and height in pixels.</summary>
    public const int MinimumDimension = 280;

    /// <summary>Inclusive maximum width and height in pixels.</summary>
    public const int MaximumDimension = 1568;

    /// <summary>Inclusive maximum encoded file size before base64 conversion: 16 MiB.</summary>
    public const long MaximumFileSizeBytes = 16L * 1024 * 1024;

    private readonly IFileSystem _fileSystem;

    /// <summary>Creates a validator using the local file system.</summary>
    public ImageValidator() : this(new FileSystem())
    {
    }

    /// <summary>Creates a validator using injectable file access.</summary>
    public ImageValidator(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }
   
    /// <summary>Detects PNG/JPEG contents, checks size and dimensions, and decodes the image.</summary>
    /// <returns>A valid case containing detected properties and encoded bytes, or an invalid case
    /// containing validation failures. Invalid contents do not throw.</returns>
    /// <remarks>Animated PNGs, including single-frame APNGs, are rejected. Progressive JPEG is not
    /// rejected: baseline JPEG is a recommendation, not a hard format requirement.
    /// A suitable filename extension is still needed when using AddImageAsset.</remarks>
    /// <exception cref="IOException">The file cannot be opened or read.</exception>
    /// <exception cref="UnauthorizedAccessException">File access is denied.</exception>
    public ImageValidationResult Validate(AbsolutePath filePath)
    {
        // Never retain a previous image's suggestion when the next validation fails early or throws.
        
        using var input = filePath.OpenRead(_fileSystem);
        var length = input.Length;
        if (length > MaximumFileSizeBytes)
            return new InvalidImageResult([new ImageSizeExceededError(MaximumFileSizeBytes)]);

        var bytes = new byte[checked((int) length)];
        input.ReadExactly(bytes);
        using var data = SKData.CreateCopy(bytes);
        using var codec = SKCodec.Create(data);
        if (codec is null)
            return new InvalidImageResult([new NotASupportedImageError()]);

        var format = codec.EncodedFormat switch
        {
            SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Jpeg => "jpg",
            _ => null
        };
        var info = codec.Info;
        if (format is null)
            return new InvalidImageResult([new NotASupportedImageError()]);

        var errors = new List<ImageValidationError>();

        
        errors.AddRange(ImageDimensionError.GetErrors(info, MinimumDimension, MaximumDimension));
        
        // Per-axis bounds also enforce the documented total-pixel bounds.
        if (format == "png")
            ValidatePngChunks(bytes, errors);
        if (codec.EncodedOrigin != SKEncodedOrigin.TopLeft)
            errors.Add("Apply EXIF orientation to the pixels and reset orientation before uploading.");

        // Do not allocate a decoded bitmap for oversized dimensions or known-invalid files.
        if (errors.Count == 0)
        {
            using var bitmap = new SKBitmap(info.Width, info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
                errors.Add("Image data is damaged, incomplete, or cannot be fully decoded.");
        }
        
        return errors.Count > 0 ? new InvalidImageResult(errors.ToArray()) : new ValidImageResult(format, info.Width, info.Height, length,bytes);
    }

    /// <summary>Walks PNG chunks to reject animation even when a decoder exposes only its default image.</summary>
    private static void ValidatePngChunks(ReadOnlySpan<byte> bytes, List<ImageValidationError> errors)
    {
        var offset = 8; // PNG signature; format identification has already succeeded.
        while (bytes.Length - offset >= 12)
        {
            var size = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
            if (size > (uint) (bytes.Length - offset - 12))
                break;
            var type = bytes.Slice(offset + 4, 4);
            if (type.SequenceEqual("acTL"u8))
            {
                errors.Add("Animated PNG (APNG) images are not supported; export a still frame.");
                return;
            }

            if (type.SequenceEqual("IEND"u8) && size == 0)
                return;
            offset += checked((int) size + 12);
        }

        errors.Add("PNG data is incomplete or contains an invalid chunk length.");
    }
}