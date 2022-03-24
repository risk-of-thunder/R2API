using RoR2;
using System.Text;
using UnityEngine.AddressableAssets;
using Xunit;

namespace R2API.Test.Dumps.AwakeDumps {

    // Create dump for DirectorAPIhelpers.cs

    public class CSCDump {

        [Fact]
        public void Awake() {
            RoR2Application.onLoad += Dump;
        }

        private void Dump() {
            var paths = @"RoR2/Base/Beetle/cscBeetle.asset
RoR2/Base/Beetle/cscBeetleGuard.asset
RoR2/Base/Beetle/cscBeetleQueen.asset
RoR2/Base/Bell/cscBell.asset
RoR2/Base/Bison/cscBison.asset
RoR2/Base/Brother/cscBrother.asset
RoR2/Base/Brother/cscBrotherHurt.asset
RoR2/Base/ClayBoss/cscClayBoss.asset
RoR2/Base/ClayBruiser/cscClayBruiser.asset
RoR2/Base/Drones/cscBackupDrone.asset
RoR2/Base/Drones/cscDrone1.asset
RoR2/Base/Drones/cscDrone2.asset
RoR2/Base/Drones/cscMegaDrone.asset
RoR2/Base/ElectricWorm/cscElectricWorm.asset
RoR2/Base/Golem/cscGolem.asset
RoR2/Base/Grandparent/cscGrandparent.asset
RoR2/Base/Gravekeeper/cscGravekeeper.asset
RoR2/Base/GreaterWisp/cscGreaterWisp.asset
RoR2/Base/HermitCrab/cscHermitCrab.asset
RoR2/Base/Imp/cscImp.asset
RoR2/Base/ImpBoss/cscImpBoss.asset
RoR2/Base/Jellyfish/cscJellyfish.asset
RoR2/Base/Lemurian/cscLemurian.asset
RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset
RoR2/Base/LunarExploder/cscLunarExploder.asset
RoR2/Base/LunarGolem/cscLunarGolem.asset
RoR2/Base/LunarWisp/cscLunarWisp.asset
RoR2/Base/MagmaWorm/cscMagmaWorm.asset
RoR2/Base/MiniMushroom/cscMiniMushroom.asset
RoR2/Base/Nullifier/cscNullifier.asset
RoR2/Base/Nullifier/cscNullifierAlly.asset
RoR2/Base/Parent/cscParent.asset
RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset
RoR2/Base/RoboBallBoss/cscRoboBallMini.asset
RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset
RoR2/Base/Scav/cscScav.asset
RoR2/Base/Scav/cscScavBoss.asset
RoR2/Base/ScavLunar/cscScavLunar.asset
RoR2/Base/Titan/cscTitanBlackBeach.asset
RoR2/Base/Titan/cscTitanDampCave.asset
RoR2/Base/Titan/cscTitanGolemPlains.asset
RoR2/Base/Titan/cscTitanGooLake.asset
RoR2/Base/Titan/cscTitanGold.asset
RoR2/Base/Vagrant/cscVagrant.asset
RoR2/Base/Vulture/cscVulture.asset
RoR2/Base/Wisp/cscLesserWisp.asset
RoR2/Base/BeetleGland/cscBeetleGuardAlly.asset
RoR2/Base/RoboBallBuddy/cscRoboBallGreenBuddy.asset
RoR2/Base/RoboBallBuddy/cscRoboBallRedBuddy.asset
RoR2/Base/Squid/cscSquidTurret.asset
RoR2/Base/TitanGoldDuringTP/cscTitanGoldAlly.asset
RoR2/DLC1/AcidLarva/cscAcidLarva.asset
RoR2/DLC1/Assassin2/cscAssassin2.asset
RoR2/DLC1/ClayGrenadier/cscClayGrenadier.asset
RoR2/DLC1/DroneCommander/cscDroneCommander.asset
RoR2/DLC1/FlyingVermin/cscFlyingVermin.asset
RoR2/DLC1/FlyingVermin/cscFlyingVerminSnowy.asset
RoR2/DLC1/Gup/cscGeepBody.asset
RoR2/DLC1/Gup/cscGipBody.asset
RoR2/DLC1/Gup/cscGupBody.asset
RoR2/DLC1/MajorAndMinorConstruct/cscMajorConstruct.asset
RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset
RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset
RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstructAttachable.asset
RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstructOnKill.asset
RoR2/DLC1/Vermin/cscVermin.asset
RoR2/DLC1/Vermin/cscVerminSnowy.asset
RoR2/DLC1/VoidBarnacle/cscVoidBarnacle.asset
RoR2/DLC1/VoidBarnacle/cscVoidBarnacleAlly.asset
RoR2/DLC1/VoidBarnacle/cscVoidBarnacleNoCast.asset
RoR2/DLC1/VoidJailer/cscVoidJailer.asset
RoR2/DLC1/VoidJailer/cscVoidJailerAlly.asset
RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset
RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrab.asset
RoR2/DLC1/VoidRaidCrab/cscVoidRaidCrabJoint.asset
RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabBase.asset
RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset
RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset
RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset
RoR2/DLC1/EliteVoid/cscVoidInfestor.asset
RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/cscBrotherIT.asset
RoR2/Junk/ArchWisp/cscArchWisp.asset
RoR2/Junk/BeetleCrystal/cscBeetleCrystal.asset
RoR2/Junk/BeetleGuardCrystal/cscBeetleGuardCrystal.asset
RoR2/Junk/BrotherGlass/cscBrotherGlass.asset
RoR2/Junk/Incubator/cscParentPod.asset";
            var sb = new StringBuilder();
            foreach (var path in paths.Split('\n')) {
                var asset = Addressables.LoadAssetAsync<RoR2.CharacterSpawnCard>(path.Trim()).WaitForCompletion();
                sb.AppendLine($"public static readonly string {asset.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>().GetDisplayName().Replace(" ", "")} = \"{asset.name.ToLowerInvariant()}\";");
            }
            R2APITest.Logger.LogError(sb.ToString());
        }
    }
}
