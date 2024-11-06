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
}
