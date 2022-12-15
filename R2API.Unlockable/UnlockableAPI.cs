using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API.AutoVersionGen;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using RoR2BepInExPack.VanillaFixes;
using UnityEngine;

namespace R2API;

// Original code from Rein and Rob
/// <summary>
/// API for adding custom unlockables to the game.
/// </summary>
[Obsolete(UnlockableAPI.ObsoleteMessage)]
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class UnlockableAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".unlockable";
    public const string PluginName = R2API.PluginName + ".Unlockable";

    public const string ObsoleteMessage = "The patch 1.2.3 for RoR2 has made UnlockableAPI's methods and implementations redundant.\n" +
        "From now on use The game's \"RegisterAchievement\" attribute on top of baseAchievement inheriting classes to register AchievementDefs and tie AchievementDefs to their respective UnlockableDefs.\n" +
        "UnlockableAPI will be removed on the next major RoR2 release.";
    private static readonly HashSet<string> UnlockableIdentifiers = new HashSet<string>();
    private static readonly List<AchievementDef> Achievements = new List<AchievementDef>();

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static bool _hooksEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        SaferAchievementManager.OnCollectAchievementDefs += AddOurDefs;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        SaferAchievementManager.OnCollectAchievementDefs -= AddOurDefs;

        _hooksEnabled = false;
    }

    private static void AddOurDefs(List<string> identifiers, Dictionary<string, AchievementDef> stringToAchievementDef, List<AchievementDef> achievementDefs)
    {
        foreach (var achievement in Achievements)
        {
            if (achievement is null)
            {
                continue;
            }

            identifiers.Add(achievement.identifier);
            achievementDefs.Add(achievement);
            stringToAchievementDef.Add(achievement.identifier, achievement);
        }
    }
    #endregion

    internal static UnlockableDef CreateNewUnlockable(UnlockableInfo unlockableInfo)
    {
        var newUnlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
        return SetupUnlockable(unlockableInfo, newUnlockableDef);
    }

    internal static UnlockableDef SetupUnlockable(UnlockableInfo unlockableInfo, UnlockableDef unlockableDef)
    {

        unlockableDef.cachedName = unlockableInfo.Name;
        unlockableDef.nameToken = unlockableInfo.Name;
        unlockableDef.getHowToUnlockString = unlockableInfo.HowToUnlockString;
        unlockableDef.getUnlockedString = unlockableInfo.UnlockedString;
        unlockableDef.sortScore = unlockableInfo.SortScore;

        return unlockableDef;
    }

    [Obsolete("The bool parameter serverTracked is redundant. Instead, pass in a Type that inherits from BaseServerAchievement if it is server tracked, or nothing if it's not")]
    public static UnlockableDef AddUnlockable<TUnlockable>(bool serverTracked) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new()
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), null, null);
    }
    public static UnlockableDef AddUnlockable<TUnlockable>(Type serverTrackerType) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new()
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), serverTrackerType, null);
    }
    public static UnlockableDef AddUnlockable<TUnlockable>(UnlockableDef unlockableDef) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new()
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), null, unlockableDef);
    }
    public static UnlockableDef AddUnlockable(Type unlockableType, Type serverTrackerType)
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), serverTrackerType, null);
    }
    public static UnlockableDef AddUnlockable(Type unlockableType, UnlockableDef unlockableDef)
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), null, unlockableDef);
    }

    /// <summary>
    /// Adds an AchievementDef to the list of achievements to add to the game
    /// </summary>
    /// <param name="achievementDef">The achievementDef to add</param>
    /// <returns>True if succesful, false otherwise</returns>
    public static bool AddAchievement(AchievementDef achievementDef)
    {
        UnlockableAPI.SetHooks();
        var identifiers = Achievements.Select(achievementDef => achievementDef.identifier);
        try
        {
            if (identifiers.Contains(achievementDef.identifier))
            {
                throw new InvalidOperationException($"The achievement identifier '{achievementDef.identifier}' is already used by another mod.");
            }
            else
            {
                Achievements.Add(achievementDef);
                return true;
            }
        }
        catch (Exception e)
        {
            UnlockablePlugin.Logger.LogError($"An error has occured while trying to add a new AchievementDef: {e}");
            return false;
        }
    }

    /// <summary>
    /// Add an unlockable tied to an achievement.
    /// For an example usage check <see href="https://github.com/ArcPh1r3/HenryTutorial/blob/master/HenryMod/Modules/Achievements/HenryMasteryAchievement.cs">rob repository</see>
    /// </summary>
    /// <typeparam name="TUnlockable">Class that inherits from BaseAchievement and implements <see cref="IModdedUnlockableDataProvider"/></typeparam>
    /// <param name="serverTrackerType">Type that inherits from BaseServerAchievement for achievements that the server needs to track</param>
    /// <param name="unlockableDef">For UnlockableDefs created in advance. Leaving null will generate an UnlockableDef instead.</param>
    /// <returns></returns>
    public static UnlockableDef AddUnlockable<TUnlockable>(Type serverTrackerType = null, UnlockableDef unlockableDef = null) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new()
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), serverTrackerType, unlockableDef);
    }

    /// <summary>
    /// Add an unlockable tied to an achievement.
    /// For an example usage check <see href="https://github.com/ArcPh1r3/HenryTutorial/blob/master/HenryMod/Modules/Achievements/HenryMasteryAchievement.cs">rob repository</see>
    /// </summary>
    /// <param name="unlockableType">Class that inherits from BaseAchievement and implements <see cref="IModdedUnlockableDataProvider"/></param>
    /// <param name="serverTrackerType">Type that inherits from <see cref="BaseServerAchievement"/> for achievements that the server needs to track</param>
    /// <param name="unlockableDef">For <see cref="UnlockableDef"/> created in advance. Leaving null will generate an <see cref="UnlockableDef"/> instead.</param>
    /// <returns></returns>
    public static UnlockableDef AddUnlockable(Type unlockableType, Type serverTrackerType = null, UnlockableDef unlockableDef = null)
    {
        UnlockableAPI.SetHooks();
        return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), serverTrackerType, unlockableDef);
    }

    private static UnlockableDef AddUnlockableInternal(Type unlockableType, Assembly assembly, Type serverTrackerType = null, UnlockableDef unlockableDef = null)
    {

        var instance = Activator.CreateInstance(unlockableType) as IModdedUnlockableDataProvider;

        if (!CatalogBlockers.GetAvailability<UnlockableDef>())
        {
            throw new InvalidOperationException($"Too late ! Tried to add unlockable: {instance.UnlockableIdentifier} after the UnlockableCatalog");
        }

        var unlockableIdentifier = instance.UnlockableIdentifier;

        if (!UnlockableIdentifiers.Add(unlockableIdentifier))
        {
            throw new InvalidOperationException($"The unlockable identifier '{unlockableIdentifier}' is already used by another mod.");
        }

        if (unlockableDef == null)
        {
            unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
        }

        var unlockableInfo = new UnlockableInfo
        {
            Name = instance.UnlockableIdentifier,
            HowToUnlockString = instance.GetHowToUnlock,
            UnlockedString = instance.GetUnlocked,
            SortScore = 200
        };

        unlockableDef = SetupUnlockable(unlockableInfo, unlockableDef);

        var achievementDef = new AchievementDef
        {
            identifier = instance.AchievementIdentifier,
            unlockableRewardIdentifier = instance.UnlockableIdentifier,
            prerequisiteAchievementIdentifier = instance.PrerequisiteUnlockableIdentifier,
            nameToken = instance.AchievementNameToken,
            descriptionToken = instance.AchievementDescToken,
            achievedIcon = instance.Sprite,
            type = instance.GetType(),
            serverTrackerType = serverTrackerType,
        };
        R2APIContentManager.HandleContentAddition(assembly, unlockableDef);
        Achievements.Add(achievementDef);

        return unlockableDef;
    }
}
