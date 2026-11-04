namespace JRK.JevRunner;

/// <summary>Identifies a locally detected model compatibility problem.</summary>
public enum RequestValidationError
{
    /// <summary>The model identifier is not in the bundled list of known versions or aliases.</summary>
    UnsupportedVersion,
    /// <summary>The selected model is known not to support Box questions.</summary>
    BoxNotSupported
}
