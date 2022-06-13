using System;

namespace R2API.Utils;

internal class StringUtils
{
    internal static void ThrowIfStringIsNullOrWhiteSpace(string stringToCheck, string paramName)
    {
        if (string.IsNullOrWhiteSpace(stringToCheck))
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
