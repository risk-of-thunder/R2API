namespace R2API.Commands
{
    /// <summary>
    ///     Represents a parser.
    /// </summary>
    /// <param name="s">The string to parse, which must not be <c>null</c>.</param>
    /// <returns>The result.</returns>
    public delegate object Parser(string s);
}
