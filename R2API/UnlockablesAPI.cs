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
using BepInEx;

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
                type = instance.GetType(),
                serverTrackerType = serverTracked ? instance.GetType() : null,
            };

            var unl = new UnlockableDef {
                nameToken = instance.UnlockableNameToken,
                getHowToUnlockString = instance.GetHowToUnlock,
                getUnlockedString = instance.GetUnlocked,
            };

            moddedUnlocks.Add((ach, unl));
        }

        /// <summary>
        /// Adds an unlockable without a corresponding achievement. Generally useful for adding behind the scenes unlockables similar to finding all newt altars and eclipse tracking
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="hidden"></param>
        /// <param name="nameToken"></param>
        /// <param name="displayModelPath"></param>
        public static void AddUnlockableOnly(string identifier, bool hidden = false, string nameToken = null, string displayModelPath = null) {
            if(!Loaded) throw new InvalidOperationException($"{nameof(UnlockablesAPI)} is not loaded. Try requesting the submodule with '[R2APISubmoduleDependency]'");
            if(!AbleToAdd) throw new InvalidOperationException("Too late to add unlocks. Must be done during awake.");
            var def = new UnlockableDef {
                name = identifier,
                hidden = hidden,
                nameToken = nameToken,
                displayModelPath = displayModelPath,
                getHowToUnlockString = null,
                getUnlockedString = null,
            };
            moddedUnlocksWithoutAchievements.Add(def);
        }

        public static void AddEclipseUnlockablesForSurvivor(string modGuid, SurvivorDef survivor) {
            if(!Loaded) throw new InvalidOperationException($"{nameof(UnlockablesAPI)} is not loaded. Try requesting the submodule with '[R2APISubmoduleDependency]'");
            if(!AbleToAdd) throw new InvalidOperationException("Too late to add unlocks. Must be done during awake.");
            if(survivor is null) throw new ArgumentNullException(nameof(survivor));
            if(survivor.name.IsNullOrWhiteSpace()) throw new ArgumentException("No name assigned", nameof(SurvivorDef));
            var usedGuid = modGuid.Replace('.', '_');
            eclipseUnlockInfos.Add((usedGuid, survivor));
        }
        private static readonly List<(string guid, SurvivorDef survivor)> eclipseUnlockInfos = new List<(string guid, SurvivorDef survivor)>();

        #endregion
        #region Internal
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.AchievementManager.CollectAchievementDefs += AchievementManager_CollectAchievementDefs;
            IL.RoR2.UnlockableCatalog.Init += UnlockableCatalog_Init;
            On.RoR2.EclipseRun.GetEclipseBaseUnlockableString += EclipseRun_GetEclipseBaseUnlockableString;
            IL.RoR2.UI.EclipseRunScreenController.SelectSurvivor += EclipseRunScreenController_SelectSurvivor;
            AbleToAdd = true;
        }


        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.AchievementManager.CollectAchievementDefs -= AchievementManager_CollectAchievementDefs;
            IL.RoR2.UnlockableCatalog.Init -= UnlockableCatalog_Init;
            On.RoR2.EclipseRun.GetEclipseBaseUnlockableString -= EclipseRun_GetEclipseBaseUnlockableString;
            IL.RoR2.UI.EclipseRunScreenController.SelectSurvivor -= EclipseRunScreenController_SelectSurvivor;
            AbleToAdd = false;
        }

        private static bool _loaded = false;
        private static bool _ableToAdd = false;

        private static String GetEclipseUnlockableName(String current, SurvivorIndex index) => identities.TryGetValue(index, out var id) ? $"Eclipse.{id}" : current;

        private static void EclipseRunScreenController_SelectSurvivor(ILContext il) => new ILCursor(il)
            .GotoNext(x => x.MatchLdstr("Eclipse.{0}"))
            .GotoNext(MoveType.AfterLabel, x => x.MatchStloc(0))
            .Emit(OpCodes.Ldarg_1)
            .EmitDelegate<Func<String, SurvivorIndex, String>>(GetEclipseUnlockableName);


        private static String EclipseRun_GetEclipseBaseUnlockableString(On.RoR2.EclipseRun.orig_GetEclipseBaseUnlockableString orig) {
            var res = orig();
            if(res == "") return res;
            var spl = res.Split('.');
            if(spl.Length != 2) return res;
            if(Enum.TryParse<SurvivorIndex>(spl[1], out var i)) {
                if(identities.TryGetValue(i, out var identity)) {
                    return $"{spl[0]}.{identity}";
                }
            }
            return res;
        }
        private static readonly Dictionary<SurvivorIndex,String> identities = new Dictionary<SurvivorIndex, String>();
        private static String CreateOrGetIdentity(string mod, SurvivorDef survivor) {
            if(identities.TryGetValue(survivor.survivorIndex, out var id)) return id;
            return identities[survivor.survivorIndex] = $"{mod}_{survivor.name.Replace('.', '_')}";
        }

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
        private static readonly List<UnlockableDef> moddedUnlocksWithoutAchievements = new List<UnlockableDef>();
        private static readonly HashSet<string> usedRewardIds = new HashSet<string>();
        private static readonly Action<string, UnlockableDef> registerUnlockable;

        private static void UnlockableCatalog_Init(ILContext il) {
            void EmittedDelegate() {
                AbleToAdd = false;
                for (Int32 i = 0; i < moddedUnlocks.Count; ++i) {
                    var (achievement, unlockable) = moddedUnlocks[i];
                    RegisterUnlockable(achievement.unlockableRewardIdentifier, unlockable);
                }
                for(Int32 i = 0; i < moddedUnlocksWithoutAchievements.Count; ++i) {
                    RegisterUnlockable(moddedUnlocksWithoutAchievements[i].name, moddedUnlocksWithoutAchievements[i]);
                }
                foreach(var (modName, survivor) in eclipseUnlockInfos) {
                    for(Int32 i = 2; i <= 8; ++i) {
                        var str = $"Eclipse.{CreateOrGetIdentity(modName, survivor)}.{i}";
                        RegisterUnlockable(str, new UnlockableDef());
                    }
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
                x => x.MatchEndfinally(),
                x => x.MatchLdloc( 1 )
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
