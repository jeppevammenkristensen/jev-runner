namespace JRK.JevRunner.Requests;

/// <summary>
/// Builds a named box question from instructions identifying a target in an image.
/// </summary>
public class BoxQuestionBuilder : IQuestionBuilder<BoxQuestion>
{
    /// <summary>Gets the question name used to identify its returned answer.</summary>
    public string Name { get; }
    
    /// <summary>Gets the instructions identifying the target and the image to search.</summary>
    public JevMessage Instructions { get; }

    /// <summary>Creates a builder for the supplied question name and image-search instructions.</summary>
    /// <param name="name">The question identifier used to associate the returned answer.</param>
    /// <param name="instructions">Instructions identifying the target and the image to search.</param>
    public BoxQuestionBuilder(string name, JevMessage instructions)
    {
        Name = name;
        Instructions = instructions;
    }
    
    /// <summary>Creates a box question using this builder's name and instructions.</summary>
    /// <returns>A new box question.</returns>
    public BoxQuestion Build()
    {
        return new BoxQuestion(Name, Instructions);
    }

    /// <summary>Creates the box question wrapped in the request question union.</summary>
    Question IQuestionBuilder<BoxQuestion>.BuildQuestion()
    {
        return Build();
    }
}
