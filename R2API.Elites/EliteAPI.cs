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
        var filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "lrapi_returns.json");
        LoadTokensFromFile(filePath);

        CombatDirector.Init();

        VanillaEliteTiers = [..CombatDirector.eliteTiers];
        VanillaFirstTierDef = VanillaEliteTiers[1];
        VanillaEliteOnlyFirstTierDef = VanillaEliteTiers[2];
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

        var regex = new Regex("RoR2.*/ed[A-Z].*\\.asset", RegexOptions.Compiled);

        _assetNameToGuid = new Dictionary<string, string>(
            from key in jSONNode.Keys
            where regex.Match(key).Success
            let asset = key.Split('/')[^1][2..^6]
            select new KeyValuePair<string, string>(asset, jSONNode[key].Value));
    }

    private static void CombatDirector_Init(ILContext il)
    {
        GetVanillaEliteTierCount(new ILCursor(il));
        ApplyAddressableOverrides(new ILCursor(il));
    }

    private static void GetVanillaEliteTierCount(ILCursor c)
    {
        if (!c.TryGotoNext(
                i => i.MatchLdcI4(out VanillaEliteTierCount),
                i => i.MatchNewarr<CombatDirector.EliteTierDef>()))
        {
            ElitesPlugin.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaEliteTierCount IL Hook");
        }
    }

    private static void ApplyAddressableOverrides(ILCursor c)
    {
        var cList = new List<(ILCursor cursor, string addressableGuid)>();

        while (c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld(out var fld) && fld.FieldType.Is(typeof(EliteDef))
            ))
        {
            if (!(c.Prev.Operand is FieldReference field && field.Name is not null))
            {
                ElitesPlugin.Logger.LogError($"how did you manage to match with a null field ref?\r\n{c}");
                continue;
            }

            if (!_assetNameToGuid.TryGetValue(field.Name, out var addressableGuid))
            {
                ElitesPlugin.Logger.LogError($"The addressable path {field.Name} is invalid! Skipping IL edit for this section!");
                continue;
            }

            ElitesPlugin.Logger.LogDebug($"{nameof(CombatDirector_Init)} | Applying addressable overrides for {field.Name}");

            cList.Add((c.Clone(), addressableGuid));
        }

        foreach ((var cursor, var addressableGuid) in cList)
        {
            c.Emit(OpCodes.Ldstr, addressableGuid);
            c.EmitDelegate(LazyNullCheck);
        }

        ElitesPlugin.Logger.LogDebug($"{nameof(CombatDirector_Init)} | Applied addressable overrides for {cList} elite defs");
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
    /// Allows for adding a new elite tier def to the combat director.
    /// When adding a new elite tier,
    /// do not fill the eliteTypes field with your custom elites defs if your goal is to add your custom elite in it right after.
    /// Because when doing EliteAPI.Add, the API will add your elite to the specified tiers <see cref="CustomElite.EliteTierDefs"/>.
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
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef)
    {
        EliteAPI.SetHooks();

        if (eliteTierDef == null)
        {
            throw new ArgumentNullException("customElite.EliteTierDefs");
        }

        var indexToInsertAt = Array.FindIndex(GetCombatDirectorEliteTiers(), x => x.costMultiplier >= eliteTierDef.costMultiplier);
        return AddCustomEliteTier(eliteTierDef, indexToInsertAt);
    }

    /// <summary>
    /// Allows for adding a new <see cref="CombatDirector.EliteTierDef"/> at the given index in <see cref="GetCombatDirectorEliteTiers"/>
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    /// <param name="indexToInsertAt">Optional index to specify if you want to insert a cheaper elite tier</param>
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef, int indexToInsertAt = -1)
    {
        EliteAPI.SetHooks();

        if (eliteTierDef == null)
        {
            throw new ArgumentNullException("customElite.EliteTierDefs");
        }

        var currentEliteTiers = GetCombatDirectorEliteTiers();
        if (currentEliteTiers != null)
        {
            if (indexToInsertAt == -1)
            {
                indexToInsertAt = currentEliteTiers.Length;
            }

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

        if (customElite.EliteTierDefs?.Any() != true)
        {
            throw new ArgumentNullException("customElite.EliteTierDefs");
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

    public static CombatDirector.EliteTierDef[] VanillaEliteTiers { get; private set; }
    public static CombatDirector.EliteTierDef VanillaFirstTierDef { get; private set; }
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef { get; private set; }
    public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

    public static int VanillaEliteTierCount;

    private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = [];

    /// <summary>
    /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
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
