using JetBrains.Annotations;

namespace AnimalQuiz;

/// <summary>
/// Supplies the facts for one quiz turn: the secret animal and the player's latest question or guess.
/// These names match the context referred to in the query's instructions.
/// </summary>
/// <param name="AnimalToGuess">The animal Jev should use when evaluating the player's question.</param>
/// <param name="CurrentQuestion">The player's input, including questions, direct guesses, or a request to give up.</param>
public record GuessQueryState([UsedImplicitly] string AnimalToGuess, string CurrentQuestion);
