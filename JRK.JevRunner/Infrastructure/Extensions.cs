using System.IO.Abstractions;
using JRK.JevRunner.Requests;
using TruePath;

namespace JRK.JevRunner.Infrastructure;

/// <summary>
/// Provides helpers for encoding image bytes and adding image files to asset-backed request state.
/// </summary>
public static class Extensions
{
    extension<TAssetState>(TAssetState state) where TAssetState : AssetState
    {
        
        /// <summary>
        /// Reads an image file, base64-encodes its bytes, and adds the image asset and its state reference.
        /// </summary>
        /// <param name="stateName">The state field that will contain the asset reference.</param>
        /// <param name="assetName">The reference identifying the image in the request's assets map.</param>
        /// <param name="filePath">The image file whose extension supplies the asset format.</param>
        /// <param name="fileSystem">An optional file system used to check and read the image file.</param>
        /// <returns>The same state instance for further configuration.</returns>
        /// <exception cref="InvalidOperationException">The image file does not exist.</exception>
        /// <remarks>The <paramref name="stateName"/> field can be referenced in questions about the image.
        /// The file extension is used as the format without validating the image contents.</remarks>
        public async Task<TAssetState> AddImageAsset(string stateName, string assetName, AbsolutePath filePath, IFileSystem? fileSystem = null)
        {
            if (!filePath.FileExists(fileSystem))
            {
                throw new InvalidOperationException("No image found at the given location");
            }

            await using var fileSystemStream = filePath.OpenRead(fileSystem);
            using var buffer = new MemoryStream();
            await fileSystemStream.CopyToAsync(buffer);
            var base64 = Convert.ToBase64String(buffer.ToArray());
            state.AddAsset(stateName, new ImageAsset(assetName,  filePath.GetExtensionWithoutDot(), base64));
            return state;
        }
    }
}
