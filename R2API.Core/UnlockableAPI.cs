using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using R2API.ContentManagement;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using RoR2BepInExPack.VanillaFixes;
using UnityEngine;

namespace R2API {

    // Original code from Rein and Rob

    /// <summary>
    /// Interface used to provide the metadata needed to register an achievement + unlockable
    /// </summary>
    [Obsolete(UnlockableAPI.ObsoleteMessage)]
    public interface IModdedUnlockableDataProvider {

        /// <summary>
        /// The identifier of the achievement being added.
        /// Should be unique
        /// </summary>
        string AchievementIdentifier { get; }

        /// <summary>
        /// The identifier of the unlockable granted when the achievement is completed.
        /// Should be unique.
        /// This is what is used when specifying an unlock condition for various things in the game
        /// </summary>
        string UnlockableIdentifier { get; }

        /// <summary>
        /// The unlockableIdentifier of a prerequisite.
        /// Should be used for skill unlocks for a custom character if the character has an unlock condition.
        /// Multiple prereqs are not supported (as far as I can tell)
        /// </summary>
        string AchievementNameToken { get; }

        /// <summary>
        /// The language token for the name to be shown in logbook for this achievement.
        /// </summary>
        string PrerequisiteUnlockableIdentifier { get; }

        /// <summary>
        /// The language token for the unlockable.
        /// Not 100% sure where this is shown in game.
        /// </summary>
        string UnlockableNameToken { get; }

        /// <summary>
        /// The language token for the description to be shown in logbook for this achievement.
        /// Also used to create the 'How to unlock' text.
        /// </summary>
        string AchievementDescToken { get; }

        /// <summary>
        /// Sprite that is used for this achievement.
        /// </summary>
        Sprite Sprite { get; }

        /// <summary>
        /// Delegate that return a string that will be shown to the user on how to unlock the achievement.
        /// </summary>
        Func<string> GetHowToUnlock { get; }

        /// <summary>
        /// Delegate that return a string that will be shown to the user when the achievement is unlocked.
        /// </summary>
        Func<string> GetUnlocked { get; }
    }

    /// <summary>
    /// Class used to provide the metadata needed to register an achievement + unlockable
    /// </summary>
    [Obsolete(UnlockableAPI.ObsoleteMessage)]
    public abstract class ModdedUnlockable : BaseAchievement, IModdedUnlockableDataProvider {
        public abstract string AchievementIdentifier { get; }
        public abstract string UnlockableIdentifier { get; }
        public abstract string AchievementNameToken { get; }
        public abstract string PrerequisiteUnlockableIdentifier { get; }
        public abstract string UnlockableNameToken { get; }
        public abstract string AchievementDescToken { get; }
        public abstract Sprite Sprite { get; }
        public abstract Func<string> GetHowToUnlock { get; }
        public abstract Func<string> GetUnlocked { get; }

        public override void OnGranted() => base.OnGranted();

        public override void OnInstall() {
            base.OnInstall();
        }

        public override void OnUninstall() {
            base.OnUninstall();
        }

        public void Revoke() {
            if (userProfile.HasAchievement(AchievementIdentifier)) {
                userProfile.RevokeAchievement(AchievementIdentifier);
            }

            userProfile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(UnlockableIdentifier));
        }

        public override float ProgressForAchievement() => base.ProgressForAchievement();

        public override BodyIndex LookUpRequiredBodyIndex() {
            return base.LookUpRequiredBodyIndex();
        }

        public override void OnBodyRequirementBroken() => base.OnBodyRequirementBroken();
        public override void OnBodyRequirementMet() => base.OnBodyRequirementMet();
        public override bool wantsBodyCallbacks { get => base.wantsBodyCallbacks; }
    }

    internal struct UnlockableInfo {
        public string Name;
        public Func<string> HowToUnlockString;
        public Func<string> UnlockedString;
        public int SortScore;
    }

    /// <summary>
    /// API for adding custom unlockables to the game.
    /// </summary>
    [Obsolete(UnlockableAPI.ObsoleteMessage)]
    public static class UnlockableAPI {

        public const string ObsoleteMessage = "The patch 1.2.3 for RoR2 has made UnlockableAPI's methods and implementations redundant.\n" +
            "From now on use The game's \"RegisterAchievement\" attribute on top of baseAchievement inheriting classes to register AchievementDefs and tie AchievementDefs to their respective UnlockableDefs.\n" +
            "UnlockableAPI will be removed on the next major RoR2 release.";
        private static readonly HashSet<string> UnlockableIdentifiers = new HashSet<string>();
        private static readonly List<AchievementDef> Achievements = new List<AchievementDef>();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        [R2APIInitialize(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            SaferAchievementManager.OnCollectAchievementDefs += AddOurDefs;
        }

        private static void AddOurDefs(List<string> identifiers, Dictionary<string, AchievementDef> stringToAchievementDef, List<AchievementDef> achievementDefs) {
            foreach (var achievement in Achievements) {
                if (achievement is null) {
                    continue;
                }

                identifiers.Add(achievement.identifier);
                achievementDefs.Add(achievement);
                stringToAchievementDef.Add(achievement.identifier, achievement);
            }
        }

        [R2APIInitialize(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            SaferAchievementManager.OnCollectAchievementDefs -= AddOurDefs;
        }

        internal static UnlockableDef CreateNewUnlockable(UnlockableInfo unlockableInfo) {
            var newUnlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            return SetupUnlockable(unlockableInfo, newUnlockableDef);
        }

        internal static UnlockableDef SetupUnlockable(UnlockableInfo unlockableInfo, UnlockableDef unlockableDef) {

            unlockableDef.cachedName = unlockableInfo.Name;
            unlockableDef.nameToken = unlockableInfo.Name;
            unlockableDef.getHowToUnlockString = unlockableInfo.HowToUnlockString;
            unlockableDef.getUnlockedString = unlockableInfo.UnlockedString;
            unlockableDef.sortScore = unlockableInfo.SortScore;

            return unlockableDef;
        }

        [Obsolete("The bool parameter serverTracked is redundant. Instead, pass in a Type that inherits from BaseServerAchievement if it is server tracked, or nothing if it's not")]
        public static UnlockableDef AddUnlockable<TUnlockable>(bool serverTracked) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
            return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), null, null);
        }
        public static UnlockableDef AddUnlockable<TUnlockable>(Type serverTrackerType) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
            return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), serverTrackerType, null);
        }
        public static UnlockableDef AddUnlockable<TUnlockable>(UnlockableDef unlockableDef) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
            return AddUnlockableInternal(typeof(TUnlockable), Assembly.GetCallingAssembly(), null, unlockableDef);
        }
        public static UnlockableDef AddUnlockable(Type unlockableType, Type serverTrackerType) {
            return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), serverTrackerType, null);
        }
        public static UnlockableDef AddUnlockable(Type unlockableType, UnlockableDef unlockableDef) {
            return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), null, unlockableDef);
        }

        /// <summary>
        /// Adds an AchievementDef to the list of achievements to add to the game
        /// </summary>
        /// <param name="achievementDef">The achievementDef to add</param>
        /// <returns>True if succesful, false otherwise</returns>
        public static bool AddAchievement(AchievementDef achievementDef) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(UnlockableAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(UnlockableAPI)})]");
            }
            var identifiers = Achievements.Select(achievementDef => achievementDef.identifier);
            try {
                if (identifiers.Contains(achievementDef.identifier)) {
                    throw new InvalidOperationException($"The achievement identifier '{achievementDef.identifier}' is already used by another mod.");
                }
                else {
                    Achievements.Add(achievementDef);
                    return true;
                }
            }
            catch (Exception e) {
                R2API.Logger.LogError($"An error has occured while trying to add a new AchievementDef: {e}");
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
        public static UnlockableDef AddUnlockable<TUnlockable>(Type serverTrackerType = null, UnlockableDef unlockableDef = null) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
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
        public static UnlockableDef AddUnlockable(Type unlockableType, Type serverTrackerType = null, UnlockableDef unlockableDef = null) {
            return AddUnlockableInternal(unlockableType, Assembly.GetCallingAssembly(), serverTrackerType, unlockableDef);
        }

        private static UnlockableDef AddUnlockableInternal(Type unlockableType, Assembly assembly, Type serverTrackerType = null, UnlockableDef unlockableDef = null) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(UnlockableAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(UnlockableAPI)})]");
            }

            var instance = Activator.CreateInstance(unlockableType) as IModdedUnlockableDataProvider;

            if (!CatalogBlockers.GetAvailability<UnlockableDef>()) {
                throw new InvalidOperationException($"Too late ! Tried to add unlockable: {instance.UnlockableIdentifier} after the UnlockableCatalog");
            }

            var unlockableIdentifier = instance.UnlockableIdentifier;

            if (!UnlockableIdentifiers.Add(unlockableIdentifier)) {
                throw new InvalidOperationException($"The unlockable identifier '{unlockableIdentifier}' is already used by another mod.");
            }

            if (unlockableDef == null) {
                unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            }

            var unlockableInfo = new UnlockableInfo {
                Name = instance.UnlockableIdentifier,
                HowToUnlockString = instance.GetHowToUnlock,
                UnlockedString = instance.GetUnlocked,
                SortScore = 200
            };

            unlockableDef = SetupUnlockable(unlockableInfo, unlockableDef);

            var achievementDef = new AchievementDef {
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
}
