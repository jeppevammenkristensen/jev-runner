namespace JRK.JevRunner;

/// <summary>Identifies a state field by path for backtick-quoted interpolation into instructions.</summary>
/// <remarks>Validates the text, but does not resolve the path against request state.</remarks>
public class StatePath : IBackTick
{
    public string Path { get; }

    /// <summary>Creates a state path without whitespace or backticks.</summary>
    /// <param name="path">The nonempty path, such as <c>ticket.messages[0].text</c>.</param>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ArgumentException">The path is empty or consists only of whitespace.</exception>
    /// <exception cref="InvalidOperationException">The path contains whitespace or backticks.</exception>
    public StatePath(string path)
    {
        StateRegex.ThrowIfInvalid(path);
        Path = path;
    }

    string IBackTick.Value => Path;
    
}
