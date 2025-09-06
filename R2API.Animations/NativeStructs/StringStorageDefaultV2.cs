using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace R2API.NativeStructs;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct StringStorageDefaultV2
{
    public StringStorageDefaultV2Union union;
    public StringRepresentation data_repr;
    public int label;
}

[StructLayout(LayoutKind.Explicit, Pack = 8)]
public struct StringStorageDefaultV2Union
{
    [FieldOffset(0)]
    public StackAllocatedRepresentationV2 embedded;
    [FieldOffset(0)]
    public HeapAllocatedRepresentationV2 heap;
}

public struct StackAllocatedRepresentationV2
{
    public unsafe fixed byte data[25];
}

public struct HeapAllocatedRepresentationV2
{
    public nint data;
    public ulong capacity;
    public ulong size;
}

public enum StringRepresentation : int
{
    Heap,
    Embedded,
    External
}
