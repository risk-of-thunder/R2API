using System;
using System.Collections.Generic;
using System.Text;

namespace R2API;

internal static class Extensions
{
    public static TValue GetOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TValue> defaultValueFunc)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        return dict[key] = defaultValueFunc();
    }

    public static int IndexOf<T>(this IEnumerable<T> values, Func<T, bool> predicate)
    {
        var i = 0;

        foreach (var value in values)
        {
            if (predicate(value))
            {
                return i;
            }
        }

        return -1;
    }
}
