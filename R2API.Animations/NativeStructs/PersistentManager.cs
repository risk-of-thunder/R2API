using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace R2API.NativeStructs;

[StructLayout(LayoutKind.Explicit)]
public struct PersistentManager
{
    [FieldOffset(0x58)]
    public IntPtr Remapper;
}
