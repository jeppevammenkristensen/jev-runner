using System.IO.Abstractions;
using JRK.JevRunner.Responses;
using SkiaSharp;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Supplies either one bounding-box answer or a collection of answers for image annotation.</summary>
/// <remarks>Collections must support repeated enumeration for validation and drawing.</remarks>
public union AppliedBox(BoxAnswer, IEnumerable<BoxAnswer>)
{
    /// <summary>Exposes either union case as an enumerable of bounding-box answers.</summary>
    public IEnumerable<BoxAnswer> GetAll()
    {
        return this switch
        {
            BoxAnswer a => [a],
            IEnumerable<BoxAnswer> a => a
        };
    }
}

/// <summary>Draws Jev bounding boxes on images and returns the annotated images as PNG bytes.</summary>
public sealed class BoxRenderer
{
    private readonly IFileSystem _fileSystem;

    /// <summary>Creates a renderer using the local file system.</summary>
    public BoxRenderer() : this(new FileSystem())
    {
    }

    /// <summary>Creates a renderer using the supplied file system to read source images.</summary>
    /// <param name="fileSystem">The file system providing the source image streams.</param>
    public BoxRenderer(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    /// <summary>Draws outlines around the supplied bounding boxes without modifying the source file.</summary>
    /// <param name="filePath">The image whose pixel layout matches the image submitted to Jev.</param>
    /// <param name="boxAnswer">One normalized box answer or a repeatable collection of answers returned by Jev.</param>
    /// <param name="color">The outline color, or null to use red.</param>
    /// <param name="strokeWidth">The positive outline width in pixels.</param>
    /// <returns>A PNG-encoded image with the original dimensions and the box outlines.</returns>
    /// <remarks>No resizing or EXIF orientation correction is applied. Confidence does not affect rendering.
    /// The stroke is centered on the box edges and clipped at the image boundary.
    /// All boxes share the same style and are enumerated for validation before drawing.
    /// An empty collection produces a PNG without outlines.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Edges are not finite fractions between zero and one,
    /// are not ordered, or the stroke width is not positive and finite.</exception>
    /// <exception cref="InvalidDataException">The source cannot be decoded as an image.</exception>
    public byte[] DrawBox(AbsolutePath filePath, AppliedBox boxAnswer, SKColor? color = null, float strokeWidth = 3f)
    {
#pragma warning disable CA2264
        ArgumentNullException.ThrowIfNull(boxAnswer);
#pragma warning restore CA2264

        ValidateBox(boxAnswer);
        if (!float.IsFinite(strokeWidth) || strokeWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(strokeWidth), "Stroke width must be positive and finite.");

        using var input = filePath.OpenRead(_fileSystem);
        using var source = SKBitmap.Decode(input)
            ?? throw new InvalidDataException("The source file could not be decoded as an image.");
        using var bitmap = new SKBitmap(source.Width, source.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            using var paint = new SKPaint
            {
                Color = color ?? SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                IsAntialias = true
            };

            foreach (var answer in boxAnswer.GetAll())
            {
                canvas.DrawRect(new SKRect(
                    (float)(answer.Left * source.Width),
                    (float)(answer.Top * source.Height),
                    (float)(answer.Right * source.Width),
                    (float)(answer.Bottom * source.Height)), paint);
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("The annotated image could not be encoded as PNG.");
        return data.ToArray();
    }

    /// <summary>Rejects non-normalized or empty boxes before accessing the source image.</summary>
    private static void ValidateBox(AppliedBox boxAnswer)
    {
        foreach (var box in boxAnswer.GetAll())
        {
            if (!IsFraction(box.Top) || !IsFraction(box.Bottom) ||
                !IsFraction(box.Left) || !IsFraction(box.Right) ||
                box.Top >= box.Bottom || box.Left >= box.Right)
            {
                throw new ArgumentOutOfRangeException(nameof(box),
                    "Box edges must be finite values between zero and one, with top < bottom and left < right.");
            }
        }
    }

    /// <summary>Checks whether an edge is a finite normalized image coordinate.</summary>
    private static bool IsFraction(double value) => double.IsFinite(value) && value is >= 0 and <= 1;
}
