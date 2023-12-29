using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

[AutoVersion]
public static partial class ProcTypeAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".proctype";
    public const string PluginName = R2API.PluginName + ".ProcType";

    public static int ModdedProcTypeCount
    {
        get => _moddedProcTypeCount;
        private set
        {
            if (_moddedProcTypeCount != value)
            {
                _moddedProcTypeCount = value;
                totalByteCount = value + 7 >> 3;
                filledByteCount = totalByteCount - 1;
                remainingBitCount = value - (filledByteCount << 3);
                if (buffer != null)
                {
                    Array.Resize(ref buffer, value);
                }
            }
        }
    }

    private static bool _hookEnabled = false;
    private static int _moddedProcTypeCount;

    #region NetworkSettings
    private static int totalByteCount;
    private static int filledByteCount;
    private static int remainingBitCount;
    private static bool[] buffer;
    #endregion

    #region API
    public static ModdedProcType ReserveProcType()
    {
        SetHooks();
        if (ModdedProcTypeCount == int.MaxValue)
        {
            throw new IndexOutOfRangeException($"Reached the ModdedProcType limit ({int.MaxValue})! Please contact R2API developers to increase the limit");
        }
        return (ModdedProcType)ModdedProcTypeCount++;
    }

    public static void AddModdedProc(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        if (procType <= ModdedProcType.Invalid || (int)procType >= ModdedProcTypeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(procType));
        }
        SetHooks();
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        bool[] value;
        if (mask != null)
        {
            value = new bool[Math.Max(mask.Length, (int)procType + 1)];
            Array.Copy(mask, value, mask.Length);
        }
        else
        {
            value = new bool[(int)procType + 1];
        }
        value[(int)procType] = true;
        ProcTypeInterop.SetModdedMask(ref procChainMask, value);
    }

    public static void RemoveModdedProc(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        if (procType <= ModdedProcType.Invalid || (int)procType >= ModdedProcTypeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(procType));
        }
        SetHooks();
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask != null && ArrayUtils.IsInBounds(mask, (int)procType))
        {
            mask = ArrayUtils.Clone(mask);
            mask[(int)procType] = false;
            ProcTypeInterop.SetModdedMask(ref procChainMask, mask);
        }
    }

#pragma warning disable R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    public static bool HasModdedProc(this ProcChainMask procChainMask, ModdedProcType procType)
#pragma warning restore R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    {
        if (procType <= ModdedProcType.Invalid || (int)procType >= ModdedProcTypeCount)
        {
            throw new ArgumentOutOfRangeException(nameof(procType));
        }
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        return mask != null && ArrayUtils.GetSafe(mask, (int)procType);
    }

    public static BitArray GetModdedMask(ProcChainMask procChainMask)
    {
        SetHooks();
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask == null)
        {
            return new BitArray(ModdedProcTypeCount);
        }
        BitArray result = new BitArray(mask);
        result.Length = ModdedProcTypeCount;
        return result;
    }

    public static void SetModdedMask(ref ProcChainMask procChainMask, BitArray moddedMask)
    {
        SetHooks();
        int finalIndex = moddedMask.Length - 1;
        while (finalIndex >= 0 && !moddedMask[finalIndex])
        {
            finalIndex--;
        }
        if (finalIndex < 0)
        {
            ProcTypeInterop.SetModdedMask(ref procChainMask, null);
            return;
        }
        bool[] value = new bool[finalIndex + 1];
        for (int i = 0; i <= finalIndex; i++)
        {
            value[i] = moddedMask[i];
        }
        ProcTypeInterop.SetModdedMask(ref procChainMask, value);
    }
    #endregion

    #region Hooks
    internal static void SetHooks()
    {
        if (_hookEnabled)
        {
            return;
        }
        IL.RoR2.ProcChainMask.AppendToStringBuilder += ProcChainMask_AppendToStringBuilder;
        On.RoR2.NetworkExtensions.Write_NetworkWriter_ProcChainMask += NetworkExtensions_Write_NetworkWriter_ProcChainMask;
        On.RoR2.NetworkExtensions.ReadProcChainMask += NetworkExtensions_ReadProcChainMask;
        On.RoR2.ProcChainMask.GetHashCode += ProcChainMask_GetHashCode;
        On.RoR2.ProcChainMask.Equals_ProcChainMask += ProcChainMask_Equals_ProcChainMask;
        _hookEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.ProcChainMask.AppendToStringBuilder -= ProcChainMask_AppendToStringBuilder;
        On.RoR2.NetworkExtensions.Write_NetworkWriter_ProcChainMask -= NetworkExtensions_Write_NetworkWriter_ProcChainMask;
        On.RoR2.NetworkExtensions.ReadProcChainMask -= NetworkExtensions_ReadProcChainMask;
        On.RoR2.ProcChainMask.GetHashCode -= ProcChainMask_GetHashCode;
        On.RoR2.ProcChainMask.Equals_ProcChainMask -= ProcChainMask_Equals_ProcChainMask;
        _hookEnabled = false;
    }

    private delegate bool ProcChainMask_AppendToStringBuilder_Delegate(ref ProcChainMask procChainMask, StringBuilder stringBuilder, bool flag);

    private static void ProcChainMask_AppendToStringBuilder(ILContext il)
    {
        int locFlagIndex = -1;
        ILCursor c = new ILCursor(il);
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdcI4(0),
            x => x.MatchStloc(out locFlagIndex))
            && c.TryGotoNext(MoveType.AfterLabel,
            x => x.MatchLdarg(1),
            x => x.MatchLdstr(")"),
            x => x.MatchCallOrCallvirt<StringBuilder>(nameof(StringBuilder.Append)))
            )
        {
            c.Emit(OpCodes.Ldarg, 0);
            c.Emit(OpCodes.Ldarg, 1);
            c.Emit(OpCodes.Ldloc, locFlagIndex);
            c.EmitDelegate<ProcChainMask_AppendToStringBuilder_Delegate>((ref ProcChainMask procChainMask, StringBuilder stringBuilder, bool flag) =>
            {
                bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
                if (mask != null)
                {
                    for (int i = 0; i < mask.Length; i++)
                    {
                        if (mask[i])
                        {
                            if (flag)
                            {
                                stringBuilder.Append("|");
                            }
                            stringBuilder.Append($"({nameof(ModdedProcType)}){i}");
                            flag = true;
                        }
                    }
                }
                return flag;
            });
            c.Emit(OpCodes.Stloc, locFlagIndex);
        }
        else ProcTypePlugin.Logger.LogError($"{nameof(ProcTypeAPI)}.{nameof(ProcChainMask_AppendToStringBuilder)} IL match failed.");
    }

    private static void NetworkExtensions_Write_NetworkWriter_ProcChainMask(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_ProcChainMask orig, NetworkWriter writer, ProcChainMask procChainMask)
    {
        orig(writer, procChainMask);
        NetworkWriteModdedMask(writer, ProcTypeInterop.GetModdedMask(procChainMask));
    }

    private static ProcChainMask NetworkExtensions_ReadProcChainMask(On.RoR2.NetworkExtensions.orig_ReadProcChainMask orig, NetworkReader reader)
    {
        ProcChainMask result = orig(reader);
        ProcTypeInterop.SetModdedMask(ref result, NetworkReadModdedMask(reader));
        return result;
    }

    private static int ProcChainMask_GetHashCode(On.RoR2.ProcChainMask.orig_GetHashCode orig, ref ProcChainMask self)
    {
        return orig(ref self) * 397 ^ ModdedMaskHashCode(ProcTypeInterop.GetModdedMask(self));
    }

    private static bool ProcChainMask_Equals_ProcChainMask(On.RoR2.ProcChainMask.orig_Equals_ProcChainMask orig, ref ProcChainMask self, ProcChainMask other)
    {
        return orig(ref self, other) && ModdedMaskEquals(ProcTypeInterop.GetModdedMask(self), ProcTypeInterop.GetModdedMask(other));
    }
    #endregion

    #region Internal
    [SystemInitializer]
    private static void Init()
    {
        buffer = new bool[ModdedProcTypeCount];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void NetworkWriteModdedMask(this NetworkWriter writer, bool[] mask)
    {
        int maskIndex = 0;
        for (int i = 0; i < totalByteCount; i++)
        {
            byte b = 0;
            if (mask == null || maskIndex < mask.Length)
            {
                int validBitCount = (i < filledByteCount) ? 8 : remainingBitCount;
                int bitIndex = 0;
                while (bitIndex < validBitCount)
                {
                    if (ArrayUtils.GetSafe(mask, maskIndex))
                    {
                        b |= (byte)(1 << bitIndex);
                    }
                    bitIndex++;
                    maskIndex++;
                }
            }
            writer.Write(b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool[] NetworkReadModdedMask(NetworkReader reader)
    {
        int finalLength = 0;
        int bufferIndex = 0;
        for (int i = 0; i < totalByteCount; i++)
        {
            int validBitCount = (i < filledByteCount) ? 8 : remainingBitCount;
            byte b = reader.ReadByte();
            int j = 0;
            while (j < validBitCount)
            {
                if (buffer[bufferIndex] = (b & (byte)(1 << j)) > 0)
                {
                    finalLength = bufferIndex + 1;
                }
                j++;
                bufferIndex++;
            }
        }
        if (finalLength <= 0)
        {
            return null;
        }
        bool[] result = new bool[finalLength];
        Array.Copy(buffer, result, finalLength);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte ModdedMaskHashCode(bool[] mask)
    {
        byte b = 0;
        if (mask != null)
        {
            int length = Math.Min(mask.Length, 8);
            for (int i = 0; i < length; i++)
            {
                if (mask[i])
                {
                    b |= (byte)(1 << i);
                }
            }
        }
        return b;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ModdedMaskEquals(bool[] a, bool[] b)
    {
        if (a == null)
        {
            return b == null || b.Length == 0;
        }
        if (b == null)
        {
            return a == null || a.Length == 0;
        }
        if (a.Length == b.Length)
        {
            return ArrayUtils.SequenceEquals(a, b);
        }
        int max = Math.Max(a.Length, b.Length);
        for (int i = 0; i < max; i++)
        {
            if (!(ArrayUtils.GetSafe(a, i) == ArrayUtils.GetSafe(b, i)))
            {
                return false;
            }
        }
        return true;
    }
    #endregion
}
