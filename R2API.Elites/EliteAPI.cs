using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
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

    public static ObservableCollection<CustomElite?>? EliteDefinitions = new ObservableCollection<CustomElite?>();

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
        {
            return;
        }

        IL.RoR2.CombatDirector.Init += CombatDirector_Init;
        R2APIContentPackProvider.WhenAddingContentPacks += AddElitesToGame;

        _hooksEnabled = true;

        // run this after, if it fails catastrophically we dont need it breaking more than once
        CombatDirectorInitNoTimingIssue();
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CombatDirector.Init -= CombatDirector_Init;
        R2APIContentPackProvider.WhenAddingContentPacks -= AddElitesToGame;

        _hooksEnabled = false;
    }

    internal static void CombatDirectorInitNoTimingIssue()
    {
        CombatDirector.Init();

        VanillaEliteTiers = RetrieveVanillaEliteTiers();
        VanillaFirstTierDef = RetrieveVanillaTier(1, "FirstTier");
        VanillaEliteOnlyFirstTierDef = RetrieveVanillaTier(2, "EliteOnlyFirstTier");
        VanillaEliteOnlyFirstExtendedTierDef = RetrieveVanillaTier(3, "EliteOnlyFirstExtendedTier");
        VanillaFirstExtendedTierDef = RetrieveVanillaTier(4, "FirstExtendedTier");
        VanillaSecondTierDef = RetrieveVanillaTier(5, "SecondTier");
        VanillaLunarTierDef = RetrieveVanillaTier(6, "LunarTier");
    }

    internal static void SetEliteTierDefNameInternal(CombatDirector.EliteTierDef eliteTierDef, string name)
    {
        if (eliteTierDef is null)
        {
            ElitesPlugin.Logger.LogError($"Error setting name of {name ?? "<NULL>"}");
            return;
        }

        ElitesInterop.SetEliteTierDefName(eliteTierDef, name);
    }

    internal static string GetEliteTierDefNameInternal(CombatDirector.EliteTierDef eliteTierDef)
    {
        if (eliteTierDef is null)
        {
            ElitesPlugin.Logger.LogError($"Error getting name of null EliteTierDef");
            return "";
        }

        return ElitesInterop.GetEliteTierDefName(eliteTierDef);
    }

    private static CombatDirector.EliteTierDef[] RetrieveVanillaEliteTiers() => CombatDirector.eliteTiers;

    private static CombatDirector.EliteTierDef RetrieveVanillaTier(int index, string name)
    {
        var tier = CombatDirector.eliteTiers[index];
        tier.SetName(name);

        ElitesPlugin.Logger.LogDebug($"Set name of {index} to {tier.GetName()}");

        return tier;
    }

    private static void AddElitesToGame()
    {
        foreach (var customElite in EliteDefinitions)
        {
            foreach (var eliteTierDef in customElite.EliteTierDefs)
            {
                if (eliteTierDef.eliteTypes == null)
                {
                    eliteTierDef.eliteTypes = Array.Empty<EliteDef>();
                }
                else
                {
                    var isCustomEliteAlreadyInEliteTierDef = eliteTierDef.eliteTypes.Any(e => e == customElite.EliteDef);
                    if (isCustomEliteAlreadyInEliteTierDef)
                    {
                        continue;
                    }
                }

                HG.ArrayUtils.ArrayAppend(ref eliteTierDef.eliteTypes, customElite.EliteDef);
            }

            if (customElite.EliteRamp)
            {
                EliteRamp.AddRamp(customElite.EliteDef, customElite.EliteRamp);
            }
        }
    }

    private static void CombatDirector_Init(ILContext il)
    {
        if (!new ILCursor(il).TryGotoNext(
                i => i.MatchLdcI4(out VanillaEliteTierCount),
                i => i.MatchNewarr<CombatDirector.EliteTierDef>()))
        {
            ElitesPlugin.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaEliteTierCount IL Hook");
        }

        int i = 0;
        var c = new ILCursor(il);

        while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld(out var fld) && fld.FieldType.Is(typeof(EliteDef))
            ))
        {
            i++;
            if (i > 100)
            {
                ElitesPlugin.Logger.LogError("Some kind of cracked out recursion is going on! There's no way vanilla has over 100 elites, right???");
                return;
            }

            var stringToLoad = GetAddressableName(c.Prev.Operand as FieldReference);
            if (Addressables.LoadAssetAsync<EliteDef>(stringToLoad).WaitForCompletion() == null)
            {
                ElitesPlugin.Logger.LogError($"The addressable path {stringToLoad} is invalid! Skipping IL edit for this section!");
                continue;
            }

            c.Emit(OpCodes.Ldstr, stringToLoad);
            c.EmitDelegate(delegate (EliteDef origDef, string addressableName)
            {
                return origDef ?? Addressables.LoadAssetAsync<EliteDef>(addressableName).WaitForCompletion();
            });
        }

        ElitesPlugin.Logger.LogDebug($"{nameof(CombatDirector_Init)} | Applied addressable overrides for {i} elite defs");
    }

    private static string GetAddressableName(FieldReference field)
    {
        // RoR2.RoR2Content/Elites::Lightning
        // RoR2.DLC1Content/Elites::Earth
        // RoR2.DLC2Content/Elites::AurelioniteHonor

        // RoR2/Base/EliteLightning/edLightning.asset
        // RoR2/DLC1/EliteEarth/edEarth.asset
        // RoR2/DLC2/Elites/EliteAurelionite/edAurelioniteHonor.asset

        // because FUCK CONSISTENCY

        if (string.IsNullOrEmpty(field?.Name) || string.IsNullOrEmpty(field.DeclaringType?.FullName))
            return $"{field?.Name ?? "NULL FIELD"} :: {field?.DeclaringType?.FullName ?? "NULL DECLARING TYPE"}";

        var dlcName = field.DeclaringType.FullName.Substring(5, 4);
        if (dlcName is "RoR2")
            dlcName = "Base";

        var folderName = $"Elite{field.Name.Replace("Honor", string.Empty)}";
        if (dlcName is not "Base" and not "DLC1")
            folderName = "Elites/" + folderName;

        var eliteName = $"ed{field.Name}.asset";

        return $"RoR2/{dlcName}/{folderName}/{eliteName}";
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
        EliteAPI.SetHooks();
        return AddInternal(elite, Assembly.GetCallingAssembly());
    }

    internal static bool AddInternal(CustomElite customElite, Assembly addingAssembly)
    {

        if (!customElite.EliteDef)
        {
            throw new ArgumentNullException("customElite.EliteDef");
        }

        if (!CatalogBlockers.GetAvailability<EliteDef>())
        {
            ElitesPlugin.Logger.LogError($"Too late ! Tried to add elite: {customElite.EliteDef.modifierToken} after the EliteCatalog has initialized!");
            return false;
        }

        if (customElite.EliteTierDefs == null || customElite.EliteTierDefs.Count() <= 0)
        {
            throw new ArgumentNullException("customElite.EliteTierDefs");
        }

        R2APIContentManager.HandleContentAddition(addingAssembly, customElite.EliteDef);
        EliteDefinitions.Add(customElite);
        return true;
    }

    #endregion Add Methods

    #region Combat Director Modifications

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef[] VanillaEliteTiers { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaFirstTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaFirstExtendedTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstExtendedTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaSecondTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static CombatDirector.EliteTierDef VanillaLunarTierDef { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public static int VanillaEliteTierCount;

    /// <summary>
    /// 
    /// </summary>
    public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

    private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = new List<CombatDirector.EliteTierDef>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteTierDef"></param>
    /// <returns></returns>
    public static string GetName(this CombatDirector.EliteTierDef eliteTierDef) => GetEliteTierDefNameInternal(eliteTierDef);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteTierDef"></param>
    /// <param name="name"></param>
    public static void SetName(this CombatDirector.EliteTierDef eliteTierDef, string name) => SetEliteTierDefNameInternal(eliteTierDef, name);

    /// <summary>
    /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
    /// </summary>
    public static CombatDirector.EliteTierDef?[]? GetCombatDirectorEliteTiers()
    {
        EliteAPI.SetHooks();
        return CombatDirector.eliteTiers;
    }

    /// <summary>
    /// The EliteTierDef array is used by the Combat Director for doing its elite spawning while doing a run.
    /// You can get the current array used by the director with <see cref="GetCombatDirectorEliteTiers"/>
    /// </summary>
    /// <param name="newEliteTiers">The new elite tiers that will be used by the combat director.</param>
    public static void OverrideCombatDirectorEliteTiers(CombatDirector.EliteTierDef?[]? newEliteTiers)
    {
        EliteAPI.SetHooks();
        CombatDirector.eliteTiers = newEliteTiers;
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
    /// When adding a new elite tier, do not fill the eliteTypes field with your custom elites defs if your goal is to add your custom elite in it right after.
    /// Because when doing EliteAPI.Add, the API will add your elite to the specified tiers <see cref="CustomElite.EliteTierDefs"/>.
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef)
    {
        EliteAPI.SetHooks();
        var indexToInsertAt = Array.FindIndex(GetCombatDirectorEliteTiers(), x => x.costMultiplier >= eliteTierDef.costMultiplier);
        if (indexToInsertAt >= 0)
        {
            return AddCustomEliteTier(eliteTierDef, indexToInsertAt);
        }
        else
        {
            return AppendCustomEliteTier(eliteTierDef);
        }
    }

    // todo : maybe sort the CombatDirector.eliteTiers array based on cost ? the game code isnt the cleanest about this
    /// <summary>
    /// Allows for adding a new <see cref="CombatDirector.EliteTierDef"/>
    /// Do NOT fill the <see cref="CombatDirector.EliteTierDef.eliteTypes"/> field with your custom elites defs if your goal is to add your custom elite in it right after.
    /// Because when doing <see cref="Add"/>, it'll add your elite to the specified <see cref="CustomElite.EliteTierDefs"/>.
    /// </summary>
    /// <param name="eliteTierDef">The new elite tier to add.</param>
    /// <param name="indexToInsertAt">Optional index to specify if you want to insert a cheaper elite tier</param>
    public static int AddCustomEliteTier(CombatDirector.EliteTierDef? eliteTierDef, int indexToInsertAt = -1)
    {
        EliteAPI.SetHooks();
        var eliteTiersSize = VanillaEliteTierCount + CustomEliteTierCount;

        var currentEliteTiers = GetCombatDirectorEliteTiers();
        if (currentEliteTiers != null)
        {

            if (indexToInsertAt == -1)
            {
                indexToInsertAt = currentEliteTiers.Length;
                HG.ArrayUtils.ArrayAppend(ref currentEliteTiers, eliteTierDef);
            }
            else
            {
                HG.ArrayUtils.ArrayInsert(ref currentEliteTiers, indexToInsertAt, eliteTierDef);
            }

            OverrideCombatDirectorEliteTiers(currentEliteTiers);
        }

        CustomEliteTierDefs.Add(eliteTierDef);

        ElitesPlugin.Logger.LogInfo($"Custom Elite Tier : (Index : {indexToInsertAt}) added");

        return indexToInsertAt;
    }

    #endregion Combat Director Modifications
}
