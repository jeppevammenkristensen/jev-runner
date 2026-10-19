using System.IO.Abstractions;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Prepares image files for asset-backed state, optionally resizing them before registration.</summary>
public static class AssetImageExtensions
{
    extension<TAssetState>(TAssetState state) where TAssetState : AssetState
    {
        /// <summary>
        /// Validates an image and throws an exception if it's invalid and otherwise add it as an ImageAssert
        /// </summary>
        /// <param name="stateName">The name added to the state</param>
        /// <param name="assetName">The asset name used in assets</param>
        /// <param name="path">The path to the file</param>
        /// <param name="token">Cancellation</param>
        /// <param name="fileSystem">optional filesystem will use default if null</param>
        /// <returns>A modified AssetStat</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TAssetState> AddImageWithValidation(string stateName, string assetName, AbsolutePath path,
            CancellationToken token = default, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            
            ImageValidator imageValidator = new ImageValidator(fileSystem);
            var validationResult = imageValidator.Validate(path);
            token.ThrowIfCancellationRequested();
            validationResult.ThrowIfInvalid();
            
            await using var input = path.OpenRead(fileSystem);
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, token);
            var bytes = buffer.ToArray();
            var format = validationResult.Format;
            
            state.AddAsset(stateName, ImageAsset.FromByteArray(assetName,
                format ?? throw new InvalidOperationException("The image format could not be detected."), bytes));
            return state;

        }
        
        /// <summary>
        /// Validates an image, attempts a suggested resize when invalid, and registers its bytes in the state.
        /// </summary>
        /// <param name="stateName">The state field containing the asset reference.</param>
        /// <param name="assetName">The reference identifying the image in the request's assets map.</param>
        /// <param name="path">The source image, which is never modified.</param>
        /// <param name="token">Cancels byte loading before the asset is registered.</param>
        /// <param name="fileSystem">Optional file access used for validation, loading, and resizing.</param>
        /// <returns>The same state instance.</returns>
        /// <remarks>Unchanged images retain their detected format and bytes. Resized images are encoded as PNG.
        /// A single resize is attempted without revalidating its output; scaling suggestions consider only
        /// dimension violations. File access and scaling failures propagate without registering an asset.</remarks>
        public async Task<TAssetState> AddImageAssetWithAutoAdjustment(string stateName, string assetName, AbsolutePath path,
            CancellationToken token = default, IFileSystem? fileSystem = null)
        {
            fileSystem ??= new FileSystem();
            
            ImageValidator imageValidator = new ImageValidator(fileSystem);
            var validationResult = imageValidator.Validate(path);
            token.ThrowIfCancellationRequested();
            
            await using var input = path.OpenRead(fileSystem);
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer, token);
            var bytes = buffer.ToArray();
            var format = validationResult.Format;
            
            if (!validationResult.IsValid)
            {
                var suggestedScaling = validationResult.SuggestedScaling;

                if (suggestedScaling == ImageSuggestedScaling.Upscale)
                {
                    ImageScaler scaler = new ImageScaler(fileSystem);
                    bytes = scaler.Upscale(path);
                    format = "png";
                }
                else if (suggestedScaling == ImageSuggestedScaling.Downscale)
                {
                    ImageScaler scaler = new ImageScaler(fileSystem);
                    bytes = scaler.Downscale(path);
                    format = "png";
                }
                else
                {
                    validationResult.ThrowIfInvalid();
                }
            }
            
            state.AddAsset(stateName, ImageAsset.FromByteArray(assetName,
                format ?? throw new InvalidOperationException("The image format could not be detected."), bytes));
            return state;

        }
    }
}
