using EntityStates;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace R2API {
    internal static class LoadRoR2ContentEarly {
        private static bool _ror2ContentLoaded;
        private static RoR2Content RoR2Content;

        internal static void Init() {
            _ = new ILHook(typeof(RoR2Application).GetNestedTypeCached("<>c").GetMethodCached("<LoadGameContent>b__57_1"), new ILContext.Manipulator(TakeOurInstanceInstead));

            On.RoR2.RoR2Content.LoadStaticContentAsync += LoadOnlyOnce;

            EarlyLoad();
        }

        internal static void EarlyLoad() {
            RoR2Content = new RoR2Content();

            RoR2Content.contentPack.identifier = RoR2Content.identifier;

            RoR2Content.contentPack.bodyPrefabs.Add(Resources.LoadAll<GameObject>("Prefabs/CharacterBodies/"));
            RoR2Content.contentPack.masterPrefabs.Add(Resources.LoadAll<GameObject>("Prefabs/CharacterMasters/"));
            RoR2Content.contentPack.projectilePrefabs.Add(Resources.LoadAll<GameObject>("Prefabs/Projectiles/"));
            RoR2Content.contentPack.gameModePrefabs.Add(Resources.LoadAll<GameObject>("Prefabs/GameModes/"));
            RoR2Content.contentPack.networkedObjectPrefabs.Add(Resources.LoadAll<GameObject>("Prefabs/NetworkedObjects/"));
            RoR2Content.contentPack.skillDefs.Add(Resources.LoadAll<SkillDef>("SkillDefs/"));
            RoR2Content.contentPack.skillFamilies.Add(Resources.LoadAll<SkillFamily>("SkillDefs/"));
            RoR2Content.contentPack.unlockableDefs.Add(Resources.LoadAll<UnlockableDef>("UnlockableDefs/"));
            RoR2Content.contentPack.surfaceDefs.Add(Resources.LoadAll<SurfaceDef>("SurfaceDefs/"));
            RoR2Content.contentPack.sceneDefs.Add(Resources.LoadAll<SceneDef>("SceneDefs/"));
            RoR2Content.contentPack.networkSoundEventDefs.Add(Resources.LoadAll<NetworkSoundEventDef>("NetworkSoundEventDefs/"));
            RoR2Content.contentPack.musicTrackDefs.Add(Resources.LoadAll<MusicTrackDef>("MusicTrackDefs/"));
            RoR2Content.contentPack.gameEndingDefs.Add(Resources.LoadAll<GameEndingDef>("GameEndingDefs/"));
            RoR2Content.contentPack.itemDefs.Add(Resources.LoadAll<ItemDef>("ItemDefs/"));
            RoR2Content.contentPack.equipmentDefs.Add(Resources.LoadAll<EquipmentDef>("EquipmentDefs/"));
            RoR2Content.contentPack.buffDefs.Add(Resources.LoadAll<BuffDef>("BuffDefs/"));
            RoR2Content.contentPack.eliteDefs.Add(Resources.LoadAll<EliteDef>("EliteDefs/"));
            RoR2Content.contentPack.survivorDefs.Add(Resources.LoadAll<SurvivorDef>("SurvivorDefs/"));
            RoR2Content.contentPack.artifactDefs.Add(Resources.LoadAll<ArtifactDef>("ArtifactDefs/"));
            var effectDefs = new List<EffectDef>();
            foreach (var effect in Resources.LoadAll<GameObject>("Prefabs/Effects/")) {
                var effectDef = new EffectDef(effect);
                effectDefs.Add(effectDef);
            }
            RoR2Content.contentPack.effectDefs.Add(effectDefs.ToArray());
            RoR2Content.contentPack.entityStateConfigurations.Add(Resources.LoadAll<EntityStateConfiguration>("EntityStateConfigurations/"));
            RoR2Content.contentPack.entityStateTypes.Add((from type in typeof(EntityState).Assembly.GetTypes()
                                                          where typeof(EntityState).IsAssignableFrom(type)
                                                          select type).ToArray());

            ContentLoadHelper.PopulateTypeFields<ArtifactDef>(typeof(RoR2Content.Artifacts), RoR2Content.contentPack.artifactDefs);
            ContentLoadHelper.PopulateTypeFields<ItemDef>(typeof(RoR2Content.Items), RoR2Content.contentPack.itemDefs);
            ContentLoadHelper.PopulateTypeFields<EquipmentDef>(typeof(RoR2Content.Equipment), RoR2Content.contentPack.equipmentDefs);
            ContentLoadHelper.PopulateTypeFields<BuffDef>(typeof(RoR2Content.Buffs), RoR2Content.contentPack.buffDefs);
            ContentLoadHelper.PopulateTypeFields<EliteDef>(typeof(RoR2Content.Elites), RoR2Content.contentPack.eliteDefs);
            ContentLoadHelper.PopulateTypeFields<GameEndingDef>(typeof(RoR2Content.GameEndings), RoR2Content.contentPack.gameEndingDefs);
            ContentLoadHelper.PopulateTypeFields<SurvivorDef>(typeof(RoR2Content.Survivors), RoR2Content.contentPack.survivorDefs);

            RoR2Content.contentPack.effectDefs.Find("CoinEmitter").cullMethod = (EffectData effectData) => SettingsConVars.cvExpAndMoneyEffects.value;

            _ror2ContentLoaded = true;
        }

        private static System.Collections.IEnumerator LoadOnlyOnce(On.RoR2.RoR2Content.orig_LoadStaticContentAsync orig, RoR2Content self, LoadStaticContentAsyncArgs args) {
            if (!_ror2ContentLoaded) {
                yield return orig(self, args);
            }

            yield break;
        }

        private static void TakeOurInstanceInstead(ILContext il) {
            var c = new ILCursor(il);

            if (c.TryGotoNext(i => i.MatchNewobj<RoR2Content>())) {
                c.Remove();

                static RoR2Content ReturnOurInstance() {
                    return RoR2Content;
                }

                c.EmitDelegate<Func<RoR2Content>>(ReturnOurInstance);
            }
        }
    }
}
