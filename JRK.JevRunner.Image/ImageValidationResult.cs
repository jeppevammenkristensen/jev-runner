using TruePath;

namespace JRK.JevRunner.Image;

/// <summary>Represents either a locally valid image with its bytes or validation failures.</summary>
/// <remarks>Pattern-match the result to access image metadata or errors and scaling suggestions.</remarks>
public union ImageValidationResult(ValidImageResult, InvalidImageResult)
{
    /// <summary>Whether all local image checks passed.</summary>
    public bool IsValid => this is ValidImageResult;
    
    /// <summary>Throws with the validation error messages when any local image check failed.</summary>
    /// <exception cref="InvalidOperationException">The result contains one or more validation errors.</exception>
    public void ThrowIfInvalid()
    {
        switch (this)
        {
            case InvalidImageResult invalidImageResult:
                ThrowHelper.Throw(invalidImageResult);
                return;
            case ValidImageResult:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}