using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using R2API.Utils;
using RoR2;
using System.Reflection;
using BF = System.Reflection.BindingFlags;
using RoR2.Achievements;

namespace R2API {
    [R2APISubmodule]
    public static class UnlockablesAPI {
        #region External
        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            set => _loaded = value;
        }

        /// <summary>
        /// Returns true if AddUnlockable can be called
        /// </summary>
        public static bool AbleToAdd {
            get => _ableToAdd;
            private set => _ableToAdd = value;
        }

        /// <summary>
        /// Adds an unlockable type and queues it to be registered
        /// In the vast majority of cases the type used should inherit from ModdedUnlockable, which handles most of the messy stuff for you
        /// </summary>
        /// <typeparam name="TUnlockable">The type that represents the achievement</typeparam>
        /// <param name="serverTracked">True if the achievement tracking is host side, false if client side</param>
        public static void AddUnlockable<TUnlockable>(Boolean serverTracked)
            where TUnlockable : BaseAchievement, IModdedUnlockableDataProvider, new() {
            if (!Loaded) throw new InvalidOperationException( $"{nameof(UnlockablesAPI)} is not loaded. Try requesting the submodule with '[R2APISubmoduleDependency]'");
            if (!AbleToAdd) throw new InvalidOperationException("Too late to add unlocks. Must be done during awake.");

            var instance = new TUnlockable();
            var unlockableIdentifier = instance.UnlockableIdentifier;
            var identifier = instance.AchievementIdentifier;

            if (!usedRewardIds.Add(unlockableIdentifier)) throw new InvalidOperationException($"The unlockable identifier '{unlockableIdentifier}' is already used by another mod or the base game.");

            var ach = new AchievementDef {
                identifier = instance.AchievementIdentifier,
                unlockableRewardIdentifier = instance.UnlockableIdentifier,
                prerequisiteAchievementIdentifier = instance.PrerequisiteUnlockableIdentifier,
                nameToken = instance.AchievementNameToken,
                descriptionToken = instance.AchievementDescToken,
                iconPath = instance.SpritePath,
                type = serverTracked ? null : instance.GetType(),
                serverTrackerType = serverTracked ? instance.GetType() : null,
            };

            var unl = new UnlockableDef {
                nameToken = instance.UnlockableNameToken,
                getHowToUnlockString = instance.GetHowToUnlock,
                getUnlockedString = instance.GetUnlocked,
            };

            moddedUnlocks.Add((ach, unl));
        }
        #endregion
        #region Internal
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs;
            IL.RoR2.UnlockableCatalog.Init += UnlockableCatalog_Init;
            AbleToAdd = true;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.AchievementManager.CollectAchievementDefs -= AchievementManager_CollectAchievementDefs;
            IL.RoR2.UnlockableCatalog.Init -= UnlockableCatalog_Init;
            AbleToAdd = false;
        }

        private static bool _loaded = false;
        private static bool _ableToAdd = false;

        private static Action<string,UnlockableDef> RegisterUnlockable {
            get {
                if( _registerUnlockable is null ) {
                    const string targetMethodName = "RegisterUnlockable";
                    MethodInfo targetMethod = typeof(UnlockableCatalog).GetMethod(targetMethodName, BF.Public | BF.Static | BF.NonPublic);
                    if (targetMethod is null) {
                        R2API.Logger.LogError($"No method '{targetMethodName}' found in type '{nameof(UnlockableCatalog)}', UnlockablesAPI will not function properly.");
                        return null;
                    }
                    DynamicMethodDefinition dmd = new DynamicMethodDefinition("UnlockableAPI<UnlockableCatalog<RegisterUnlockable<", null,
                        new[] { typeof(string), typeof(UnlockableDef), });

                    ILProcessor proc = dmd.GetILProcessor();
                    proc.Emit(OpCodes.Jmp, targetMethod);
                    proc.Emit(OpCodes.Ret);
                    _registerUnlockable = (Action<string, UnlockableDef>)dmd.Generate().CreateDelegate<Action<string, UnlockableDef>>();
                }

                return _registerUnlockable;
            }
        }
        private static Action<string, UnlockableDef> _registerUnlockable;

        private static readonly List<(AchievementDef achievementDef, UnlockableDef unlockableDef)> moddedUnlocks = new List<(AchievementDef, UnlockableDef)>();
        private static readonly HashSet<string> usedRewardIds = new HashSet<string>();
        private static readonly Action<string, UnlockableDef> registerUnlockable;

        private static void UnlockableCatalog_Init(ILContext il) {
            void EmittedDelegate() {
                AbleToAdd = false;
                for (Int32 i = 0; i < moddedUnlocks.Count; ++i) {
                    var (achievement, unlockable) = moddedUnlocks[i];
                    RegisterUnlockable(achievement.unlockableRewardIdentifier, unlockable);
                }
            }

            var c = new ILCursor(il);

            String text = "";
            while (c.TryGotoNext(MoveType.After,
                x => x.MatchLdstr(out text),
                x => x.MatchStfld(out _))) {
                _ = usedRewardIds.Add(text);
            }

            _ = c.GotoNext(MoveType.Before, x => x.MatchLdsfld(typeof(RoR2.UnlockableCatalog).GetField("nameToDefTable", BF.Public | BF.NonPublic | BF.Static)));
            _ = c.EmitDelegate<Action>(EmittedDelegate);
        }
        private static void AchievementManager_CollectAchievementDefs(ILContext il) {
            const string fieldName = "achievementIdentifiers";
            FieldInfo field = typeof(AchievementManager).GetField(fieldName, BF.Public | BF.Static | BF.NonPublic);
            if( field is null ) {
                R2API.Logger.LogError($"No field '{fieldName}' found in type '{nameof(AchievementManager)}', UnlockablesAPI will not function properly.");
                return;
            }

            var cursor = new ILCursor(il);
            _ = cursor.GotoNext(MoveType.After,
                x => x.MatchEndfinally()
            );

            void EmittedDelegate(List<AchievementDef> achievementDefs, Dictionary<string, AchievementDef> map, List<string> identifiers) {
                AbleToAdd = false;
                for (Int32 i = 0; i < moddedUnlocks.Count; ++i) {
                    var (ach, unl) = moddedUnlocks[i];
                    identifiers.Add(ach.identifier);
                    achievementDefs.Add(ach);
                    map.Add(ach.identifier, ach);
                }
            }

            _ = cursor.Emit(OpCodes.Ldarg_0);
            _ = cursor.Emit(OpCodes.Ldsfld, field);
            _ = cursor.EmitDelegate<Action<List<AchievementDef>, Dictionary<string, AchievementDef>, List<string>>>(EmittedDelegate);
            _ = cursor.Emit(OpCodes.Ldloc_1);
        }
        #endregion
    }
}
