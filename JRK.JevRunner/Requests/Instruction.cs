using System.Linq.Expressions;

namespace JRK.JevRunner.Requests;

/// <summary>
/// Base class for writing Jev instructions that refer to properties on the instruction object.
/// </summary>
/// <remarks>
/// Use <c>GetInstructionPath(() =&gt; Name)</c> to refer to a property by name rather than
/// insert its value. Pass the interpolated text to <see cref="FormatInstruction"/>
/// to put backticks around the reference.
/// </remarks>
public abstract class Instruction
{
    /// <summary>
    /// Formats an interpolated instruction, putting backticks around references
    /// to request state or assets.
    /// </summary>
    /// <param name="interpolated">The interpolated instruction, including any references to quote.</param>
    /// <returns>The instruction text.</returns>
    protected string FormatInstruction(FormattableString interpolated)
    {
        return interpolated.Escaped;
    }

    /// <summary>
    /// Creates a reference to a property or array element on this instruction without reading its value.
    /// </summary>
    /// <typeparam name="TValue">The type of the selected value.</typeparam>
    /// <param name="expression">A selector such as <c>() =&gt; Items[0].Name</c>.</param>
    /// <returns>A path that is backtick-quoted when interpolated into the instruction.</returns>
    protected StatePath GetInstructionPath<TValue>(Expression<Func<TValue>> expression) => StateExtensions.GetPath(expression);
}