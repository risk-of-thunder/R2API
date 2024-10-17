using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using R2API.NativeStructs;

namespace R2API;

internal static unsafe class NativeHelpers
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate void InstanceIDToSerializedObjectIdentifierHandler(IntPtr remapper, int instanceID, SerializedObjectIdentifier* identifier);
    private const int InstanceIDToSerializedObjectIdentifierOffset = 0x690020;

    private static IntPtr PersistentManagerPtr;
    private static PersistentManager* PersistentManager => *(PersistentManager**)PersistentManagerPtr.ToPointer();
    private static InstanceIDToSerializedObjectIdentifierHandler InstanceIDToSerializedObjectIdentifier;


    public static void Init()
    {
        static bool IsUnityPlayer(ProcessModule p)
        {
            return p.ModuleName.ToLowerInvariant().Contains("unityplayer");
        }

        var proc = Process.GetCurrentProcess().Modules
            .Cast<ProcessModule>()
            .FirstOrDefault(IsUnityPlayer) ?? Process.GetCurrentProcess().MainModule;
        var baseAddress = proc.BaseAddress;

        PersistentManagerPtr = baseAddress + 0x1b3c168;
        InstanceIDToSerializedObjectIdentifier = Marshal.GetDelegateForFunctionPointer<InstanceIDToSerializedObjectIdentifierHandler>(baseAddress + InstanceIDToSerializedObjectIdentifierOffset);
    }

    public static long GetAssetPathID(UnityEngine.Object obj)
    {
        var identifier = new SerializedObjectIdentifier();
        var remapper = PersistentManager->Remapper;
        InstanceIDToSerializedObjectIdentifier(remapper, obj.GetInstanceID(), &identifier);
        return identifier.pathID;
    }
}
