﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

/// <summary>
/// API for reserving custom ProcTypes and interacting with <see cref="ProcChainMask"/>.
/// </summary>
[AutoVersion]
public static partial class ProcTypeAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".proctype";
    public const string PluginName = R2API.PluginName + ".ProcType";

    /// <summary>
    /// The number of modded Proc Types currently reserved by <see cref="ReserveProcType"/>.
    /// </summary>
    public static int ModdedProcTypeCount
    {
        get => _moddedProcTypeCount;
        private set
        {
            if (_moddedProcTypeCount != value)
            {
                _moddedProcTypeCount = value;
                if (byteCount != (byteCount = value + 7 >> 3))
                {
                    buffer = new byte[byteCount];
                }
            }
        }
    }

    private static bool _hookEnabled = false;
    private static int _moddedProcTypeCount;

    #region NetworkSettings
    private static int byteCount;
    private static byte[] buffer;
    #endregion

    #region API
    /// <summary>
    /// Reserve a <see cref="ModdedProcType"/> for use with
    /// <see cref="AddModdedProc(ref ProcChainMask, ModdedProcType)"/>,
    /// <see cref="RemoveModdedProc(ref ProcChainMask, ModdedProcType)"/> and
    /// <see cref="HasModdedProc(ProcChainMask, ModdedProcType)"/>.
    /// </summary>
    /// <returns>A valid <see cref="ModdedProcType"/>.</returns>
    public static ModdedProcType ReserveProcType()
    {
        SetHooks();
        if (ModdedProcTypeCount == int.MaxValue)
        {
            throw new IndexOutOfRangeException($"Reached the ModdedProcType limit ({int.MaxValue})! Please contact R2API developers to increase the limit");
        }

        ModdedProcTypeCount++;
        return (ModdedProcType)ModdedProcTypeCount;
    }

    /// <summary>
    /// Enable a <see cref="ModdedProcType"/> on this <see cref="ProcChainMask"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="procType"/> is invalid</exception>
    public static void AddModdedProc(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        if (procType <= ModdedProcType.Invalid || (int)procType > ModdedProcTypeCount)
        {
            ProcTypePlugin.Logger.LogError($"Parameter '{nameof(procType)}' with value {procType} is out of range of registered types (1-{ModdedProcTypeCount})\n{new StackTrace(true)}");
            return;
        }

        SetHooks();

        procType--;

        byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        byte[] value;
        int i = (int)procType >> 3;

        if (mask is null)
        {
            value = new byte[i + 1];
            value[i] = GetMaskingBit(i, procType);
        }
        else if (mask.Length > i) // current mask is large enough to set procType
        {
            byte b = (byte)(mask[i] | GetMaskingBit(i, procType)); // relevant byte with procType enabled
            if (b == mask[i])
            {
                return; // procType was already enabled, no need to create a new array
            }
            value = ArrayUtils.Clone(mask); // ensure mask is treated as immutable
            value[i] = b;
        }
        else
        {
            value = mask;
            Array.Resize(ref value, i + 1);
            value[i] = GetMaskingBit(i, procType);
        }

        ProcTypeInterop.SetModdedMask(ref procChainMask, value);
    }

    /// <summary>
    /// Disable a <see cref="ModdedProcType"/> on this <see cref="ProcChainMask"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="procType"/> is invalid</exception>
    public static void RemoveModdedProc(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        if (procType <= ModdedProcType.Invalid || (int)procType > ModdedProcTypeCount)
        {
            ProcTypePlugin.Logger.LogError($"Parameter '{nameof(procType)}' with value {procType} is out of range of registered types (1-{ModdedProcTypeCount})\n{new StackTrace(true)}");
            return;
        }

        SetHooks();

        procType--;

        byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask == null)
        {
            return; // mask is 0
        }

        int i = (int)procType >> 3;
        if (mask.Length <= i)
        {
            return; // mask is not long enough for procType to be enabled
        }

        byte b = (byte)(mask[i] & ~GetMaskingBit(i, procType)); // relevant byte with procType disabled
        if (b == mask[i])
        {
            return; // procType was already disabled, no need to make a new array
        }

        if (b == 0 && mask.Length == i + 1) // new byte is empty trailing data
        {
            var newLength = 0;
            for (var j = mask.Length - 2; j >= 0; j--)
            {
                if (mask[j] != 0)
                {
                    newLength = j + 1;
                    break;
                }
            }

            if (newLength == 0)
            {
                mask = null; // mask no longer holds any information
            }
            else
            {
                Array.Resize(ref mask, newLength); // disable procType by resizing to removing relevant byte
            }
        }
        else
        {
            mask = ArrayUtils.Clone(mask); // ensure mask is treated as immutable
            mask[i] = b;
        }

        ProcTypeInterop.SetModdedMask(ref procChainMask, mask);
    }

    /// <summary>
    /// Check if a <see cref="ModdedProcType"/> is enabled on this <see cref="ProcChainMask"/>
    /// </summary>
    /// <returns>true if the <see cref="ModdedProcType"/> is enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="procType"/> is invalid</exception>
#pragma warning disable R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    public static bool HasModdedProc(this ProcChainMask procChainMask, ModdedProcType procType)
#pragma warning restore R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    {
        if (procType <= ModdedProcType.Invalid || (int)procType > ModdedProcTypeCount)
        {
            ProcTypePlugin.Logger.LogError($"Parameter '{nameof(procType)}' with value {procType} is out of range of registered types (1-{ModdedProcTypeCount})\n{new StackTrace(true)}");
            return false;
        }

        procType--;

        byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask == null)
        {
            return false;
        }

        int i = (int)procType >> 3;
        return mask.Length > i && (mask[i] & GetMaskingBit(i, procType)) > 0;
    }

    /// <summary>
    /// Access a <see cref="BitArray"/> that represents the <see cref="ProcTypeAPI"/> equivalent of <see cref="ProcChainMask.mask"/>. 
    /// </summary>
    /// <returns>A <see cref="BitArray"/> with length equal to <see cref="ModdedProcTypeCount"/> that is equivalent to the underlying modded procs mask.</returns>
#pragma warning disable R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    public static BitArray GetModdedMask(ProcChainMask procChainMask)
#pragma warning restore R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    {
        BitArray result = GetModdedMaskRaw(procChainMask);
        result.Length = ModdedProcTypeCount;
        return result;
    }

    /// <inheritdoc cref="GetModdedMask(ProcChainMask)"/>
    /// <remarks>This overload allows reuse of a single <see cref="BitArray"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="dest"/> is null.</exception>
#pragma warning disable R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    public static void GetModdedMask(ProcChainMask procChainMask, BitArray dest)
#pragma warning restore R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    {
        GetModdedMaskRaw(procChainMask, dest);
        dest.Length = ModdedProcTypeCount;
    }

    /// <summary>
    /// Access a <see cref="BitArray"/> of that represents the <see cref="ProcTypeAPI"/> equivalent of <see cref="ProcChainMask.mask"/>; this variant does not normalize <see cref="BitArray.Length"/>.
    /// </summary>
    /// <returns>A <see cref="BitArray"/> of arbitrary length that is equivalent to the underlying modded procs mask.</returns>
    public static BitArray GetModdedMaskRaw(ProcChainMask procChainMask)
    {
        SetHooks();
        byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask == null)
        {
            return new BitArray(0);
        }
        return new BitArray(mask);
    }

    /// <inheritdoc cref="GetModdedMaskRaw(ProcChainMask)"/>
    /// <remarks>This overload allows reuse of a single <see cref="BitArray"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="dest"/> is null.</exception>
    public static void GetModdedMaskRaw(ProcChainMask procChainMask, BitArray dest)
    {
        if (dest == null)
        {
            throw new ArgumentNullException(nameof(dest));
        }

        SetHooks();
        byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask != null)
        {
            dest.Length = mask.Length << 3; // eight times mask length
            for (int maskIndex = 0; maskIndex < mask.Length; maskIndex++)
            {
                byte b = mask[maskIndex]; // read one byte
                int i = maskIndex << 3;
                dest[i]     = (b & 1) > 0;
                dest[i + 1] = (b & (1 << 1)) > 0;
                dest[i + 2] = (b & (1 << 2)) > 0;
                dest[i + 3] = (b & (1 << 3)) > 0;
                dest[i + 4] = (b & (1 << 4)) > 0;
                dest[i + 5] = (b & (1 << 5)) > 0;
                dest[i + 6] = (b & (1 << 6)) > 0;
                dest[i + 7] = (b & (1 << 7)) > 0;
            }
        }
        else
        {
            dest.Length = 0;
        }
    }

    /// <summary>
    /// Assign the <see cref="ProcTypeAPI"/> equivalent of <see cref="ProcChainMask.mask"/>. 
    /// </summary>
    /// <param name="procChainMask"></param>
    /// <param name="value">A <see cref="BitArray"/> representing a modded procs mask.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static void SetModdedMask(ref ProcChainMask procChainMask, BitArray value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }
        SetHooks();

        var length = 0;
        for (var i = value.Length - 1; i >= 0; i--)
        {
            if (value[i])
            {
                length = (i + 7) >> 3;
                break;
            }
        }

        if (length > 0)
        {
            byte[] array = new byte[length]; // minimum bytes to store data
            value.CopyTo(buffer, 0);
            Array.Copy(buffer, 0, array, 0, length);
            ProcTypeInterop.SetModdedMask(ref procChainMask, array);
        }
        else
        {
            ProcTypeInterop.SetModdedMask(ref procChainMask, null);
        }
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
                byte[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
                if (mask != null)
                {
                    for (int i = 0; i < mask.Length; i++)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if ((mask[i] & (1 << j)) > 0)
                            {
                                if (flag)
                                {
                                    stringBuilder.Append("|");
                                }
                                stringBuilder.Append($"({nameof(ModdedProcType)}){(i << 3) + j}");
                                flag = true;
                            }
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
    /// <summary>
    /// Util used by <see cref="AddModdedProc(ref ProcChainMask, ModdedProcType)"/>, <see cref="RemoveModdedProc(ref ProcChainMask, ModdedProcType)"/>, and <see cref="HasModdedProc(ProcChainMask, ModdedProcType)"/> to find the masking bit for a <see cref="ModdedProcType"/> given a byte index.
    /// </summary>
    /// <param name="maskIndex">Relevant byte index in a modded mask.</param>
    /// <param name="procType"></param>
    /// <returns>A byte with one bit flagged.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte GetMaskingBit(int maskIndex, ModdedProcType procType) => (byte)(1 << ((int)procType - (maskIndex << 3)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void NetworkWriteModdedMask(this NetworkWriter writer, byte[] mask)
    {
        var length = mask?.Length ?? 0;
        if (ModdedProcTypeCount <= byte.MaxValue)
        {
            writer.Write((byte)length);
        }
        else if (ModdedProcTypeCount <= ushort.MaxValue)
        {
            writer.Write((ushort)length);
        }
        else
        {
            writer.Write(length);
        }

        if (length != 0)
        {
            writer.Write(mask, length);
        }
    }

    /// <returns>A mask trimmed to remove irrelevant trailing bytes, or null for a mask of 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte[] NetworkReadModdedMask(NetworkReader reader)
    {
        var length = ModdedProcTypeCount switch
        {
            <= byte.MaxValue => reader.ReadByte(),
            <= ushort.MaxValue => reader.ReadUInt16(),
            _ => reader.ReadInt32()
        };

        if (length <= 0)
        {
            return null;
        }

        return reader.ReadBytes(length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ModdedMaskHashCode(byte[] mask)
    {
        var hash = 0;
        if (mask is not null)
        {
            for (var i = 0; i < mask.Length; i++)
            {
                hash = HashCode.Combine(hash, mask[i]);
            }
        }

        return hash;
    }

    /// <summary>
    /// Compare two masks while ignoring length, null is treated as 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ModdedMaskEquals(byte[] a, byte[] b)
    {
        return ArrayUtils.SequenceEquals(a ?? [], b ?? []);
    }

    #endregion
}
