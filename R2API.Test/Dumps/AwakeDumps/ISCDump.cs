using RoR2;
using System;
using System.Text;
using UnityEngine.AddressableAssets;
using Xunit;

namespace R2API.Test.Dumps.AwakeDumps {

    // Create dump for DirectorAPIhelpers.cs

    public class ISCDump {

        [Fact]
        public void Awake() {
            RoR2Application.onLoad += Dump;
        }

        private void Dump() {
            var paths = @"RoR2/Base/Drones/iscBrokenDrone1.asset
RoR2/Base/Drones/iscBrokenDrone2.asset
RoR2/Base/Drones/iscBrokenEmergencyDrone.asset
RoR2/Base/Drones/iscBrokenEquipmentDrone.asset
RoR2/Base/Drones/iscBrokenFlameDrone.asset
RoR2/Base/Drones/iscBrokenMegaDrone.asset
RoR2/Base/Drones/iscBrokenMissileDrone.asset
RoR2/Base/Drones/iscBrokenTurret1.asset
RoR2/Base/Scav/iscScavBackpack.asset
RoR2/Base/Scav/iscScavLunarBackpack.asset
RoR2/Base/Barrel1/iscBarrel1.asset
RoR2/Base/CasinoChest/iscCasinoChest.asset
RoR2/Base/CategoryChest/iscCategoryChestDamage.asset
RoR2/Base/CategoryChest/iscCategoryChestHealing.asset
RoR2/Base/CategoryChest/iscCategoryChestUtility.asset
RoR2/Base/Chest1/iscChest1.asset
RoR2/Base/Chest1StealthedVariant/iscChest1Stealthed.asset
RoR2/Base/Chest2/iscChest2.asset
RoR2/Base/Duplicator/iscDuplicator.asset
RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset
RoR2/Base/DuplicatorMilitary/iscDuplicatorMilitary.asset
RoR2/Base/DuplicatorWild/iscDuplicatorWild.asset
RoR2/Base/EquipmentBarrel/iscEquipmentBarrel.asset
RoR2/Base/GoldChest/iscGoldChest.asset
RoR2/Base/LunarChest/iscLunarChest.asset
RoR2/Base/PortalGoldshores/iscGoldshoresPortal.asset
RoR2/Base/PortalMS/iscMSPortal.asset
RoR2/Base/PortalShop/iscShopPortal.asset
RoR2/Base/RadarTower/iscRadarTower.asset
RoR2/Base/Scrapper/iscScrapper.asset
RoR2/Base/ShrineBlood/iscShrineBlood.asset
RoR2/Base/ShrineBlood/iscShrineBloodSandy.asset
RoR2/Base/ShrineBlood/iscShrineBloodSnowy.asset
RoR2/Base/ShrineBoss/iscShrineBoss.asset
RoR2/Base/ShrineBoss/iscShrineBossSandy.asset
RoR2/Base/ShrineBoss/iscShrineBossSnowy.asset
RoR2/Base/ShrineChance/iscShrineChance.asset
RoR2/Base/ShrineChance/iscShrineChanceSandy.asset
RoR2/Base/ShrineChance/iscShrineChanceSnowy.asset
RoR2/Base/ShrineCleanse/iscShrineCleanse.asset
RoR2/Base/ShrineCleanse/iscShrineCleanseSandy.asset
RoR2/Base/ShrineCleanse/iscShrineCleanseSnowy.asset
RoR2/Base/ShrineCombat/iscShrineCombat.asset
RoR2/Base/ShrineCombat/iscShrineCombatSandy.asset
RoR2/Base/ShrineCombat/iscShrineCombatSnowy.asset
RoR2/Base/ShrineGoldshoresAccess/iscShrineGoldshoresAccess.asset
RoR2/Base/ShrineHealing/iscShrineHealing.asset
RoR2/Base/ShrineRestack/iscShrineRestack.asset
RoR2/Base/ShrineRestack/iscShrineRestackSandy.asset
RoR2/Base/ShrineRestack/iscShrineRestackSnowy.asset
RoR2/Base/Teleporters/iscLunarTeleporter.asset
RoR2/Base/Teleporters/iscTeleporter.asset
RoR2/Base/TripleShop/iscTripleShop.asset
RoR2/Base/TripleShopEquipment/iscTripleShopEquipment.asset
RoR2/Base/TripleShopLarge/iscTripleShopLarge.asset
RoR2/Base/goldshores/iscGoldshoresBeacon.asset
RoR2/DLC1/VoidRaidCrab/iscVoidRaidSafeWard.asset
RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerPortal.asset
RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWard.asset
RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/iscInfiniteTowerSafeWardAwaitingInteraction.asset
RoR2/DLC1/CategoryChest2/iscCategoryChest2Damage.asset
RoR2/DLC1/CategoryChest2/iscCategoryChest2Healing.asset
RoR2/DLC1/CategoryChest2/iscCategoryChest2Utility.asset
RoR2/DLC1/DeepVoidPortal/iscDeepVoidPortal.asset
RoR2/DLC1/DeepVoidPortalBattery/iscDeepVoidPortalBattery.asset
RoR2/DLC1/PortalVoid/iscVoidPortal.asset
RoR2/DLC1/VoidCamp/iscVoidCamp.asset
RoR2/DLC1/VoidChest/iscVoidChest.asset
RoR2/DLC1/VoidCoinBarrel/iscVoidCoinBarrel.asset
RoR2/DLC1/VoidOutroPortal/iscVoidOutroPortal.asset
RoR2/DLC1/VoidSuppressor/iscVoidSuppressor.asset
RoR2/DLC1/VoidTriple/iscVoidTriple.asset
RoR2/DLC1/FreeChest/iscFreeChest.asset
RoR2/DLC1/TreasureCacheVoid/iscLockboxVoid.asset
RoR2/DLC1/gauntlets/iscGauntletEntrance.asset
RoR2/Junk/SquidTurret/iscSquidTurret.asset
RoR2/Junk/TreasureCache/iscLockbox.asset";
            var sb = new StringBuilder();
            foreach (var path in paths.Split('\n')) {
                var asset = Addressables.LoadAssetAsync<RoR2.InteractableSpawnCard>(path.Trim()).WaitForCompletion();
                try {
                    sb.AppendLine($"public static readonly string {Language.GetString(asset.prefab.GetComponent<IDisplayNameProvider>().GetDisplayName()).Replace(" ", "")} = \"{asset.name.ToLowerInvariant()}\";");
                }
                catch (Exception) {
                    sb.AppendLine($"public static readonly string {asset.prefab.name.Replace(" ", "")} = \"{asset.name.ToLowerInvariant()}\";");
                }
            }
            R2APITest.Logger.LogError(sb.ToString());
        }
    }
}
