using System.Linq.Expressions;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Base class for writing Jev instructions that refer to properties on the request state
/// as well as properties on the instruction object.
/// </summary>
/// <typeparam name="TState">The type of request state the instruction can reference.</typeparam>
/// <remarks>
/// Use <c>GetStatePath(state =&gt; state.File)</c> to refer to a state property by name.
/// You do not need a state object to build the reference.
/// </remarks>
public abstract class InstructionWithState<TState> : Instruction
{
    /// <summary>
    /// Creates a reference to a property or array element in the request state without reading its value.
    /// </summary>
    /// <param name="expression">A selector rooted at its state parameter, such as <c>state =&gt; state.File</c>.</param>
    /// <returns>A path that is backtick-quoted when formatted into the instruction.</returns>
    protected StatePath GetStatePath(Expression<Func<TState, object>> expression) => StateExtensions.GetPath(expression);
}