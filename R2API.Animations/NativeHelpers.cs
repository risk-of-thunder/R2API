using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using R2API.NativeStructs;

namespace R2API;

internal static unsafe class NativeHelpers
{
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate void InstanceIDToSerializedObjectIdentifierHandler(IntPtr remapper, int instanceID, SerializedObjectIdentifier* identifier);
    private const int InstanceIDToSerializedObjectIdentifierOffset = 0x690020;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate IntPtr PersistentManagerGetPathNameHandler(PersistentManager* persistentManager, StringStorageDefaultV2* returnString, int instanceID);
    private const int PersistentManagerGetPathNameOffset = 0x67e6f0;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeAllocInternalHandler(IntPtr ptr, int label, IntPtr file, int line);
    private const int FreeAllocInternalOffset = 0x279340;

    private const int PersistentManagerOffset = 0x1b3c168;

    private static IntPtr PersistentManagerPtr;
    private static PersistentManager* PersistentManager => *(PersistentManager**)PersistentManagerPtr.ToPointer();
    private static InstanceIDToSerializedObjectIdentifierHandler InstanceIDToSerializedObjectIdentifier;
    private static PersistentManagerGetPathNameHandler PersistentManagerGetPathName;
    private static FreeAllocInternalHandler FreeAllocInternal;

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

        PersistentManagerPtr = baseAddress + PersistentManagerOffset;
        InstanceIDToSerializedObjectIdentifier = Marshal.GetDelegateForFunctionPointer<InstanceIDToSerializedObjectIdentifierHandler>(baseAddress + InstanceIDToSerializedObjectIdentifierOffset);
        PersistentManagerGetPathName = Marshal.GetDelegateForFunctionPointer<PersistentManagerGetPathNameHandler>(baseAddress + PersistentManagerGetPathNameOffset);
        FreeAllocInternal = Marshal.GetDelegateForFunctionPointer<FreeAllocInternalHandler>(baseAddress + FreeAllocInternalOffset);
    }

    /// <summary>
    /// Get PathID in an AssetBundle that the object is loaded from
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static long GetAssetPathID(UnityEngine.Object obj)
    {
        if (!obj)
        {
            return 0;
        }

        return GetAssetPathID(obj.GetInstanceID());
    }

    /// <summary>
    /// Get PathID in an AssetBundle that the object is loaded from
    /// </summary>
    /// <param name="instanceID"></param>
    /// <returns></returns>
    public static long GetAssetPathID(int instanceID)
    {
        var identifier = new SerializedObjectIdentifier();
        var remapper = PersistentManager->Remapper;

        InstanceIDToSerializedObjectIdentifier(remapper, instanceID, &identifier);
        return identifier.pathID;
    }

    /// <summary>
    /// Get internal name for an AssetBundle that the object is loaded from
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static string GetPathName(UnityEngine.Object obj)
    {
        if (!obj)
        {
            return "";
        }

        return GetPathName(obj.GetInstanceID());
    }

    /// <summary>
    /// Get internal name for an AssetBundle that the object is loaded from
    /// </summary>
    /// <param name="instanceID"></param>
    /// <returns></returns>
    public static string GetPathName(int instanceID)
    {
        var stringStorage = new StringStorageDefaultV2();
        PersistentManagerGetPathName(PersistentManager, &stringStorage, instanceID);

        if (stringStorage.data_repr != StringRepresentation.Embedded && (stringStorage.union.heap.data == 0 || stringStorage.union.heap.size == 0))
        {
            return "";
        }

        switch(stringStorage.data_repr)
        {
            case StringRepresentation.Embedded:
            {
                return Marshal.PtrToStringAnsi((IntPtr)stringStorage.union.embedded.data);
            }
            default:
            {
                var str = Marshal.PtrToStringAnsi(stringStorage.union.heap.data, (int)stringStorage.union.heap.size);
                if (str != null)
                {
                    FreeAllocInternal(stringStorage.union.heap.data, stringStorage.label, IntPtr.Zero, 0);
                    return str;
                }

                return "";
            }
        };
    }
}
