namespace JRK.JevRunner;

/// <summary>Formats instructions with backtick-quoted state references and paths.</summary>
public static class FormattableStringExtensions
{
    /// <param name="fm">The interpolated instructions whose arguments are to be formatted.</param>
    extension(FormattableString fm)
    {
        /// <summary>
        /// Formats the text, quoting state references and paths and preserving other arguments.
        /// </summary>
        /// <remarks>Uses the current culture and the original format string; no state lookup is performed.</remarks>
        public string Escaped
        {
            get
            {
                List<object?> objects = [];
        
                foreach (var argument in fm.GetArguments())
                {
                    object? obj = argument switch
                    {
                        IBackTick name => name.BackTickQuoted,
                        _ => argument
                    };
                    objects.Add(obj);
                }
                return string.Format(fm.Format, objects.ToArray());
            }
        }
    }
}
