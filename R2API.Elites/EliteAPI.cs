using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
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

    public static ObservableCollection<CustomElite?>? EliteDefinitions = new ObservableCollection<CustomElite?>();

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    /// <summary>
    /// See <see cref="CombatDirectorInitNoTimingIssue"/>
    /// </summary>
    static EliteAPI()
    {
        CombatDirectorInitNoTimingIssue();

        VanillaEliteTiers = RetrieveVanillaEliteTiers();
        VanillaFirstTierDef = RetrieveFirstVanillaTierDef();
        VanillaEliteOnlyFirstTierDef = RetrieveVanillaEliteOnlyFirstTierDef();

        ElitesPlugin.Logger.LogDebug("EliteAPI.cctor finished.");
    }

    private static bool _hooksEnabled = false;

    #region ModHelper Events and Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.CombatDirector.Init += RetrieveVanillaEliteTierCount;
        On.RoR2.CombatDirector.Init += UseOurCombatDirectorInitInstead;

        R2APIContentPackProvider.WhenAddingContentPacks += AddElitesToGame;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CombatDirector.Init -= RetrieveVanillaEliteTierCount;
        On.RoR2.CombatDirector.Init -= UseOurCombatDirectorInitInstead;

        R2APIContentPackProvider.WhenAddingContentPacks -= AddElitesToGame;

        _hooksEnabled = false;
    }

    private static void UseOurCombatDirectorInitInstead(On.RoR2.CombatDirector.orig_Init orig)
    {
        CombatDirectorInitNoTimingIssue();
    }

    /// <summary>
    /// Only hope at this point is HG using extensible code and not hard coding.
    /// Before we were loading all hard refs earlier, but we removed that in favor of a bit better loading screen.
    /// Bandaid fix for now for the api to work again : replace the RoR2Content hard refs with Addressables Load Asset.
    /// Todo : investigate if adding an event like the other catalogs have + putting code inside a GenerateContentPackAsync
    /// would be a cleaner fix or not.
    /// Note: Will be breaking as opposed to current solution below which doesnt change anything on how the old mods were operating.
    /// </summary>
    private static void CombatDirectorInitNoTimingIssue()
    {
        if (_combatDirectorInitialized)
        {
            return;
        }

        var eliteTiersDef = new List<CombatDirector.EliteTierDef>();

        var eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = 1f,
            eliteTypes = new EliteDef[1],
            isAvailable = (SpawnCard.EliteRules rules) => CombatDirector.NotEliteOnlyArtifactActive(),
            canSelectWithoutAvailableEliteDef = true
        };
        eliteTiersDef.Add(eliteTierDef);

        eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = CombatDirector.baseEliteCostMultiplier,
            eliteTypes = new EliteDef[] {
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLightning/edLightning.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteIce/edIce.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteFire/edFire.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteEarth/edEarth.asset").WaitForCompletion(),
            },
            isAvailable = (SpawnCard.EliteRules rules) => CombatDirector.NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default && Run.instance.stageClearCount < 2,
            canSelectWithoutAvailableEliteDef = false
        };
        eliteTiersDef.Add(eliteTierDef);

        eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteCostMultiplier, 0.5f),
            eliteTypes = new EliteDef[] {
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLightning/edLightningHonor.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteIce/edIceHonor.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteFire/edFireHonor.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteEarth/edEarthHonor.asset").WaitForCompletion(),
            },
            isAvailable = (SpawnCard.EliteRules rules) => CombatDirector.IsEliteOnlyArtifactActive(),
            canSelectWithoutAvailableEliteDef = false
        };
        eliteTiersDef.Add(eliteTierDef);

        eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = CombatDirector.baseEliteCostMultiplier,
            eliteTypes = new EliteDef[] {
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLightning/edLightning.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteIce/edIce.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteFire/edFire.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC1/EliteEarth/edEarth.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC2/Elites/EliteAurelionite/edAurelionite.asset").WaitForCompletion(),
            },
            isAvailable = (SpawnCard.EliteRules rules) => CombatDirector.NotEliteOnlyArtifactActive() && rules == SpawnCard.EliteRules.Default && Run.instance.stageClearCount >= 2,
            canSelectWithoutAvailableEliteDef = false
        };
        eliteTiersDef.Add(eliteTierDef);

        eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = CombatDirector.baseEliteCostMultiplier * 6f,
            eliteTypes = new EliteDef[] {
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/ElitePoison/edPoison.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteHaunted/edHaunted.asset").WaitForCompletion(),
                Addressables.LoadAssetAsync<EliteDef>("RoR2/DLC2/Elites/EliteBead/edBead.asset").WaitForCompletion(),
            },
            isAvailable = (SpawnCard.EliteRules rules) => Run.instance.loopClearCount > 0 && rules == SpawnCard.EliteRules.Default,
            canSelectWithoutAvailableEliteDef = false
        };
        eliteTiersDef.Add(eliteTierDef);

        eliteTierDef = new CombatDirector.EliteTierDef
        {
            costMultiplier = CombatDirector.baseEliteCostMultiplier,
            eliteTypes = new EliteDef[] {
                Addressables.LoadAssetAsync<EliteDef>("RoR2/Base/EliteLunar/edLunar.asset").WaitForCompletion(),
            },
            isAvailable = (SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Lunar,
            canSelectWithoutAvailableEliteDef = false
        };
        eliteTiersDef.Add(eliteTierDef);

        CombatDirector.eliteTiers = eliteTiersDef.ToArray();

        _combatDirectorInitialized = true;
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

    private static void RetrieveVanillaEliteTierCount(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
                i => i.MatchLdcI4(out VanillaEliteTierCount),
                i => i.MatchNewarr<CombatDirector.EliteTierDef>()))
        {
        }
        else
        {
            ElitesPlugin.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaEliteTierCount IL Hook");
        }
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

    private static CombatDirector.EliteTierDef[] RetrieveVanillaEliteTiers()
    {
        return CombatDirector.eliteTiers;
    }

    private static CombatDirector.EliteTierDef RetrieveFirstVanillaTierDef()
    {
        return CombatDirector.eliteTiers[1];
    }

    private static CombatDirector.EliteTierDef RetrieveVanillaEliteOnlyFirstTierDef()
    {
        return CombatDirector.eliteTiers[2];
    }

    public static CombatDirector.EliteTierDef[] VanillaEliteTiers { get; private set; }
    public static CombatDirector.EliteTierDef VanillaFirstTierDef { get; private set; }
    public static CombatDirector.EliteTierDef VanillaEliteOnlyFirstTierDef { get; private set; }

    /// <summary>
    /// Returns the current elite tier definitions used by the Combat Director for doing its elite spawning while doing a run.
    /// </summary>
    public static CombatDirector.EliteTierDef?[]? GetCombatDirectorEliteTiers()
    {
        EliteAPI.SetHooks();
        return CombatDirector.eliteTiers;
    }

    private static bool _combatDirectorInitialized;

    public static int VanillaEliteTierCount;
    private static readonly List<CombatDirector.EliteTierDef> CustomEliteTierDefs = new List<CombatDirector.EliteTierDef>();
    public static int CustomEliteTierCount => CustomEliteTierDefs.Count;

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
