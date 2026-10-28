using JRK.JevRunner.Infrastructure;
using TruePath;

namespace FileImageAnalyzer;

/// <summary>
/// Hold photo asset state
/// </summary>
/// <remarks>If you want to include Images use AssetState or a class that inherits in some way from AssetState</remarks>
public class PhotoAssetState : AssetState
{
    private readonly AbsolutePath _path;

    public PhotoAssetState(AbsolutePath path)
    {
        _path = path;
    }
    
    /// <summary>The asset name used to reference the image in query questions.</summary>
    public const string PhotoAssetName = "Photo";

    /// <summary>
    /// This is here just to signal that a AssetState can also contain additional properties
    /// </summary>
    public string PhotoFileName => _path.FileName;

}