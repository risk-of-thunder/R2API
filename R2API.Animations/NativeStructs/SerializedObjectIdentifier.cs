using System.Runtime.InteropServices;

namespace R2API.NativeStructs;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct SerializedObjectIdentifier
{
    public int fileID;
    public long pathID;
}
