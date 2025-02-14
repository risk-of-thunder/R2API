using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using EntityStates.FalseSonBoss;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API.AutoVersionGen;
using RoR2;

namespace R2API;

///<summary>
/// API for registering custom CostTypeDefs
///</summary>
[AutoVersion]
public static partial class CostAPI {
    public const string PluginGUID = R2API.PluginGUID + ".costtype";
    public const string PluginName = R2API.PluginName + ".CostType";
    private static bool catalogAvailable = false;
    private static List<CostTypeHolder> pendingHolders = new();
    private static bool _hooksEnabled = false;

    internal static void SetHooks() {
        if (_hooksEnabled) return;
        _hooksEnabled = true;
        IL.RoR2.CostTypeCatalog.Init += Init;
    }

    internal static void UnsetHooks() {
        _hooksEnabled = false;
        IL.RoR2.CostTypeCatalog.Init -= Init;
    }

    // we do this to process as early as we can
    private static void Init(ILContext il)
    {
        ILCursor c = new(il);
        
        while (c.TryGotoNext(MoveType.After, x => x.MatchRet())) {
            // progress to the ending return
        }

        c.Index--;
        c.Emit(OpCodes.Call, typeof(CostAPI).GetMethod(nameof(ProcessHolders), BindingFlags.Static | BindingFlags.NonPublic));
    }

    private static void ProcessHolders()
    {
        catalogAvailable = true;

        foreach (CostTypeHolder holder in pendingHolders) {
            CostTypeIndex index = RegisterCostType(holder.CostTypeDef);
            holder._costTypeIndex = index;
            holder.OnReserved(index);
        }

        pendingHolders.Clear();
    }

    /// <summary>
    /// Registers a CostTypeDef to the catalog. CostTypeCatalog must be initialized when calling this.
    /// </summary>
    /// <param name="costTypeDef">The CostTypeDef to register.</param>
    /// <returns>The CostTypeIndex corresponding to your CostTypeDef, or -1 upon failure.</returns>
    public static CostTypeIndex RegisterCostType(CostTypeDef costTypeDef) {
        SetHooks();

        if (!catalogAvailable) {
            CostTypePlugin.Logger.LogError("Attempted to register CostTypeIndex before the CostTypeCatalog has initialized!");
            return (CostTypeIndex)(-1f);
        }

        CostTypeIndex index = (CostTypeIndex)CostTypeCatalog.costTypeDefs.Length;
        Array.Resize(ref CostTypeCatalog.costTypeDefs, (int)index + 1);
        CostTypeCatalog.Register(index, costTypeDef);

        return index;
    }
    
    /// <summary>
    /// Reserves a CostTypeIndex if the catalog has not yet been initialized.
    /// </summary>
    /// <param name="costTypeDef">The CostTypeDef to register.</param>
    /// <param name="onRegistered">A callback to run once the CostTypeCatalog initializes.</param>
    /// <returns>A CostTypeHolder, which will be registered once the CostTypeCatalog has initialized.</returns>
    public static CostTypeHolder ReserveCostType(CostTypeDef costTypeDef, Action<CostTypeIndex> onRegistered) {
        SetHooks();

        CostTypeHolder holder = new() {
            OnReserved = onRegistered,
            CostTypeDef = costTypeDef
        };

        if (catalogAvailable) {
            CostTypeIndex index = RegisterCostType(costTypeDef);
            holder._costTypeIndex = index;
            holder.OnReserved(index);
            return holder;
        }

        pendingHolders.Add(holder);
        return holder;
    }
}