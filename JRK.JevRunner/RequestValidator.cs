using JRK.JevRunner.Requests;

namespace JRK.JevRunner;

/// <summary>Checks requests against the locally known model and question compatibility rules.</summary>
internal static class RequestValidator
{
    /// <summary>Checks for unknown model identifiers and unsupported Box questions.</summary>
    /// <param name="request">The request whose model and questions are inspected.</param>
    /// <returns>True when valid, or the first failure's description and category.</returns>
    public static SimpleValidation Validate(Request request)
    {
        if (request.Model is {IsKnownVersion: false})
        {
            return ($"Unknown version {request.Model.Model}", RequestValidationError.UnsupportedVersion);
        }

        if (request.Questions.Any(x => x is BoxQuestion) && request.Model.IsVersion(JevVersions.Version1_13_0))
        {
            return ($"Box questions are only supported from version {JevVersions.Version1_14_0}", RequestValidationError.BoxNotSupported);
        }

        return true;
    }
}
