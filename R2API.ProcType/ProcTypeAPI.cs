using System;
using System.Collections.Generic;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using RoR2;
using UnityEngine;

namespace R2API;

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class ProcTypeAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".proctype";
    public const string PluginName = R2API.PluginName + ".ProcType";

    public static int ModdedProcTypeCount { get; private set; }

    private static bool _hookEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hookEnabled)
        {
            return;
        }

        _hookEnabled = true;
    }

    internal static void UnsetHooks()
    {

        _hookEnabled = false;
    }
    #endregion

    #region Public
    public static void AddModdedProcType(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        SetHooks();
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        bool[] value = new bool[Math.Max(mask.Length, (int)procType + 1)];
        Array.Copy(mask, value, mask.Length);
        value[(int)procType] = true;
        ProcTypeInterop.SetModdedMask(ref procChainMask, value);
    }

    public static void RemoveModdedProcType(ref this ProcChainMask procChainMask, ModdedProcType procType)
    {
        SetHooks();
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        if (mask != null && ArrayUtils.IsInBounds(mask, (int)procType))
        {
            mask = ArrayUtils.Clone(mask);
            mask[(int)procType] = false;
            ProcTypeInterop.SetModdedMask(ref procChainMask, mask);
            /*Array.Copy(moddedMask, value, procTypeIndex);
            int nextIndex = procTypeIndex + 1;
            if (nextIndex < moddedMask.Length)
            {
                Array.Copy(moddedMask, nextIndex, value, nextIndex, moddedMask.Length - nextIndex);
            }*/
        }
    }

#pragma warning disable R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    public static bool HasModdedProcType(this ProcChainMask procChainMask, ModdedProcType procType)
#pragma warning restore R2APISubmodulesAnalyzer // Public API Method is not enabling the hooks if needed.
    {
        bool[] mask = ProcTypeInterop.GetModdedMask(procChainMask);
        return mask != null && ArrayUtils.GetSafe(mask, (int)procType);
    }
    #endregion

    #region Private

    #endregion
}
