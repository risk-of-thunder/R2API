using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using R2API.Utils;

namespace R2API {
    public static class EliteRamp {
        private static List<(EliteDef, Texture2D)> elitesAndRamps = new();
        private static Dictionary<EliteIndex, Texture2D> eliteIndexToTexture = new();
        private static Texture2D vanillaEliteRamp;
        private static int EliteRampPropertyID => Shader.PropertyToID("_EliteRamp");

        #region Hooks
        internal static async void SetHooks() {
            IL.RoR2.CharacterModel.UpdateMaterials += UpdateRampProperly;
            RoR2Application.onLoad += SetupDictionary;
            vanillaEliteRamp = await Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/ColorRamps/texRampElites.psd").Task;
        }
        internal static void UnsetHooks() {
            IL.RoR2.CharacterModel.UpdateMaterials -= UpdateRampProperly;
            RoR2Application.onLoad -= SetupDictionary;
            vanillaEliteRamp = null;
        }

        private static void UpdateRampProperly(MonoMod.Cil.ILContext il) {
            ILCursor c = new ILCursor(il);
            var firstMatchSuccesful = c.TryGotoNext(MoveType.After,
                                        x => x.MatchLdarg(0),
                                        x => x.MatchLdfld<CharacterModel>(nameof(CharacterModel.propertyStorage)),
                                        x => x.MatchLdsfld(typeof(CommonShaderProperties), nameof(CommonShaderProperties._EliteIndex)));

            var secondMatchSuccesful = c.TryGotoNext(MoveType.After,
                                         x => x.MatchCallOrCallvirt<MaterialPropertyBlock>(nameof(MaterialPropertyBlock.SetFloat)));

            if (firstMatchSuccesful && secondMatchSuccesful) {
                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<CharacterModel>>(UpdateRampProperly);
            }
            else {
                R2API.Logger.LogError($"Elite Ramp ILHook failed");
            }

            static void UpdateRampProperly(CharacterModel charModel) {
                if(charModel.myEliteIndex != EliteIndex.None && eliteIndexToTexture.TryGetValue(charModel.myEliteIndex, out var ramp)) {
                    charModel.propertyStorage.SetTexture(EliteRampPropertyID, ramp);
                    return;
                }
                charModel.propertyStorage.SetTexture(EliteRampPropertyID, vanillaEliteRamp);
            }
        }

        private static void SetupDictionary() {
            foreach((var eliteDef, var texture) in elitesAndRamps) {
                eliteIndexToTexture[eliteDef.eliteIndex] = texture;
            }
            elitesAndRamps.Clear();
        }
        #endregion

        public static void AddRamp(EliteDef eliteDef, Texture2D ramp) {
            try {
                if (eliteDef.shaderEliteRampIndex > 0)
                    eliteDef.shaderEliteRampIndex = 0;

                elitesAndRamps.Add((eliteDef, ramp));
            }
            catch(Exception ex) {
                R2API.Logger.LogError(ex);
            }
        }

        public static void AddRampToMultipleElites(IEnumerable<EliteDef> eliteDefs, Texture2D ramp) {
            foreach(EliteDef def in eliteDefs) {
                AddRamp(def, ramp);
            }
        }
    }
}
