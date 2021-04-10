using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using RoR2.ContentManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

using BF = System.Reflection.BindingFlags;

namespace R2API {

    // Original code from Rein and Rob

    public interface IModdedUnlockableDataProvider {
        string AchievementIdentifier { get; }
        string UnlockableIdentifier { get; }
        string AchievementNameToken { get; }
        string PrerequisiteUnlockableIdentifier { get; }
        string UnlockableNameToken { get; }
        string AchievementDescToken { get; }
        Sprite Sprite { get; }
        Func<string> GetHowToUnlock { get; }
        Func<string> GetUnlocked { get; }
    }

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

    public struct UnlockableInfo {
        internal string Name;
        internal Func<string> HowToUnlockString;
        internal Func<string> UnlockedString;
        internal int SortScore;
    }

    /// <summary>
    /// API for adding custom unlockables to the game.
    /// </summary>
    [R2APISubmodule]
    public static class UnlockableAPI {

        private static readonly HashSet<string> UnlockableIdentifiers = new HashSet<string>();
        private static readonly List<UnlockableDef> Unlockables = new List<UnlockableDef>();
        private static readonly List<AchievementDef> Achievements = new List<AchievementDef>();

        private static bool _unlockableCatalogInitialized;

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            R2APIContentPackProvider.WhenContentPackReady += AddUnlockablesToGame;
            IL.RoR2.AchievementManager.CollectAchievementDefs += AddCustomAchievements;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            R2APIContentPackProvider.WhenContentPackReady -= AddUnlockablesToGame;
            IL.RoR2.AchievementManager.CollectAchievementDefs -= AddCustomAchievements;
        }

        private static void AddUnlockablesToGame(ContentPack r2apiContentPack) {
            foreach (var unlockable in Unlockables) {
                R2API.Logger.LogInfo($"Custom Unlockable: {unlockable.cachedName} added");
            }

            r2apiContentPack.unlockableDefs.Add(Unlockables.ToArray());
            _unlockableCatalogInitialized = true;
        }

        private static void AddCustomAchievements(ILContext il) {
            var achievementIdentifierField = typeof(AchievementManager).GetField("achievementIdentifiers", BF.Public | BF.Static | BF.NonPublic);
            if (achievementIdentifierField is null) {
                throw new NullReferenceException($"Could not find field in {nameof(AchievementManager)}");
            }

            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After,
                x => x.MatchEndfinally(),
                x => x.MatchLdloc(1)
            );

            void AddOurDefs(List<AchievementDef> achievementDefs, Dictionary<string, AchievementDef> stringToAchievementDef, List<string> identifiers) {
                for (var i = 0; i < Unlockables.Count; i++) {
                    var unlockable = Unlockables[i];
                    var achievement = Achievements[i];

                    if (achievement is null) {
                        continue;
                    }

                    identifiers.Add(achievement.identifier);
                    achievementDefs.Add(achievement);
                    stringToAchievementDef.Add(achievement.identifier, achievement);
                }
            }

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldsfld, achievementIdentifierField);
            cursor.EmitDelegate<Action<List<AchievementDef>, Dictionary<string, AchievementDef>, List<string>>>(AddOurDefs);
            cursor.Emit(OpCodes.Ldloc_1);
        }

        internal static UnlockableDef CreateNewUnlockable(UnlockableInfo unlockableInfo) {
            var newUnlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();

            newUnlockableDef.cachedName = unlockableInfo.Name;
            newUnlockableDef.nameToken = unlockableInfo.Name;
            newUnlockableDef.getHowToUnlockString = unlockableInfo.HowToUnlockString;
            newUnlockableDef.getUnlockedString = unlockableInfo.UnlockedString;
            newUnlockableDef.sortScore = unlockableInfo.SortScore;

            return newUnlockableDef;
        }

        public static UnlockableDef AddUnlockable<TUnlockable>(bool serverTracked) where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(UnlockableAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(UnlockableAPI)})]");
            }

            var instance = new TUnlockable();

            if (_unlockableCatalogInitialized) {
                throw new InvalidOperationException($"Too late ! Tried to add unlockable: {instance.UnlockableIdentifier} after the unlockable list was created");
            }

            string unlockableIdentifier = instance.UnlockableIdentifier;

            if (!UnlockableIdentifiers.Add(unlockableIdentifier)) {
                throw new InvalidOperationException($"The unlockable identifier '{unlockableIdentifier}' is already used by another mod.");
            }

            var unlockableDef = CreateNewUnlockable(new UnlockableInfo {
                Name = instance.UnlockableIdentifier,
                HowToUnlockString = instance.GetHowToUnlock,
                UnlockedString = instance.GetUnlocked,
                SortScore = 200
            });

            var achievementDef = new AchievementDef {
                identifier = instance.AchievementIdentifier,
                unlockableRewardIdentifier = instance.UnlockableIdentifier,
                prerequisiteAchievementIdentifier = instance.PrerequisiteUnlockableIdentifier,
                nameToken = instance.AchievementNameToken,
                descriptionToken = instance.AchievementDescToken,
                achievedIcon = instance.Sprite,
                type = instance.GetType(),
                serverTrackerType = serverTracked ? instance.GetType() : null,
            };

            Unlockables.Add(unlockableDef);
            Achievements.Add(achievementDef);

            return unlockableDef;
        }
    }
}
