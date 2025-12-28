using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Utils;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using UnityEngine.AddressableAssets;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API;

// ReSharper disable once InconsistentNaming
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class EliteAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".elites";
    public const string PluginName = R2API.PluginName + ".Elites";

    public static ObservableCollection<CustomElite> EliteDefinitions = [];

    public static CombatDirector.EliteTierDef[] VanillaEliteTiers
    {
        get =>_vanillaEliteTiers;
        private set
        {
            _vanillaEliteTiers = value;
            VanillaEliteTierCount = value?.Length ?? 0;
        }
    }

    public static CombatDirector.EliteTierDef VanillaFirstTierDef => GetVanillaEliteTierDef(VanillaEliteTier.BaseTier1);
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef => GetVanillaEliteTierDef(VanillaEliteTier.BaseTier1Honor);
    public static CombatDirector.EliteTierDef GetVanillaEliteTierDef(VanillaEliteTier tier) => HG.ArrayUtils.GetSafe(VanillaEliteTiers, (int)tier);
    public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

    public static int VanillaEliteTierCount;

    private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = [];
    private static CombatDirector.EliteTierDef[] _vanillaEliteTiers = [];

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static bool _resolvedFields = false;

    #region ModHelper Events and Hooks

    internal static void Init()
    {
        // im just lazy and want the ILContext
        IL.RoR2.CombatDirector.Init += ResolveFieldInfo;
        IL.RoR2.CombatDirector.Init -= ResolveFieldInfo;

        On.RoR2.CombatDirector.Init += CopyCombatDirectorTiers;
        IL.RoR2.CombatDirector.Init += InitEarlyCombatDirector;

        // call init before anyone else places hooks
        // wrb expects the elite catalog to be populated when init is called
        CombatDirector.Init();
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CombatDirector.Init -= InitEarlyCombatDirector;
        On.RoR2.CombatDirector.Init -= CopyCombatDirectorTiers;
    }

    private static void CopyCombatDirectorTiers(On.RoR2.CombatDirector.orig_Init orig)
    {
        orig();

        if (VanillaEliteTierCount == 0)
        {
            VanillaEliteTiers = [.. CombatDirector.eliteTiers];
        }

        AddElitesToGame();
    }

    private static void InitEarlyCombatDirector(ILContext il)
    {
        var c = new ILCursor(il);
        int idx = 0;

        while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(out idx),
                x => x.MatchNewobj<CombatDirector.EliteTierDef>()
            ))
        {
            c.Emit(OpCodes.Ldc_I4, idx);
            c.EmitDelegate(UseExistingTierDef);
        }
    }

    private static CombatDirector.EliteTierDef UseExistingTierDef(CombatDirector.EliteTierDef tierDef, int index) => HG.ArrayUtils.GetSafe(VanillaEliteTiers, index, tierDef);

    private static void ResolveFieldInfo(ILContext il)
    {
        if (_resolvedFields)
            return;

        if (!TryLoadTokensFromFile(out Dictionary<string, string> assetNameToGuid))
            return;

        var c = new ILCursor(il);
        FieldReference fieldRef = null;

        // populate null static fields
        while (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld(out fieldRef)))
        {
            if (!assetNameToGuid.TryGetValue(fieldRef.Name, out var addressableGuid))
                continue;

            var addressable = Addressables.LoadAssetAsync<EliteDef>(addressableGuid).WaitForCompletion();
            if (addressable is null)
            {
                ElitesPlugin.Logger.LogWarning("Failed to load addressable " + fieldRef.Name + " | " + addressableGuid);
                continue;
            }

            var fieldInfo = fieldRef.ResolveReflection();
            if (fieldInfo.GetValue(null) is null)
                fieldInfo.SetValue(null, addressable);
        }

        _resolvedFields = true;
    }


    private static bool TryLoadTokensFromFile(out Dictionary<string, string> assetNameToGuid)
    {
        assetNameToGuid = null;

        try
        {
            var bepPackPath = typeof(GameAssetPathsSerde).Assembly.Location;
            bepPackPath = Directory.GetParent(bepPackPath).FullName;
            bepPackPath = System.IO.Path.Combine(bepPackPath, "GameAssetPaths.bin");
            GameAssetPathsSerde.Deserialize(bepPackPath, out var paths, out var guids);

            var regex = new Regex("RoR2.*/ed[A-Z].*asset");

            assetNameToGuid = new Dictionary<string, string>();
            for (int i = 0; i < paths.Length; i++)
            {
                var key = paths[i];
                if (!regex.IsMatch(key))
                    continue;

                // ignore the "ed" prefix and the ".asset" postfix
                var asset = key.Split('/')[^1][2..^6];

                assetNameToGuid[asset] = guids[i];
            }

            return true;
        }
        catch (Exception e)
        {
            ElitesPlugin.Logger.LogError("Failed to load elite addressable tokens from file: " + e);
        }

        return false;
    }

    #endregion ModHelper Events and Hooks

    #region Add Methods

    /// <summary>
    /// Add a custom elite to the elite catalog.
    /// If this is called after the <see cref="EliteCatalog"/> inits then this will throw.
    /// <see cref="CustomElite.EliteTierDefs"/> shouldnt be empty if you want to see your custom elite appear in the game.
    /// You can also directly modify <see cref="CombatDirector.eliteTiers"/>.
    /// Please check the constructors docs of <see cref="CustomElite"/> for more information.
    /// </summary>
    /// <param name="elite">The elite to add.</param>
    /// <returns>true if added, false otherwise</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Add(CustomElite? elite)
    {
        return AddInternal(elite, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Allows for adding a new elite tier def to the combat director during runtime.
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    public static int AppendCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef)
    {
        return AddCustomEliteTier(eliteTierDef, -1);
    }

    /// <summary>
    /// Allows for adding a new elite tier def to the combat director.
    /// Automatically insert the eliteTierDef at the correct position in the array based on its <see cref="CombatDirector.EliteTierDef.costMultiplier"/>
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    /// <returns>Index inserted at, or -1 if the operation failed</returns>
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef)
    {
        if (eliteTierDef is null)
        {
            ElitesPlugin.Logger.LogError("EliteTierDef cannot be null");
            return -1;
        }

        var indexToInsertAt = Array.FindIndex(GetCombatDirectorEliteTiers(), x => x.costMultiplier >= eliteTierDef.costMultiplier);
        return AddCustomEliteTier(eliteTierDef, indexToInsertAt);
    }

    /// <summary>
    /// Allows for adding a new <see cref="CombatDirector.EliteTierDef"/> at the given index in <see cref="GetCombatDirectorEliteTiers"/>
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    /// <param name="indexToInsertAt">Optional index to specify if you want to insert a cheaper elite tier</param>
    /// <returns>Index inserted at, or -1 if the operation failed</returns>
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef, int indexToInsertAt = -1)
    {
        if (eliteTierDef is null)
        {
            ElitesPlugin.Logger.LogError("EliteTierDef cannot be null");
            return -1;
        }

        eliteTierDef.eliteTypes ??= [];

        var currentEliteTiers = GetCombatDirectorEliteTiers();
        if (currentEliteTiers != null)
        {
            if (indexToInsertAt == -1)
                indexToInsertAt = currentEliteTiers.Length;

            HG.ArrayUtils.ArrayInsert(ref currentEliteTiers, indexToInsertAt, eliteTierDef);

            OverrideCombatDirectorEliteTiers(currentEliteTiers);
        }

        CustomEliteTierDefs.Add(eliteTierDef);

        ElitesPlugin.Logger.LogInfo($"Custom Elite Tier : (Index : {indexToInsertAt}) added");

        return indexToInsertAt;
    }


    internal static bool AddInternal(CustomElite? customElite, Assembly addingAssembly)
    {
        if (customElite?.EliteDef == null)
        {
            throw new ArgumentNullException("customElite.EliteDef");
        }

        if (!CatalogBlockers.GetAvailability<EliteDef>())
        {
            ElitesPlugin.Logger.LogError($"Too late ! Tried to add elite: {customElite.EliteDef.modifierToken} after the EliteCatalog has initialized!");
            return false;
        }

        R2APIContentManager.HandleContentAddition(addingAssembly, customElite.EliteDef);
        EliteDefinitions.Add(customElite);

        return true;
    }

    private static void AddElitesToGame()
    {
        foreach (var customElite in EliteDefinitions)
        {
            if (customElite.EliteRamp)
                EliteRamp.AddRamp(customElite.EliteDef, customElite.EliteRamp!);

            foreach (var tierDefToAdd in customElite.EliteTierDefs)
            {
                if (Array.IndexOf(tierDefToAdd.eliteTypes, customElite.EliteDef) == -1)
                    HG.ArrayUtils.ArrayAppend(ref tierDefToAdd.eliteTypes, customElite.EliteDef);
            }
        }
    }

    #endregion Add Methods

    #region Combat Director Modifications

    /// <summary>
    /// Used for ensuring correct tier placement when creating a new <see cref="CustomElite"/>. When given <see cref="VanillaEliteTier.BaseTier1"/>,
    /// the list will also include <see cref="VanillaEliteTier.FullTier1"/> in the result to ensure the elite doesn't stop spawning after stage 2.
    /// </summary>
    /// <returns> All vanilla <see cref="CombatDirector.EliteTierDef"/> that the elite can appear in, or empty for None. </returns>
    public static IEnumerable<CombatDirector.EliteTierDef> GetEliteTierEnumerable(VanillaEliteTier tier) => tier switch
    {
        VanillaEliteTier.None => [],
        VanillaEliteTier.BaseTier1 => [GetVanillaEliteTierDef(tier), GetVanillaEliteTierDef(VanillaEliteTier.FullTier1)],
        VanillaEliteTier.BaseTier1Honor => [GetVanillaEliteTierDef(tier), GetVanillaEliteTierDef(VanillaEliteTier.FullTier1Honor)],
        _ => [GetVanillaEliteTierDef(tier)]
    };

    /// <summary>
    /// <para>Retrieves the honor compatible <see cref="CombatDirector.EliteTierDef"/> for the given tier. <see cref="VanillaEliteTier.BaseTier1"/> and <see cref="VanillaEliteTier.FullTier1"/> will be changed to their Honor variants.</para>
    /// Used for ensuring correct tier placement when creating a new <see cref="CustomElite"/>. When given <see cref="VanillaEliteTier.BaseTier1Honor"/>,
    /// the list will also include <see cref="VanillaEliteTier.FullTier1Honor"/> in the result to ensure the elite doesn't stop spawning after stage 2.
    /// </summary>
    /// <returns> All vanilla honor <see cref="CombatDirector.EliteTierDef"/> that the elite can appear in, or empty for None, Tier2 and Lunar. </returns>
    public static IEnumerable<CombatDirector.EliteTierDef> GetHonorEliteTierEnumerable(VanillaEliteTier tier) => tier switch
    {
        VanillaEliteTier.BaseTier1 or VanillaEliteTier.BaseTier1Honor => [GetVanillaEliteTierDef(VanillaEliteTier.BaseTier1Honor), GetVanillaEliteTierDef(VanillaEliteTier.FullTier1Honor)],
        VanillaEliteTier.FullTier1 or VanillaEliteTier.FullTier1Honor => [GetVanillaEliteTierDef(VanillaEliteTier.FullTier1Honor)],
        _ => [],
    };

    /// <summary>
    /// Returns the current <see cref="CombatDirector.eliteTiers"/> used by the Combat Director for doing its elite spawning while doing a run.
    /// </summary>
    public static CombatDirector.EliteTierDef[] GetCombatDirectorEliteTiers()
    {
        return CombatDirector.eliteTiers;
    }

    /// <summary>
    /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
    /// You can get the current array used by the director with <see cref="GetCombatDirectorEliteTiers"/>
    /// </summary>
    /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
    public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef[] newEliteTiers)
    {
        CombatDirector.eliteTiers = newEliteTiers;
    }
    #endregion Combat Director Modifications
}
