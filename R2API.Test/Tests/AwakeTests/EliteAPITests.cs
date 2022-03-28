using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xunit;

namespace R2API.Test.Tests.AwakeTests {
    public class EliteAPITests {
        public EliteAPITests() {
        }

        // This test a bunch of thing, an elite, with its custom tier def, so also a custom buff, a custom equipment, and language token additions.
        [Fact]
        public void Test() {

            var defaultIcon = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            Assert.True(defaultIcon);

            var eliteBuffDef = ScriptableObject.CreateInstance<BuffDef>();
            eliteBuffDef.name = "R2APITest_EliteBuffDef";
            eliteBuffDef.buffColor = new Color32(255, 255, 255, byte.MaxValue);
            eliteBuffDef.iconSprite = defaultIcon;
            eliteBuffDef.canStack = false;

            const string EliteAffixToken = "R2APITEST_eliteEquipmentDefAffixToken";
            const string RegisteredToken = "_REGISTERED";
            LanguageAPI.Add(EliteAffixToken, EliteAffixToken + RegisteredToken);

            var eliteEquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            eliteEquipmentDef.name = "ELITE_EQUIPMENT_" + EliteAffixToken;
            LanguageAPI.Add(eliteEquipmentDef.name, eliteEquipmentDef.name + RegisteredToken);

            eliteEquipmentDef.nameToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_NAME";
            LanguageAPI.Add(eliteEquipmentDef.nameToken, eliteEquipmentDef.nameToken + RegisteredToken);

            eliteEquipmentDef.pickupToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_PICKUP";
            LanguageAPI.Add(eliteEquipmentDef.pickupToken, eliteEquipmentDef.pickupToken + RegisteredToken);

            eliteEquipmentDef.descriptionToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_DESCRIPTION";
            LanguageAPI.Add(eliteEquipmentDef.descriptionToken, eliteEquipmentDef.descriptionToken + RegisteredToken);

            eliteEquipmentDef.loreToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_LORETOKEN";
            LanguageAPI.Add(eliteEquipmentDef.loreToken, eliteEquipmentDef.loreToken + RegisteredToken);

            eliteEquipmentDef.pickupModelPrefab = null;
            eliteEquipmentDef.pickupIconSprite = defaultIcon;
            eliteEquipmentDef.appearsInSinglePlayer = true;
            eliteEquipmentDef.appearsInMultiPlayer = true;
            eliteEquipmentDef.canDrop = true;
            eliteEquipmentDef.cooldown = 2;
            eliteEquipmentDef.enigmaCompatible = false;
            eliteEquipmentDef.isBoss = false;
            eliteEquipmentDef.isLunar = false;
            eliteEquipmentDef.passiveBuffDef = eliteBuffDef;

            ItemDisplayRule[] itemDisplayRules = null;
            var customEquipment = new CustomEquipment(eliteEquipmentDef, itemDisplayRules);
            Assert.True(ItemAPI.Add(customEquipment));

            var customEliteTierDefs = new CombatDirector.EliteTierDef[]
            {
                new CombatDirector.EliteTierDef()
                {
                    costMultiplier = CombatDirector.baseEliteCostMultiplier,
                    eliteTypes = Array.Empty<EliteDef>(),
                    isAvailable = SetAvailability,
                },
            };

            foreach (var eliteTierDef in customEliteTierDefs) {
                EliteAPI.AddCustomEliteTier(eliteTierDef);
            }

            var customEliteDef = ScriptableObject.CreateInstance<EliteDef>();
            customEliteDef.healthBoostCoefficient = 2;
            customEliteDef.color = new Color32(255, 0, 0, byte.MaxValue);
            customEliteDef.eliteEquipmentDef = eliteEquipmentDef;
            customEliteDef.damageBoostCoefficient = 2;
            customEliteDef.modifierToken = "R2API_TEST_ELITEDEF_MODIFIERTOKEN";
            LanguageAPI.Add(customEliteDef.modifierToken, customEliteDef.modifierToken + RegisteredToken);

            customEliteDef.shaderEliteRampIndex = 0;

            eliteBuffDef.eliteDef = customEliteDef;

            Assert.True(EliteAPI.Add(new CustomElite(customEliteDef, customEliteTierDefs)));
        }

        private bool SetAvailability(SpawnCard.EliteRules arg) {
            return arg == SpawnCard.EliteRules.Default;
        }
    }
}
