
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;

namespace AnimalQuiz;

/// <summary>Classifies a question asked while guessing an animal.</summary>
public enum GuessQuestionType
{
    /// <summary>A yes-or-no question that narrows down the animal.</summary>
    LimitQuestion,
    /// <summary>A direct guess of the animal's identity.</summary>
    DirectQuestion,
    /// <summary>A question that neither narrows down nor directly guesses the animal.</summary>
    OtherQuestion,
    /// <summary>Input that is not formulated as a question.</summary>
    NotAQuestion,
    /// <summary>Input asking to stop the round or reveal the hidden animal.</summary>
    UserGivesUp
}

/// <summary>
/// Defines the possible kinds of player input so Jev can choose how the game should handle a turn.
/// Each choice has a stable name for code and a natural-language description for the model.
/// </summary>
public class QuessType : IChoiceQuestionDefinition<string>
{

    /// <summary>Gets the choice name for a narrowing question such as "Does it fly?".</summary>
    public string LimitQuestion { get; } = nameof(GuessQuestionType.LimitQuestion);
    /// <summary>Gets the choice name for an identity guess such as "Is it an eagle?".</summary>
    public string DirectQuestion { get; } = nameof(GuessQuestionType.DirectQuestion);
    /// <summary>Gets the choice name for a question outside the supported yes-or-no guessing format.</summary>
    public string OtherQuestion { get; } = nameof(GuessQuestionType.OtherQuestion);
    /// <summary>Gets the choice name for input that is not phrased as a question.</summary>
    public string NotAQuestion { get; } = nameof(GuessQuestionType.NotAQuestion);

    /// <summary>Gets the choice name for a request to give up or reveal the animal.</summary>
    public string UserGivesUp { get; } = nameof(GuessQuestionType.UserGivesUp);

    /// <summary>Pairs each choice name with instructions and examples that help Jev classify the player's input.</summary>
    public QuessType()
    {
        Choices =
        [
            new(LimitQuestion,
                "Trying to limit by yes or no to find out what animal it is. For instance. Does it live in water. Does it fly"),
            new(DirectQuestion,
                "Trying to directly guess what animal it is. For instance is the animal foo"),
            new(OtherQuestion, "A question that is not a limiting question (yes or no) or directly trying to guess"),
            new(NotAQuestion, "The CurrentQuestion is not formulated as a question"),
            new (UserGivesUp, "The user signals that they give up or want to now what the animal is")
        ];
    }

    /// <summary>Gets the instruction directing Jev to classify the current input from the shared state.</summary>
    public string Instructions { get; } = "Evalutate the CurrentQuestion";

    /// <summary>Gets the five named alternatives Jev can choose from for this judgment.</summary>
    public ChoiceCriteria[] Choices { get; }

    /// <summary>Converts Jev's selected choice name into the enum used to branch in the game loop.</summary>
    /// <returns>The enum value corresponding to the choice name.</returns>
    /// <exception cref="ArgumentException">The value is not one of the five choice names.</exception>
    public GuessQuestionType GetQuestionType(string value) => value switch
    {
        nameof(GuessQuestionType.LimitQuestion) => GuessQuestionType.LimitQuestion,
        nameof(GuessQuestionType.DirectQuestion) => GuessQuestionType.DirectQuestion,
        nameof(GuessQuestionType.OtherQuestion) => GuessQuestionType.OtherQuestion,
        nameof(GuessQuestionType.NotAQuestion) => GuessQuestionType.NotAQuestion,
        nameof(GuessQuestionType.UserGivesUp) => GuessQuestionType.UserGivesUp,
        _ => throw new ArgumentException("Unknown question type.", nameof(value))
    };
}
