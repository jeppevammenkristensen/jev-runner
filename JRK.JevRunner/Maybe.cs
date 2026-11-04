namespace JRK.JevRunner;

/// <summary>Represents a value or an explicit empty result.</summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
internal union Maybe<T>(T,NoneValue)
{
    /// <summary>Wraps a present value in an optional result.</summary>
    /// <param name="value">The value to wrap.</param>
    /// <returns>An optional result containing the supplied value.</returns>
    internal Maybe<T> Some (T value)
    {
        return new Maybe<T>(value);
    }

    internal Maybe<T> None => NoneValue.New;

    /// <summary>Invokes the callback corresponding to the stored value or empty case.</summary>
    /// <param name="some">The callback receiving the stored value when present.</param>
    /// <param name="none">The optional callback invoked when the result is empty.</param>
    /// <returns>This optional result for further processing.</returns>
    public Maybe<T> If(Action<T> some, Action? none = null)
    {
        switch (Value)
        {
            case NoneValue:
                none?.Invoke();
                break;
            case T unknown:
                some.Invoke(unknown);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return this;
    }
}
