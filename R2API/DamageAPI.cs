using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using R2API.Utils;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace R2API {
    /// <summary>
    /// API for handling deployables added by mods
    /// </summary>
    [R2APISubmodule]
    public static class DamageAPI {
        public enum ModdedDamageType { };

        private const byte flagsPerValue = 8;
        private const byte valuesPerBlock = 18;
        private const byte valuesPerSection = flagsPerValue * valuesPerBlock;
        private const byte sectionsCount = 8;
        private const byte blockPartsCount = 4;

        private static readonly ConditionalWeakTable<object, ModdedDamageTypeHolder> damageTypeHolders = new();

        private static ModdedDamageTypeHolder TempHolder { get; set; }

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;

        /// <summary>
        /// Reserved ModdedDamageTypes count
        /// </summary>
        public static int ModdedDamageTypeCount { get; private set; }


        #region Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo += WriteDamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo += ReadDamageInfo;

            IL.RoR2.BulletAttack.DefaultHitCallback += BulletAttackDefaultHitCallbackIL;
            IL.RoR2.Orbs.DamageOrb.OnArrival += DamageOrbOnArrivalIL;
            IL.RoR2.Orbs.GenericDamageOrb.OnArrival += GenericDamageOrbOnArrivalIL;
            IL.RoR2.Orbs.LightningOrb.OnArrival += LightningOrbOnArrivalIL;
            IL.RoR2.Projectile.DeathProjectile.FixedUpdate += DeathProjectileFixedUpdateIL;
            IL.RoR2.Projectile.ProjectileGrantOnKillOnDestroy.OnDestroy += ProjectileGrantOnKillOnDestroyOnDestroyIL;
            IL.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact += ProjectileSingleTargetImpactOnProjectileImpactIL;

            IL.RoR2.DotController.EvaluateDotStacksForType += DotControllerEvaluateDotStacksForTypeIL;
            IL.RoR2.DotController.AddPendingDamageEntry += DotControllerAddPendingDamageEntryIL;

            IL.RoR2.BlastAttack.HandleHits += BlastAttackHandleHitsIL;
            IL.RoR2.BlastAttack.PerformDamageServer += BlastAttackPerformDamageServerIL;
            //MMHook can't handle private structs in parameters of On hooks
            HookEndpointManager.Add(
                typeof(RoR2.BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Write)),
                (BlastAttackDamageInfoWriteDelegate)BlastAttackDamageInfoWrite);
            HookEndpointManager.Add(
                typeof(RoR2.BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Read)),
                (BlastAttackDamageInfoReadDelegate)BlastAttackDamageInfoRead);

            IL.RoR2.OverlapAttack.ProcessHits += OverlapAttackProcessHitsIL;
            IL.RoR2.OverlapAttack.PerformDamage += OverlapAttackPerformDamageIL;
            On.RoR2.OverlapAttack.OverlapAttackMessage.Serialize += OverlapAttackMessageSerialize;
            On.RoR2.OverlapAttack.OverlapAttackMessage.Deserialize += OverlapAttackMessageDeserialize;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo -= WriteDamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo -= ReadDamageInfo;

            IL.RoR2.BulletAttack.DefaultHitCallback -= BulletAttackDefaultHitCallbackIL;
            IL.RoR2.Orbs.DamageOrb.OnArrival -= DamageOrbOnArrivalIL;
            IL.RoR2.Orbs.GenericDamageOrb.OnArrival -= GenericDamageOrbOnArrivalIL;
            IL.RoR2.Orbs.LightningOrb.OnArrival -= LightningOrbOnArrivalIL;
            IL.RoR2.Projectile.DeathProjectile.FixedUpdate -= DeathProjectileFixedUpdateIL;
            IL.RoR2.Projectile.ProjectileGrantOnKillOnDestroy.OnDestroy -= ProjectileGrantOnKillOnDestroyOnDestroyIL;
            IL.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact -= ProjectileSingleTargetImpactOnProjectileImpactIL;

            IL.RoR2.DotController.EvaluateDotStacksForType -= DotControllerEvaluateDotStacksForTypeIL;
            IL.RoR2.DotController.AddPendingDamageEntry -= DotControllerAddPendingDamageEntryIL;

            IL.RoR2.BlastAttack.HandleHits -= BlastAttackHandleHitsIL;
            IL.RoR2.BlastAttack.PerformDamageServer -= BlastAttackPerformDamageServerIL;
            //MMHook can't handle private structs in parameters of On hooks
            HookEndpointManager.Remove(
                typeof(RoR2.BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Write)),
                (BlastAttackDamageInfoWriteDelegate)BlastAttackDamageInfoWrite);
            HookEndpointManager.Remove(
                typeof(RoR2.BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Read)),
                (BlastAttackDamageInfoReadDelegate)BlastAttackDamageInfoRead);

            IL.RoR2.OverlapAttack.ProcessHits -= OverlapAttackProcessHitsIL;
            IL.RoR2.OverlapAttack.PerformDamage -= OverlapAttackPerformDamageIL;
            On.RoR2.OverlapAttack.OverlapAttackMessage.Serialize -= OverlapAttackMessageSerialize;
            On.RoR2.OverlapAttack.OverlapAttackMessage.Deserialize -= OverlapAttackMessageDeserialize;
        }

        private static void OverlapAttackMessageDeserialize(On.RoR2.OverlapAttack.OverlapAttackMessage.orig_Deserialize orig, MessageBase self, NetworkReader reader) {
            orig(self, reader);

            var holder = ModdedDamageTypeHolder.FromNetworkReader(reader);
            if (holder != null) {
                TempHolder = holder;
            }
        }

        private static void OverlapAttackMessageSerialize(On.RoR2.OverlapAttack.OverlapAttackMessage.orig_Serialize orig, MessageBase self, NetworkWriter writer) {
            orig(self, writer);


            var holder = TempHolder;
            if (holder == null) {
                writer.Write((byte)0);
                return;
            }

            holder.Write(writer);
        }

        private static void OverlapAttackPerformDamageIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldloc, damageInfoIndex);
            EmitGetTempHolder(c);
            EmitCopyCall(c);
        }

        private static void OverlapAttackProcessHitsIL(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(x => x.MatchCall<NetworkServer>("get_active"));

            c.Emit(OpCodes.Ldarg_0);
            EmitSetTempHolder(c);
        }

        private static void BlastAttackDamageInfoRead(BlastAttackDamageInfoReadOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader) {
            orig(ref self, networkReader);

            var holder = ModdedDamageTypeHolder.FromNetworkReader(networkReader);
            if (holder != null) {
                TempHolder = holder;
            }
        }

        private static void BlastAttackDamageInfoWrite(BlastAttackDamageInfoWriteOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter) {
            orig(ref self, networkWriter);

            var holder = TempHolder;
            if (holder == null) {
                networkWriter.Write((byte)0);
                return;
            }

            holder.Write(networkWriter);
        }

        private static void BlastAttackPerformDamageServerIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldloc, damageInfoIndex);
            EmitGetTempHolder(c);
            EmitCopyCall(c);
        }

        private static void BlastAttackHandleHitsIL(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(x => x.MatchCall<NetworkServer>("get_active"));

            c.Emit(OpCodes.Ldarg_0);
            EmitSetTempHolder(c);
        }

        private static void DotControllerAddPendingDamageEntryIL(ILContext il) {
            var c = new ILCursor(il);

            var pendingDamageIndex = -1;
            c.GotoNext(
                x => x.MatchLdloc(out pendingDamageIndex),
                x => x.MatchLdarg(3),
                x => x.MatchStfld(out _));

            c.Emit(OpCodes.Ldloc, pendingDamageIndex);
            EmitGetTempHolder(c);
            EmitCopyCall(c);
        }

        private static void DotControllerEvaluateDotStacksForTypeIL(ILContext il) {
            var c = new ILCursor(il);

            var dotStackIndex = -1;
            c.GotoNext(
                MoveType.After,
                x => x.MatchLdloc(out dotStackIndex),
                x => x.MatchLdfld<DotController.DotStack>(nameof(DotController.DotStack.dotIndex)),
                x => x.MatchLdarg(1),
                x => x.MatchBneUn(out _));

            c.Emit(OpCodes.Ldloc, dotStackIndex);
            EmitSetTempHolder(c);

            var damageInfoIndex = -1;
            c.GotoNext(
                x => x.MatchNewobj<DamageInfo>(),
                x => x.MatchStloc(out damageInfoIndex));

            c.GotoNext(x => x.MatchLdfld<DotController.PendingDamage>(nameof(DotController.PendingDamage.damageType)));

            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldloc, damageInfoIndex);
            EmitCopyCall(c);
        }

        private static void EmitGetTempHolder(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetGetMethod(true));

        private static void EmitSetTempHolder(ILCursor c) {
            c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(MakeCopyOfModdedDamageTypeFromObject)));
            c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetSetMethod(true));
        }

        private static void LightningOrbOnArrivalIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, damageInfoIndex);

            EmitCopyCall(c);
        }

        private static void GenericDamageOrbOnArrivalIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, damageInfoIndex);

            EmitCopyCall(c);
        }

        private static void ProjectileSingleTargetImpactOnProjectileImpactIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.GotoNext(
                x => x.MatchLdloc(damageInfoIndex),
                x => x.MatchLdarg(0));

            c.Emit(OpCodes.Ldarg_0);
            c.Emit<RoR2.Projectile.ProjectileSingleTargetImpact>(OpCodes.Ldfld, nameof(RoR2.Projectile.ProjectileSingleTargetImpact.projectileDamage));
            c.Emit(OpCodes.Ldloc, damageInfoIndex);
            
            EmitCopyCall(c);
        }

        private static void ProjectileGrantOnKillOnDestroyOnDestroyIL(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchNewobj<DamageInfo>());
            
            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<RoR2.Projectile.ProjectileGrantOnKillOnDestroy>(OpCodes.Ldfld, nameof(RoR2.Projectile.ProjectileGrantOnKillOnDestroy.projectileDamage));
            
            EmitCopyInversedCall(c);
        }

        private static void DeathProjectileFixedUpdateIL(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(
                MoveType.After,
                x => x.MatchNewobj<DamageInfo>());

            c.Emit(OpCodes.Dup);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit<RoR2.Projectile.DeathProjectile>(OpCodes.Ldfld, nameof(RoR2.Projectile.DeathProjectile.projectileDamage));
            
            EmitCopyInversedCall(c);
        }

        private static void DamageOrbOnArrivalIL(ILContext il) {
            var c = new ILCursor(il);
            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, damageInfoIndex);

            EmitCopyCall(c);
        }

        private static void BulletAttackDefaultHitCallbackIL(ILContext il) {
            var c = new ILCursor(il);

            var damageInfoIndex = GotoDamageInfo(c);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, damageInfoIndex);

            EmitCopyCall(c);
        }

        private static int GotoDamageInfo(ILCursor c) {
            int damageInfoIndex = -1;
            c.GotoNext(
                MoveType.After,
                x => x.MatchNewobj<DamageInfo>(),
                x => x.MatchStloc(out damageInfoIndex));

            return damageInfoIndex;
        }

        private static void EmitCopyCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(CopyModdedDamageType)));
        private static void EmitCopyInversedCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(CopyModdedDamageTypeInversed)));

        private static void CopyModdedDamageTypeInversed(object to, object from) => CopyModdedDamageType(from, to);
        private static void CopyModdedDamageType(object from, object to) {
            if (from == null || to == null) {
                return;
            }
            if (damageTypeHolders.TryGetValue(from, out var holder)) {
                damageTypeHolders.Remove(to);
                damageTypeHolders.Add(to, holder.MakeCopy());
            }
        }

        private static ModdedDamageTypeHolder MakeCopyOfModdedDamageTypeFromObject(object from) {
            if (from == null) {
                return null;
            }

            if (damageTypeHolders.TryGetValue(from, out var holder)) {
                return holder.MakeCopy();
            }

            return null;
        }

        private static RoR2.DamageInfo ReadDamageInfo(On.RoR2.NetworkExtensions.orig_ReadDamageInfo orig, UnityEngine.Networking.NetworkReader reader) {
            var damageInfo = orig(reader);

            var holder = ModdedDamageTypeHolder.FromNetworkReader(reader);
            if (holder != null) {
                damageTypeHolders.Add(damageInfo, holder);
            }

            return damageInfo;
        }

        private static void WriteDamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, UnityEngine.Networking.NetworkWriter writer, RoR2.DamageInfo damageInfo) {
            orig(writer, damageInfo);

            if (!damageTypeHolders.TryGetValue(damageInfo, out var holder)) {
                writer.Write((byte)0);
                return;
            }

            holder.Write(writer);
        }
        #endregion

        #region Public
        /// <summary>
        /// Reserve ModdedDamageType to use it with
        /// <see cref="DamageAPI.AddModdedDamageType(DamageInfo, ModdedDamageType)"/> and
        /// <see cref="DamageAPI.HasModdedDamageType(DamageInfo, ModdedDamageType)"/>
        /// </summary>
        /// <returns></returns>
        public static ModdedDamageType ReserveDamageType() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if (ModdedDamageTypeCount >= sectionsCount * valuesPerSection) {
                //I doubt this ever gonna happen, but just in case.
                throw new IndexOutOfRangeException($"Reached the limit of {sectionsCount * valuesPerSection} ModdedDamageTypes. Please contact R2API developers to increase the limit");
            }

            return (ModdedDamageType)ModdedDamageTypeCount++;
        }

        #region AddModdedDamageType
        /// <summary>
        /// Adding ModdedDamageType to DamageInfo instance. You can add more than one damage type to one DamageInfo
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(damageInfo, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to BulletAttack instance. You can add more than one damage type to one BulletAttack
        /// </summary>
        /// <param name="bulletAttack"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(bulletAttack, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to DamageOrb instance. You can add more than one damage type to one DamageOrb
        /// </summary>
        /// <param name="damageOrb"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(damageOrb, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to GenericDamageOrb instance. You can add more than one damage type to one GenericDamageOrb
        /// </summary>
        /// <param name="genericDamageOrb"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(genericDamageOrb, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to LightningOrb instance. You can add more than one damage type to one LightningOrb
        /// </summary>
        /// <param name="lightningOrb"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(lightningOrb, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to ProjectileDamage instance. You can add more than one damage type to one ProjectileDamage
        /// </summary>
        /// <param name="projectileDamage"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this ProjectileDamage projectileDamage, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(projectileDamage, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to BlastAttack instance. You can add more than one damage type to one BlastAttack
        /// </summary>
        /// <param name="blastAttack"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(blastAttack, moddedDamageType);

        /// <summary>
        /// Adding ModdedDamageType to OverlapAttack instance. You can add more than one damage type to one OverlapAttack
        /// </summary>
        /// <param name="overlapAttack"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(overlapAttack, moddedDamageType);

        private static void AddModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0) {
                throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
            }

            if (!damageTypeHolders.TryGetValue(obj, out var holder)) {
                damageTypeHolders.Add(obj, holder = new ModdedDamageTypeHolder());
            }

            holder.Add(moddedDamageType);
        }
        #endregion

        #region HasModdedDamageType
        /// <summary>
        /// Checks if DamageInfo instance has ModdedDamageType assigned. One DamageInfo can have more than one damage type.
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(damageInfo, moddedDamageType);

        /// <summary>
        /// Checks if BulletAttack instance has ModdedDamageType assigned. One BulletAttack can have more than one damage type.
        /// </summary>
        /// <param name="bulletAttack"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(bulletAttack, moddedDamageType);

        /// <summary>
        /// Checks if DamageOrb instance has ModdedDamageType assigned. One DamageOrb can have more than one damage type.
        /// </summary>
        /// <param name="damageOrb"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(damageOrb, moddedDamageType);

        /// <summary>
        /// Checks if GenericDamageOrb instance has ModdedDamageType assigned. One GenericDamageOrb can have more than one damage type.
        /// </summary>
        /// <param name="genericDamageOrb"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(genericDamageOrb, moddedDamageType);

        /// <summary>
        /// Checks if LightningOrb instance has ModdedDamageType assigned. One LightningOrb can have more than one damage type.
        /// </summary>
        /// <param name="lightningOrb"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(lightningOrb, moddedDamageType);

        /// <summary>
        /// Checks if ProjectileDamage instance has ModdedDamageType assigned. One ProjectileDamage can have more than one damage type.
        /// </summary>
        /// <param name="projectileDamage"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this ProjectileDamage projectileDamage, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(projectileDamage, moddedDamageType);

        /// <summary>
        /// Checks if BlastAttack instance has ModdedDamageType assigned. One BlastAttack can have more than one damage type.
        /// </summary>
        /// <param name="blastAttack"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(blastAttack, moddedDamageType);

        /// <summary>
        /// Checks if OverlapAttack instance has ModdedDamageType assigned. One OverlapAttack can have more than one damage type.
        /// </summary>
        /// <param name="overlapAttack"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(overlapAttack, moddedDamageType);

        private static bool HasModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0) {
                throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
            }

            if (!damageTypeHolders.TryGetValue(obj, out var holder)) {
                return false;
            }

            return holder.Has(moddedDamageType);
        }
        #endregion
        #endregion

        /// <summary>
        /// Holds flag values of ModdedDamageType.
        /// </summary>
        public class ModdedDamageTypeHolder {
            private const uint blockValuesMask = 0b_00111111_00011111_00001111_00000111;
            private const uint fullBlockHeader = 0b_00000000_01000000_01100000_01110000;

            private const uint block1HeaderMask = 0b_01000000;
            private const uint block2HeaderMask = 0b_01100000;
            private const uint block3HeaderMask = 0b_01110000;
            private const uint block4HeaderMask = 0b_01111000;

            private const uint block1HeaderXor = 0b_00000000;
            private const uint block2HeaderXor = 0b_01000000;
            private const uint block3HeaderXor = 0b_01100000;
            private const uint block4HeaderXor = 0b_01110000;

            private const uint highestBitInInt = 0b_10000000_00000000_00000000_00000000;
            private const uint highestBitInByte = 0b_10000000;

            private byte[] values;

            /// <summary>
            /// Enable ModdedDamageType for this instance
            /// </summary>
            /// <param name="moddedDamageType"></param>
            public void Add(ModdedDamageType moddedDamageType) {
                var valueIndex = (int)moddedDamageType / flagsPerValue;
                var flagIndex = (int)moddedDamageType % flagsPerValue;

                ResizeIfNeeded(valueIndex);

                values[valueIndex] = (byte)(values[valueIndex] | highestBitInByte >> flagIndex);
            }

            /// <summary>
            /// Checks if ModdedDamageType is enabled
            /// </summary>
            /// <param name="moddedDamageType"></param>
            /// <returns></returns>
            public bool Has(ModdedDamageType moddedDamageType) {
                var valueIndex = (int)moddedDamageType / flagsPerValue;
                var flagIndex = (int)moddedDamageType % flagsPerValue;

                if (values == null || valueIndex >= values.Length) {
                    return false;
                }

                return (values[valueIndex] & (highestBitInByte >> flagIndex)) != 0;
            }

            #region CopyTo
            /// <summary>
            /// Copies enabled ModdedDamageTypes to the DamageInfo instance (completely replacing already set values)
            /// </summary>
            /// <param name="damageInfo"></param>
            public void CopyTo(DamageInfo damageInfo) => CopyToInternal(damageInfo);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the BulletAttack instance (completely replacing already set values)
            /// </summary>
            /// <param name="bulletAttack"></param>
            public void CopyTo(BulletAttack bulletAttack) => CopyToInternal(bulletAttack);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the DamageOrb instance (completely replacing already set values)
            /// </summary>
            /// <param name="damageOrb"></param>
            public void CopyTo(DamageOrb damageOrb) => CopyToInternal(damageOrb);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the GenericDamageOrb instance (completely replacing already set values)
            /// </summary>
            /// <param name="genericDamageOrb"></param>
            public void CopyTo(GenericDamageOrb genericDamageOrb) => CopyToInternal(genericDamageOrb);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the LightningOrb instance (completely replacing already set values)
            /// </summary>
            /// <param name="lightningOrb"></param>
            public void CopyTo(LightningOrb lightningOrb) => CopyToInternal(lightningOrb);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the ProjectileDamage instance (completely replacing already set values)
            /// </summary>
            /// <param name="projectileDamage"></param>
            public void CopyTo(ProjectileDamage projectileDamage) => CopyToInternal(projectileDamage);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the BlastAttack instance (completely replacing already set values)
            /// </summary>
            /// <param name="blastAttack"></param>
            public void CopyTo(BlastAttack blastAttack) => CopyToInternal(blastAttack);

            /// <summary>
            /// Copies enabled ModdedDamageTypes to the OverlapAttack instance (completely replacing already set values)
            /// </summary>
            /// <param name="overlapAttack"></param>
            public void CopyTo(OverlapAttack overlapAttack) => CopyToInternal(overlapAttack);

            private void CopyToInternal(object obj) {
                damageTypeHolders.Remove(obj);
                damageTypeHolders.Add(obj, MakeCopy());
            }
            #endregion

            /// <summary>
            /// Makes a copy of this instance 
            /// </summary>
            /// <returns></returns>
            public ModdedDamageTypeHolder MakeCopy() {
                var holder = new ModdedDamageTypeHolder {
                    values = values?.ToArray()
                };
                return holder;
            }

            /// <summary>
            /// Reads compressed value from the NerworkReader. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            public static ModdedDamageTypeHolder FromNetworkReader(NetworkReader reader) {
                var sectionByte = reader.ReadByte();
                if (sectionByte == 0) {
                    return null;
                }
                var holder = new ModdedDamageTypeHolder();

                for (var i = 0; i < 8; i++) {
                    if ((sectionByte & 1 << i) != 0) {
                        holder.ReadBlock(reader, i);
                    }
                }

                return holder;
            }

            private void ReadBlock(NetworkReader reader, int blockIndex) {
                var fullBlockMask = new FullBlockMask();

                var maskIndex = 0;
                while (true) {
                    var blockBytes = reader.ReadByte();
                    uint mask, xor;
                    do {
                        (mask, xor) = GetMask(maskIndex++);
                    }
                    while ((blockBytes & mask ^ xor) != 0);

                    fullBlockMask[maskIndex - 1] = blockBytes;

                    if ((blockBytes & highestBitInByte) != 0) {
                        break;
                    }
                }

                var bitesSkipped = 0;
                for (var i = 0; i < 32; i++) {
                    if ((blockValuesMask & highestBitInInt >> i) == 0) {
                        bitesSkipped++;
                        continue;
                    }
                    if ((fullBlockMask.integer & highestBitInInt >> i) != 0) {
                        var valueIndex = (blockIndex * valuesPerBlock) + i - bitesSkipped;
                        ResizeIfNeeded(valueIndex);
                        values[valueIndex] = reader.ReadByte();
                    }
                }
            }

            private void ResizeIfNeeded(int valueIndex) {
                if (values == null) {
                    values = new byte[valueIndex + 1];
                }
                if (valueIndex >= values.Length) {
                    Array.Resize(ref values, valueIndex + 1);
                }
            }

            /// <summary>
            /// Writes compressed value to the NerworkWriter. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
            /// </summary>
            /// <param name="writer"></param>
            public void Write(NetworkWriter writer) {
                int section = 0;
                for (var i = 0; i < sectionsCount; i++) {
                    if (!IsBlockEmpty(i)) {
                        section |= 1 << i;
                    }
                }
                writer.Write((byte)section);

                for (var i = 0; i < sectionsCount; i++) {
                    if (!IsBlockEmpty(i)) {
                        WriteBlock(writer, i);
                    }
                }
            }

            private void WriteBlock(NetworkWriter writer, int blockIndex) {
                var bitesSkipped = 0;
                var fullBlockMask = new FullBlockMask();
                var orderedValues = new List<byte>();
                for (var i = 0; i < 32; i++) {
                    fullBlockMask.integer <<= 1;
                    if ((blockValuesMask & highestBitInInt >> i) == 0) {
                        bitesSkipped++;
                        continue;
                    }
                    var valueIndex = blockIndex * valuesPerBlock + (i - bitesSkipped);
                    if (valueIndex >= values.Length) {
                        continue;
                    }
                    if (values[valueIndex] != 0) {
                        fullBlockMask.integer |= 1;
                        orderedValues.Add(values[valueIndex]);
                    }
                }
                var lastIndex = 0;
                for (var i = 0; i < 4; i++) {
                    if (fullBlockMask[i] != 0) {
                        lastIndex = i;
                    }
                }

                fullBlockMask.integer |= fullBlockHeader;
                fullBlockMask[lastIndex] = (byte)(fullBlockMask[lastIndex] | highestBitInByte);

                for (var i = 0; i <= lastIndex; i++) {
                    var (headerMask, _) = GetMask(i);
                    if ((fullBlockMask[i] & (~headerMask)) != 0) {
                        writer.Write(fullBlockMask[i]);
                    }
                }
                foreach (var value in orderedValues) {
                    writer.Write(value);
                }
            }

            private bool IsBlockEmpty(int blockIndex) {
                if (values == null || values.Length == 0 || values.Length / valuesPerBlock < blockIndex) {
                    return true;
                }

                for (var i = blockIndex * valuesPerBlock; i < Math.Min((blockIndex + 1) * valuesPerBlock, values.Length); i++) {
                    if (values[i] != 0) {
                        return false;
                    }
                }

                return true;
            }

            private static (uint mask, uint xor) GetMask(int i) {
                return i switch {
                    0 => (block1HeaderMask, block1HeaderXor),
                    1 => (block2HeaderMask, block2HeaderXor),
                    2 => (block3HeaderMask, block3HeaderXor),
                    3 => (block4HeaderMask, block4HeaderXor),
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FullBlockMask {
            [FieldOffset(3)]
            public byte byte0;
            [FieldOffset(2)]
            public byte byte1;
            [FieldOffset(1)]
            public byte byte2;
            [FieldOffset(0)]
            public byte byte3;

            [FieldOffset(0)]
            public uint integer;

            public byte this[int i] {
                get {
                    return i switch {
                        0 => byte0,
                        1 => byte1,
                        2 => byte2,
                        3 => byte3,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set {
                    switch (i) {
                        case 0:
                            byte0 = value;
                            break;
                        case 1:
                            byte1 = value;
                            break;
                        case 2:
                            byte2 = value;
                            break;
                        case 3:
                            byte3 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }

        private delegate void BlastAttackDamageInfoWriteDelegate(BlastAttackDamageInfoWriteOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter);
        private delegate void BlastAttackDamageInfoWriteOrig(ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter);

        private delegate void BlastAttackDamageInfoReadDelegate(BlastAttackDamageInfoReadOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader);
        private delegate void BlastAttackDamageInfoReadOrig(ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader);
    }
}
