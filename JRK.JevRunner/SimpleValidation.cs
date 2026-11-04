namespace JRK.JevRunner;

/// <summary>Represents successful validation or a failure description paired with its category.</summary>
internal union SimpleValidation(bool, (string, RequestValidationError))
{
}
