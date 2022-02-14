﻿using MonoMod.Cil;
using System;

namespace R2API.MiscHelpers {
    internal static class ILCursorExtensions {
        public static ILCursor EmitDel<TDel>(this ILCursor cursor, TDel func)
            where TDel : Delegate {
            cursor.EmitDelegate(func);
            return cursor;
        }
    }
}
