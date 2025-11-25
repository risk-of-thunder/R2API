using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace R2API.Animations.Editor;
public static class Extensions
{
    public static void AddNotNull<T>(this List<T> list, T value) where T : class
    {
        if (value is not null)
        {
            list.Add(value);
        }
    }

    public static void AddRangeNotNull<T>(this List<T> list, IEnumerable<T> values) where T : class
    {
        list.AddRange(values.Where(static v => v is not null));
    }
}
