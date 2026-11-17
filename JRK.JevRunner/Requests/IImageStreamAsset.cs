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