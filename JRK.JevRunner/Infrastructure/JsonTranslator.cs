using System.IO.Abstractions;
using System.Text.Json.Nodes;
using JRK.JevRunner.Requests;
using TruePath;

namespace JRK.JevRunner.Infrastructure;

/// <summary>
/// Translates JevRunner requests into the JSON payload expected by the Jev service.
/// </summary>
internal class JsonTranslator
{
    /// <summary>
    /// Creates the JSON payload for a request, including its state, model, questions, and assets.
    /// </summary>
    internal static JsonObject CreateRequestJson(Request request)
    {
        var json = new JsonObject
        {
            {"state", request.State.Node},
            {"model", request.Model.Model}
        };

        var question = new JsonObject();

        foreach (var requestQuestion in request.Questions)
        {
            requestQuestion.AddTo(question);
        }
        
        json.Add("questions", question);
        
        var assets = new JsonObject();

        foreach (var asset in request.Assets)
        {
            asset.AddTo(assets);
        }
        
        json.Add("assets", assets);

        return json;
    }
}

/// <summary>
/// Provides helpers for adding image files to asset-backed request state.
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
        /// <remarks>The stateName can be used in Questions. For instance 'Is there a cat in `foo`</remarks>
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


/// <summary>
/// Stores named asset references as request state and tracks their associated assets.
/// </summary>
/// <remarks>Using this class or inheriting from it ensures that the given asset is also added when included in a Request</remarks>
public class AssetState : Dictionary<string,string>
{
    /// <summary>
    /// Gets or sets the assets indexed by their state field names.
    /// </summary>
    internal Dictionary<string, Asset> Assets { get; set; } = [];

    /// <summary>
    /// Registers an asset and adds its reference to the state under the supplied field name.
    /// </summary>
    /// <param name="name">The state field that will contain the asset reference. </param>
    /// <param name="asset">The asset to include with the request. (The <see cref="name"/> will point to this assets name)</param>
    /// <returns>The same state instance for further configuration.</returns>
    /// <exception cref="ArgumentException">The state already contains the supplied field name.</exception>
    public AssetState AddAsset(string name, Asset asset)
    {
        Assets[name] = asset;
        Add(name, asset.Name);
        return this;
    }
}
