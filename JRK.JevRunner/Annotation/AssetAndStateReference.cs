using JRK.JevRunner.Requests;

namespace JRK.JevRunner.Annotation;

/// <summary>
/// Pairs an asset with the state field that will reference it when registered.
/// </summary>
/// <param name="Reference">The state field used to refer to the asset in questions and request state.</param>
/// <param name="Asset">The asset to register under the state field.</param>
public record AssetAndStateReference(StateReference Reference, Asset Asset)
{
    
}