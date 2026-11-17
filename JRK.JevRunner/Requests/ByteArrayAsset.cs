namespace JRK.JevRunner.Requests;

/// <summary>
/// Provides image bytes as fresh read-only streams for multipart uploads.
/// </summary>
/// <remarks>The supplied array is retained without copying. Do not modify it while the asset is in use.</remarks>
public sealed class ByteArrayAsset : IImageStreamAsset
{
    /// <summary>
    /// The image bytes retained without copying and shared by the streams opened by this asset.
    /// </summary>
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates an image asset backed by the supplied byte array without copying its contents.
    /// </summary>
    /// <param name="bytes">The encoded image file contents. Do not modify the array while the asset is in use.</param>
    /// <param name="name">The asset reference used in the assets map and request content. Must not be null, empty, or whitespace.</param>
    /// <param name="fileName">The file name reported for the uploaded image. Must not be null, empty, or whitespace.</param>
    /// <param name="assetName">An asset name that is validated but not stored or used. Must not be null, empty, or whitespace.</param>
    /// <param name="format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot. Must not be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException">A string argument is empty or consists only of whitespace.</exception>
    public ByteArrayAsset(byte[] bytes, string name, string fileName, string assetName, string format)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        _bytes = bytes;
        Name = name;
        FileName = fileName;
        AssetName = assetName;
        Format = format;
    }

    /// <summary>
    /// Gets or sets the name used to reference this asset from request state.
    /// </summary>
    public string AssetName { get; set; }

    /// <summary>
    /// Gets the asset reference used in the assets map and request content.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the file name reported for the uploaded image.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the supplied image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Opens a fresh read-only stream positioned at the start of the retained image bytes.
    /// </summary>
    /// <remarks>Each stream has its own position but shares the underlying byte array.</remarks>
    /// <returns>A fresh stream that the caller must dispose after use.</returns>
    public Stream OpenRead() => new MemoryStream(_bytes, writable: false);
}
