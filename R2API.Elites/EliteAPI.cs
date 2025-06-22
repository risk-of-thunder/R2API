using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using SimpleJSON;
using UnityEngine;
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
    public static CombatDirector.EliteTierDef[] VanillaEliteTiers { get; private set; }
    public static CombatDirector.EliteTierDef VanillaFirstTierDef => GetVanillaEliteTierDef(VanillaEliteTier.BaseTier1);
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef => GetVanillaEliteTierDef(VanillaEliteTier.BaseTier1Honor);
    public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

    public static int VanillaEliteTierCount;

    private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = [];
    private static Dictionary<string, string> _assetNameToGuid = [];

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static bool _hooksEnabled = false;

    #region ModHelper Events and Hooks

    internal static void SetHooks()
    {
        if (_hooksEnabled)
            return;

        var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "lrapi_returns.json");
        LoadTokensFromFile(filePath);

        IL.RoR2.CombatDirector.Init += CombatDirector_Init;
        R2APIContentPackProvider.WhenAddingContentPacks += AddElitesToGame;

        CombatDirectorInitNoTimingIssue();

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CombatDirector.Init -= CombatDirector_Init;
        R2APIContentPackProvider.WhenAddingContentPacks -= AddElitesToGame;

        _hooksEnabled = false;
    }

    private static void CombatDirectorInitNoTimingIssue()
    {
        CombatDirector.Init();

        VanillaEliteTiers = [.. CombatDirector.eliteTiers];
        VanillaEliteTierCount = VanillaEliteTiers.Length;
    }

    private static void LoadTokensFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ElitesPlugin.Logger.LogError(filePath + " doesnt exist");
            return;
        }

        using Stream stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
        JSONNode jSONNode = JSON.Parse(streamReader.ReadToEnd());
        if (jSONNode == null)
        {
            ElitesPlugin.Logger.LogError("json is null");
            return;
        }

        var regex = new Regex("RoR2.*/ed[A-Z].*asset", RegexOptions.Compiled);

        _assetNameToGuid = new Dictionary<string, string>(
            from key in jSONNode.Keys
            where regex.Match(key).Success
            let asset = key.Split('/')[^1][2..^6]
            select new KeyValuePair<string, string>(asset, jSONNode[key].Value));

        ElitesPlugin.Logger.LogDebug($"{nameof(CombatDirector_Init)} | Able to apply addressable overrides for {_assetNameToGuid.Count} elite defs");
    }

    private static void CombatDirector_Init(ILContext il)
    {
        var c = new ILCursor(il);
        var cList = new List<(ILCursor cursor, string addressableGuid)>();

        while (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld(out var fld) && fld.FieldType.Is(typeof(EliteDef))
            ))
        {
            if (c.Prev.Operand is not FieldReference field || string.IsNullOrEmpty(field.Name))
            {
                ElitesPlugin.Logger.LogError($"how did you manage to match with a null field ref?\r\n{c}");
                continue;
            }

            if (!_assetNameToGuid.TryGetValue(field.Name, out var addressableGuid))
            {
                ElitesPlugin.Logger.LogError($"The addressable path {field.Name} is invalid! Skipping IL edit for this section!");
                continue;
            }

            cList.Add((c.Clone(), addressableGuid));
        }

        foreach ((var cursor, var addressableGuid) in cList)
        {
            cursor.Emit(OpCodes.Ldstr, addressableGuid);
            cursor.EmitDelegate(LazyNullCheck);
        }

        ElitesPlugin.Logger.LogDebug($"{nameof(CombatDirector_Init)} | Applied addressable overrides for {cList.Count} elite defs");
    }

    private static EliteDef LazyNullCheck(EliteDef origDef, string addressableGuid) => origDef ?? Addressables.LoadAssetAsync<EliteDef>(addressableGuid).WaitForCompletion();

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
        EliteAPI.SetHooks();

        return AddInternal(elite, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Allows for adding a new elite tier def to the combat director during runtime.
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    public static int AppendCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef)
    {
        EliteAPI.SetHooks();

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
        EliteAPI.SetHooks();

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
        EliteAPI.SetHooks();

        if (eliteTierDef is null)
        {
            ElitesPlugin.Logger.LogError("EliteTierDef cannot be null");
            return -1;
        }

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


    internal static bool AddInternal(CustomElite customElite, Assembly addingAssembly)
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
                EliteRamp.AddRamp(customElite.EliteDef, customElite.EliteRamp);

            foreach (var tierDefToAdd in customElite.EliteTierDefs)
            {
                tierDefToAdd.eliteTypes ??= [];

                if (Array.IndexOf(tierDefToAdd.eliteTypes, customElite.EliteDef) == -1)
                    HG.ArrayUtils.ArrayAppend(ref tierDefToAdd.eliteTypes, customElite.EliteDef);
            }
        }
    }

    #endregion Add Methods

    #region Combat Director Modifications

    /// <summary>
    /// Returns the vanilla <see cref="CombatDirector.EliteTierDef"/> for the given <see cref="VanillaEliteTier"/>
    /// </summary>
    public static CombatDirector.EliteTierDef GetVanillaEliteTierDef(VanillaEliteTier tier)
    {
        EliteAPI.SetHooks();

        return VanillaEliteTiers[(int)tier];
    }

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
        EliteAPI.SetHooks();

        return CombatDirector.eliteTiers;
    }

    /// <summary>
    /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
    /// You can get the current array used by the director with <see cref="GetCombatDirectorEliteTiers"/>
    /// </summary>
    /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
    public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef[] newEliteTiers)
    {
        EliteAPI.SetHooks();

        CombatDirector.eliteTiers = newEliteTiers;
    }
    #endregion Combat Director Modifications
}
