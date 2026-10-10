using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;

namespace AnimalQuiz;

/// <summary>
/// Groups the context and AI judgments for one turn of the animal quiz into a single Jev request.
/// </summary>
/// <remarks>
/// The <c>JevQuery</c> attribute enables source generation of <c>GetRequest()</c> and named answer helpers.
/// The game uses those helpers to read the input classification and the answer to the player's question.
/// </remarks>
[JevQuery]
public partial class GuessQuery
{
    /// <summary>Creates a turn using the hidden animal and the player's latest input as shared context.</summary>
    public GuessQuery(GuessQueryState queryState)
    {
        State = queryState;
    }
    
    /// <summary>Gets the context that Jev evaluates all of this turn's questions against.</summary>
    public GuessQueryState State { get; }
    /// <summary>Gets the model selection; this sample requests the latest Jev model.</summary>
    public JevModel JevModel { get; } = JevModel.Latest;

    /// <summary>Asks Jev whether the input narrows the search, guesses an animal, is invalid, or gives up.</summary>
    public QuessType QuestionType { get; } = new();

    /// <summary>
    /// Asks whether the player's question is true of the hidden animal, such as whether a dolphin lives in water.
    /// The game also uses this answer to check a direct guess such as "Is it a dolphin?".
    /// </summary>
    public NoulQuestion AskedQuestionIsCorrect { get; } =
        new("The CurrentQuestion is correct regarding the specific AnimalToGuess", null, null);

    /// <summary>
    /// Adds a separate judgment of whether the player guessed the animal.
    /// It is included in the request but is not currently read by the game loop.
    /// </summary>
    public NoulQuestion UserGuessTheAnimal { get; } = new("The user guessed the animal", null, null);


}
