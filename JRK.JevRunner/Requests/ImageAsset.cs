using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using TruePath;

namespace JRK.JevRunner.Requests;

/// <summary>Provides image contents as a stream.</summary>
/// <remarks>This will use multipart upload to upload the file</remarks>
public interface IImageStreamAsset
{
    /// <summary>
    /// Gets the multipart field name reported in the file part's Content-Disposition header.
    /// </summary>
    /// <remarks>
    /// For example, when this value is <c>image-file</c>, the header can be:
    /// <code>Content-Disposition: form-data; name="image-file"; filename="photo.jpg"</code>
    /// The <c>name</c> parameter comes from this property; <c>filename</c> comes from <see cref="FileName"/>.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Gets the file name reported in the multipart file part's Content-Disposition header.
    /// </summary>
    /// <remarks>
    /// For example, when this value is <c>photo.jpg</c>, the header can be:
    /// <code>Content-Disposition: form-data; name="image-file"; filename="photo.jpg"</code>
    /// The <c>filename</c> parameter comes from this property; <c>name</c> identifies the multipart field.
    /// </remarks>
    string FileName { get; }
    
    /// <summary>
    /// This is the asset name used in Assets that can be referenced in State
    /// </summary>
    string AssetName { get; }
    
    /// <summary>
    /// Gets the image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.
    /// </summary>
    string Format { get; }
    
    /// <summary>Opens a readable stream containing the image data.</summary>
    /// <returns>A stream that the caller must dispose after use.</returns>
    Stream OpenRead();
}

/// <summary>
/// Creates random hexadecimal prefixes for multipart image field names.
/// </summary>
internal static class RandomHexGenerator
{
    /// <summary>
    /// Gets a fresh eight-character lowercase hexadecimal string from four random bytes.
    /// </summary>
    public static string RandomHex => Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
}

/// <summary>
/// Represents a file-backed image asset for multipart upload with configurable stream-opening options.
/// </summary>
public class FilePathAsset : IImageStreamAsset
{
    /// <summary>The file system used to open the image file.</summary>
    private readonly IFileSystem _fileSystem;

    /// <summary>Gets the absolute path of the image file.</summary>
    public AbsolutePath Path { get; }

    /// <summary>
    /// Creates a file-backed image asset using the physical file system.
    /// </summary>
    /// <param name="path">The absolute path of the image file to open when its contents are requested.</param>
    public FilePathAsset(AbsolutePath path) : this(path, new FileSystem())
    {
    }

    /// <summary>
    /// Creates a file-backed image asset using the supplied file system.
    /// </summary>
    /// <param name="path">The absolute path of the image file to open when its contents are requested.</param>
    /// <param name="fileSystem">The file system used to open the image, or <see langword="null"/> to use the physical file system.</param>
    public FilePathAsset(AbsolutePath path, IFileSystem? fileSystem)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        Path = path;
        Name = RandomHexGenerator.RandomHex + Path.GetFilenameWithoutExtension();
        FileName = Path.FileName;
        AssetName = Name;
        Format = Path.GetExtensionWithoutDot();
    }

    /// <inheritdoc/>
    public string AssetName { get; set; }

    /// <summary>
    /// Gets or sets the asset reference, initially a random hexadecimal prefix followed by the source file name without its extension.
    /// </summary>
    /// <remarks>By default this will be the a 8 digit Hex and the filename without extensions</remarks>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the upload file name, initially the source file name including its extension.
    /// </summary>
    /// <remarks>Changing this value does not rename the source file or change its format.</remarks>
    public string FileName { get; set; }

    /// <summary>
    /// Sets the upload file name without changing the source file path.
    /// </summary>
    /// <param name="fileName">The file name to report for the uploaded image.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithFileName(string fileName)
    {
        FileName = fileName;
        return this;
    }

    /// <summary>
    /// Sets the image format reported in request metadata without converting the source file.
    /// </summary>
    /// <param name="format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithFormat(string format)
    {
        Format = format;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="AssetName"/>
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public FilePathAsset WithAssetName(string assetName)
    {
        AssetName = assetName;
        return this;
    }
    

    /// <summary>
    /// Sets the asset reference used to identify the image in request content.
    /// </summary>
    /// <param name="name">The reference to use in the assets map and request content.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithName(string name)
    {
        Name = name;
        return this;
    }

    /// <summary>
    /// Gets the image format from the source file extension without its leading dot.
    /// </summary>
    public string Format { get; set; } 

    /// <summary>
    /// Opens the source image file using the configured mode, access, sharing permissions, and file system.
    /// </summary>
    /// <returns>A fresh stream that the caller must dispose after use.</returns>
    public Stream OpenRead()
    {
        return Path.Open(Mode, FileAccess, FileShare, _fileSystem);
    }

    /// <summary>Gets the sharing permissions. Defaults to <c>FileShare.Read</c> to allow concurrent readers.</summary>
    public FileShare FileShare { get; private set; } = System.IO.FileShare.Read;

    /// <summary>Gets the access permissions. Defaults to <c>FileAccess.Read</c> for reading image data.</summary>
    public FileAccess FileAccess { get; private set; } = System.IO.FileAccess.Read;

    /// <summary>Gets the file mode. Defaults to <c>FileMode.Open</c> to open an existing file without creating or truncating it.</summary>
    public FileMode Mode { get; private set; } = FileMode.Open;

    /// <summary>
    /// Sets sharing permissions for subsequent stream openings.
    /// </summary>
    /// <param name="fileShare">The access other callers may have while an image stream is open.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithFileShare(FileShare fileShare)
    {
        FileShare = fileShare;
        return this;
    }

    /// <summary>
    /// Sets access permissions for subsequent stream openings.
    /// </summary>
    /// <param name="fileAccess">The read or write access requested for each new stream.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithFileAccess(FileAccess fileAccess)
    {
        FileAccess = fileAccess;
        return this;
    }

    /// <summary>
    /// Sets the file mode for subsequent stream openings.
    /// </summary>
    /// <param name="mode">The mode controlling how each stream opens or creates the source file.</param>
    /// <returns>This asset for further configuration.</returns>
    public FilePathAsset WithMode(FileMode mode)
    {
        Mode = mode;
        return this;
    }
}

/// <summary>
/// Represents a named image backed by bytes, Base64 text
/// </summary>
/// <param name="Name">The name identifying the image in the request's assets map.</param>
/// <param name="Format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
/// <param name="FileData">The image contents as raw bytes or base64-encoded text.</param>
/// <remarks>Using this will Base64 encode the image</remarks>
public record ImageAsset(string Name, string Format, FileData FileData)
{
    /// <summary>
    /// Creates image metadata with either Base64 data or a multipart part reference, without its asset name.
    /// </summary>
    public JsonNode Node
    {
        get
        {
            var jsonObject = new JsonObject();
            jsonObject["type"] = "image";
            jsonObject["format"] = Format;
            jsonObject["data_base64"] = FileData.Base64;
            return jsonObject;
        }
    }

    /// <summary>
    /// Create an ImageAsset from a byte array
    /// </summary>
    /// <param name="name">The name identifying the image in the request's assets map.</param>
    /// <param name="format">The image format, such as <c>png</c> or <c>jpg</c>, without a leading dot.</param>
    /// <param name="bytes">The raw image file contents.</param>
    /// <returns>An image asset backed by the supplied bytes.</returns>
    /// <remarks>This has been marked Obsolete as since 0.6.0 the class takes the <see cref="FileData"/> union type that supports both string and bytes</remarks>
    [Obsolete("Use new ImageAsset(name, format, bytes) instead.")]
    public static ImageAsset FromByteArray(string name, string format, Byte[] bytes)
    {
        return new ImageAsset(name, format, bytes);
    }
}
