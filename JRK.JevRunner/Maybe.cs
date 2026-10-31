namespace JRK.JevRunner;

internal union Maybe<T>(T,NoneValue)
{
    internal Maybe<T> Some (T value)
    {
        return new Maybe<T>(value);
    }

    internal Maybe<T> None => NoneValue.New;

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