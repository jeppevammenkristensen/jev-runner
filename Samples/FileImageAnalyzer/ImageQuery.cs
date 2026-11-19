using JRK.JevRunner.Annotation;
using JRK.JevRunner.Image;
using TruePath;

namespace FileImageAnalyzer;

/// <summary>Defines questions about pets, children, and screenshot content in an image asset.</summary>
[JevQuery]
public partial class ImageQuery
{
    /// <summary>Creates a query using the supplied image asset state.</summary>
    public ImageQuery(PhotoAssetState state, AbsolutePath file)
    {
        State = state;
        Photo = file.CreateAssetWithAdjustments("Photo");
    }

    /// <summary>Gets the asset state containing the image to analyze.</summary>
    public PhotoAssetState State { get; }

    /// <summary>Gets the question asking whether the image contains a pet.</summary>
    public GenericNoulQuestionDefinition<FormattableString> HasPet => new($"{Photo} has a Pet in the image", null, null);
    
    /// <summary>Gets the question asking whether the image contains a child.</summary>
    public GenericNoulQuestionDefinition<FormattableString> HasChild =>
        new($"{Photo} has a Child in the image", null, null);
    
    /// <summary>Gets the question asking whether the image is a screenshot.</summary>
    public GenericNoulQuestionDefinition<FormattableString> IsScreenShot => new ($"{Photo} is a ScreenShot image", null, null);
    
    public GenericBoxQuestionDefinition<FormattableString> ChildBox => new ($"`Find a Child in {Photo}");
    public AssetAndStateReference Photo { get; set; }
}