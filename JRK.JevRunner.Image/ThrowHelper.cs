using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JRK.JevRunner.Image;

/// <summary>Formats image validation failures as an exception.</summary>
internal static class ThrowHelper
{
    /// <summary>Throws an exception containing all reported image validation messages.</summary>
    [DoesNotReturn]
    public static void Throw(InvalidImageResult imageResult)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Image is not valid to use in a Jev Query");

        foreach (var imageValidationError in imageResult.Errors)
        {
            builder.AppendLine(imageValidationError.GetErrorMessage());
        }

        throw new InvalidOperationException(builder.ToString());
    }
}