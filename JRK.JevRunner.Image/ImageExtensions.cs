using System.IO.Abstractions;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>
/// Prepares file-backed or resized image assets for later registration in request state.
/// </summary>
public static class ImageExtensions
{
    /// <param name="path">The source image file accessed through the supplied file system.</param>
    extension(AbsolutePath path)
    {
                
        /// <summary>
        /// Validates an image file and creates an asset paired with a state reference, resizing the image when suggested.
        /// </summary>
        /// <param name="stateName">The state reference to pair with the created asset.</param>
        /// <param name="assetName">The asset reference to assign, or null or whitespace to retain the generated reference.</param>
        /// <param name="token">Checks for cancellation after validation, before creating or resizing the asset.</param>
        /// <param name="fileSystem">The file system used for validation and image access, or <see langword="null"/> to use the physical file system.</param>
        /// <returns>A file-backed asset for an unchanged image, or a byte-backed PNG asset for a resized image, paired with the supplied state reference.</returns>
        /// <remarks>
        /// The source file is not modified. A suggested resize is attempted once without revalidating its output.
        /// This method does not register the asset in a state.
        /// </remarks>
        /// <exception cref="OperationCanceledException">Cancellation is requested when checked after validation.</exception>
        /// <exception cref="InvalidOperationException">No asset can be created because the image format could not be detected.</exception>
        public AssetAndStateReference CreateAssetWithAdjustments(StateReference stateName,
            string? assetName,
            CancellationToken token = default,
            IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            
            ImageValidator imageValidator = new ImageValidator(fileSystem);
            var validationResult = imageValidator.Validate(path);
            token.ThrowIfCancellationRequested();

            Asset? asset = null;

            if (validationResult is ValidImageResult valid)
            {
                var filePathAsset = new FilePathAsset(path, fileSystem)
                    .WithFormat(valid.Format);
                if (!string.IsNullOrWhiteSpace(assetName))
                {
                    filePathAsset = filePathAsset.WithAssetName(assetName);
                }
                asset =  filePathAsset;
            }
            else if (validationResult is InvalidImageResult invalidImageResult)
            {
                var suggestedScaling = invalidImageResult.SuggestedScaling;
                var filePathAsset = new FilePathAsset(path, fileSystem);
                
                if (!string.IsNullOrWhiteSpace(assetName))
                {
                    filePathAsset = filePathAsset.WithAssetName(assetName);
                }
                
                if (suggestedScaling == ImageSuggestedScaling.Upscale)
                {
                    ImageScaler scaler = new ImageScaler(fileSystem);
                    asset = new ByteArrayAsset(scaler.Upscale(path), filePathAsset.Name, path.WithExtension("png").FileName, filePathAsset.AssetName, "png");
                }
                else if (suggestedScaling == ImageSuggestedScaling.Downscale)
                {
                    ImageScaler scaler = new ImageScaler(fileSystem);
                    asset = new ByteArrayAsset(scaler.Downscale(path),filePathAsset.Name, path.WithExtension("png").FileName, filePathAsset.AssetName, "png");
                }
                else
                {
                    ThrowHelper.Throw(invalidImageResult);
                }
            }

            if (asset == null)
            {
                throw new InvalidOperationException("The image format could not be detected.");
            }

            return new AssetAndStateReference(stateName, asset.Value);
        }

    }
}