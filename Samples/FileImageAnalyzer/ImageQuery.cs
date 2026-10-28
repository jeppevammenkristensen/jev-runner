using JRK.JevRunner.Annotation;

namespace FileImageAnalyzer;

/// <summary>Defines questions about pets, children, and screenshot content in an image asset.</summary>
[JevQuery]
public partial class ImageQuery
{
    /// <summary>Creates a query using the supplied image asset state.</summary>
    public ImageQuery(PhotoAssetState state)
    {
        State = state;
    }

    /// <summary>Gets the asset state containing the image to analyze.</summary>
    public PhotoAssetState State { get; }

    /// <summary>Gets the question asking whether the image contains a pet.</summary>
    public GenericNoulQuestionDefinition<string> HasPet => new($"`{PhotoAssetState.PhotoAssetName}` has a Pet in the image", null, null);
    
    /// <summary>Gets the question asking whether the image contains a child.</summary>
    public GenericNoulQuestionDefinition<string> HasChild =>
        new($"`{PhotoAssetState.PhotoAssetName}` has a Child in the image", null, null);
    
    /// <summary>Gets the question asking whether the image is a screenshot.</summary>
    public GenericNoulQuestionDefinition<string> IsScreenShot => new ($"`{PhotoAssetState.PhotoAssetName}` is a ScreenShot image", null, null);
    
    public GenericBoxQuestionDefinition<string> ChildBox => new ($"`Find a Child in `{PhotoAssetState.PhotoAssetName}`");
    
    
    
}