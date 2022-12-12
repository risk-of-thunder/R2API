using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// API for handling DamageTypes added by mods
/// </summary>

[AutoVersion]
public static partial class DamageAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".damagetype";
    public const string PluginName = R2API.PluginName + ".DamageType";

    public enum ModdedDamageType { };

    private static readonly ConditionalWeakTable<object, ModdedDamageTypeHolder> damageTypeHolders = new();

    private static ModdedDamageTypeHolder TempHolder { get; set; }

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    /// <summary>
    /// Reserved ModdedDamageTypes count
    /// </summary>
    public static int ModdedDamageTypeCount { get; private set; }

    private static bool _hooksEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo += WriteDamageInfo;
        On.RoR2.NetworkExtensions.ReadDamageInfo += ReadDamageInfo;

        IL.RoR2.BulletAttack.DefaultHitCallbackImplementation += BulletAttackDefaultHitCallbackIL;

        IL.RoR2.Orbs.DamageOrb.OnArrival += DamageOrbOnArrivalIL;
        IL.RoR2.Orbs.GenericDamageOrb.OnArrival += GenericDamageOrbOnArrivalIL;
        IL.RoR2.Orbs.LightningOrb.OnArrival += LightningOrbOnArrivalIL;
        IL.RoR2.Orbs.ChainGunOrb.OnArrival += ChainGunOrbOnArrivalIL;

        IL.RoR2.Projectile.DeathProjectile.FixedUpdate += DeathProjectileFixedUpdateIL;
        IL.RoR2.Projectile.ProjectileDotZone.ResetOverlap += ProjectileDotZoneResetOverlapIL;
        IL.RoR2.Projectile.ProjectileExplosion.DetonateServer += ProjectileExplosionDetonateServerIL;
        IL.RoR2.Projectile.ProjectileGrantOnKillOnDestroy.OnDestroy += ProjectileGrantOnKillOnDestroyOnDestroyIL;
        IL.RoR2.Projectile.ProjectileIntervalOverlapAttack.FixedUpdate += ProjectileIntervalOverlapAttackFixedUpdateIL;
        IL.RoR2.Projectile.ProjectileOverlapAttack.Start += ProjectileOverlapAttackStartIL;
        IL.RoR2.Projectile.ProjectileOverlapAttack.ResetOverlapAttack += ProjectileOverlapAttackResetOverlapAttackIL;
        IL.RoR2.Projectile.ProjectileProximityBeamController.UpdateServer += ProjectileProximityBeamControllerUpdateServerIL;
        IL.RoR2.Projectile.ProjectileSingleTargetImpact.OnProjectileImpact += ProjectileSingleTargetImpactOnProjectileImpactIL;

        IL.RoR2.DotController.EvaluateDotStacksForType += DotControllerEvaluateDotStacksForTypeIL;
        IL.RoR2.DotController.AddPendingDamageEntry += DotControllerAddPendingDamageEntryIL;

        IL.RoR2.BlastAttack.HandleHits += BlastAttackHandleHitsIL;
        IL.RoR2.BlastAttack.PerformDamageServer += BlastAttackPerformDamageServerIL;
        //MMHook can't handle private structs in parameters of On hooks
        HookEndpointManager.Add(
            typeof(BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Write)),
            (BlastAttackDamageInfoWriteDelegate)BlastAttackDamageInfoWrite);
        HookEndpointManager.Add(
            typeof(BlastAttack.BlastAttackDamageInfo).GetMethodCached(nameof(BlastAttack.BlastAttackDamageInfo.Read)),
            (BlastAttackDamageInfoReadDelegate)BlastAttackDamageInfoRead);

        IL.RoR2.OverlapAttack.ProcessHits += OverlapAttackProcessHitsIL;
        IL.RoR2.OverlapAttack.PerformDamage += OverlapAttackPerformDamageIL;
        On.RoR2.OverlapAttack.OverlapAttackMessage.Serialize += OverlapAttackMessageSerialize;
        On.RoR2.OverlapAttack.OverlapAttackMessage.Deserialize += OverlapAttackMessageDeserialize;

        IL.RoR2.GlobalEventManager.OnHitAll += GlobalEventManagerOnHitAllIL;

        IL.RoR2.HealthComponent.SendDamageDealt += HealthComponentSendDamageDealtIL;
        On.RoR2.DamageDealtMessage.Serialize += DamageDealtMessageSerialize;
        On.RoR2.DamageDealtMessage.Deserialize += DamageDealtMessageDeserialize;

        IL.RoR2.ContactDamage.FireOverlaps += ContactDamageFireOverlapsIL;

        IL.RoR2.DelayBlast.Detonate += DelayBlastDetonateIL;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo -= WriteDamageInfo;
        On.RoR2.NetworkExtensions.ReadDamageInfo -= ReadDamageInfo;

        IL.RoR2.BulletAttack.DefaultHitCallbackImplementation -= BulletAttackDefaultHitCallbackIL;

        IL.RoR2.Orbs.DamageOrb.OnArrival -= DamageOrbOnArrivalIL;
        IL.RoR2.Orbs.GenericDamageOrb.OnArrival -= GenericDamageOrbOnArrivalIL;
        IL.RoR2.Orbs.LightningOrb.OnArrival -= LightningOrbOnArrivalIL;
        IL.RoR2.Orbs.ChainGunOrb.OnArrival -= ChainGunOrbOnArrivalIL;

        IL.RoR2.Projectile.DeathProjectile.FixedUpdate -= DeathProjectileFixedUpdateIL;
        IL.RoR2.Projectile.ProjectileDotZone.ResetOverlap -= ProjectileDotZoneResetOverlapIL;
        IL.RoR2.Projectile.ProjectileExplosion.DetonateServer -= ProjectileExplosionDetonateServerIL;
        IL.RoR2.Projectile.ProjectileGrantOnKillOnDestroy.OnDestroy -= ProjectileGrantOnKillOnDestroyOnDestroyIL;
        IL.RoR2.Projectile.ProjectileIntervalOverlapAttack.FixedUpdate -= ProjectileIntervalOverlapAttackFixedUpdateIL;
        IL.RoR2.Projectile.ProjectileOverlapAttack.Start -= ProjectileOverlapAttackStartIL;
        IL.RoR2.Projectile.ProjectileOverlapAttack.ResetOverlapAttack -= ProjectileOverlapAttackResetOverlapAttackIL;
        IL.RoR2.Projectile.ProjectileProximityBeamController.UpdateServer -= ProjectileProximityBeamControllerUpdateServerIL;
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

        IL.RoR2.GlobalEventManager.OnHitAll -= GlobalEventManagerOnHitAllIL;

        IL.RoR2.HealthComponent.SendDamageDealt -= HealthComponentSendDamageDealtIL;
        On.RoR2.DamageDealtMessage.Serialize -= DamageDealtMessageSerialize;
        On.RoR2.DamageDealtMessage.Deserialize -= DamageDealtMessageDeserialize;

        IL.RoR2.ContactDamage.FireOverlaps -= ContactDamageFireOverlapsIL;

        IL.RoR2.DelayBlast.Detonate -= DelayBlastDetonateIL;

        _hooksEnabled = false;
    }

    #region DamageInfo
    private static DamageInfo ReadDamageInfo(On.RoR2.NetworkExtensions.orig_ReadDamageInfo orig, NetworkReader reader)
    {
        var damageInfo = orig(reader);

        var holder = ModdedDamageTypeHolder.ReadFromNetworkReader(reader);
        if (holder != null)
        {
            damageTypeHolders.Add(damageInfo, holder);
        }

        return damageInfo;
    }

    private static void WriteDamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, NetworkWriter writer, DamageInfo damageInfo)
    {
        orig(writer, damageInfo);

        if (!damageTypeHolders.TryGetValue(damageInfo, out var holder))
        {
            writer.Write((byte)0);
            return;
        }

        holder.WriteToNetworkWriter(writer);
    }
    #endregion

    #region BulletAttack
    private static void BulletAttackDefaultHitCallbackIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyCall(c);
    }
    #endregion

    #region Orbs
    private static void LightningOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyCall(c);

        var bouncingLightningOrbIndex = -1;
        c.GotoNext(
            x => x.MatchNewobj<LightningOrb>(),
            x => x.MatchStloc(out bouncingLightningOrbIndex));

        c.GotoNext(
            MoveType.After,
            x => x.MatchLdfld<LightningOrb>(nameof(LightningOrb.damageType)),
            x => x.MatchStfld(out _));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, bouncingLightningOrbIndex);
        EmitCopyCall(c);
    }

    private static void GenericDamageOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyCall(c);
    }

    private static void DamageOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);
        var damageInfoIndex = GotoDamageInfo(c);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyCall(c);
    }

    private static void ChainGunOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);
        var damageInfoIndex = GotoDamageInfo(c);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyCall(c);

        var chainGunOrbIndex = -1;
        c.GotoNext(
            x => x.MatchNewobj<ChainGunOrb>(),
            x => x.MatchStloc(out chainGunOrbIndex));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, chainGunOrbIndex);

        EmitCopyCall(c);
    }
    #endregion

    #region Projectiles
    private static void ProjectileProximityBeamControllerUpdateServerIL(ILContext il)
    {
        var c = new ILCursor(il);

        var lightningOrbIndex = -1;
        c.GotoNext(
            x => x.MatchNewobj<LightningOrb>(),
            x => x.MatchStloc(out lightningOrbIndex));

        c.GotoNext(
            MoveType.After,
            x => x.MatchLdfld<ProjectileDamage>(nameof(ProjectileDamage.damageType)),
            x => x.MatchStfld(out _));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, lightningOrbIndex);
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileOverlapAttackResetOverlapAttackIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileOverlapAttack>(OpCodes.Ldfld, nameof(ProjectileOverlapAttack.attack));
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileOverlapAttackStartIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<OverlapAttack>(),
            x => x.MatchStfld(out _));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileOverlapAttack>(OpCodes.Ldfld, nameof(ProjectileOverlapAttack.attack));
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileIntervalOverlapAttackFixedUpdateIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<OverlapAttack>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentInversedCall(c);
    }

    private static void ProjectileExplosionDetonateServerIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<BlastAttack>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentInversedCall(c);
    }

    private static void ProjectileDotZoneResetOverlapIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<OverlapAttack>(),
            x => x.MatchStfld(out _));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileDotZone>(OpCodes.Ldfld, nameof(ProjectileDotZone.attack));
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileSingleTargetImpactOnProjectileImpactIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        c.GotoNext(
            x => x.MatchLdloc(damageInfoIndex),
            x => x.MatchLdarg(0));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);

        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileGrantOnKillOnDestroyOnDestroyIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyFromComponentInversedCall(c);
    }

    private static void DeathProjectileFixedUpdateIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyFromComponentInversedCall(c);
    }
    #endregion

    #region DotController
    private static void DotControllerAddPendingDamageEntryIL(ILContext il)
    {
        var c = new ILCursor(il);

        var pendingDamageIndex = -1;
        c.GotoNext(
            x => x.MatchLdloc(out pendingDamageIndex),
            x => x.MatchLdarg(3),
            x => x.MatchStfld(out _));

        EmitGetTempHolder(c);
        c.Emit(OpCodes.Ldloc, pendingDamageIndex);
        EmitAssignHolderCall(c);
    }

    private static void DotControllerEvaluateDotStacksForTypeIL(ILContext il)
    {
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
    #endregion

    #region BlastAttack
    private static void BlastAttackDamageInfoRead(BlastAttackDamageInfoReadOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader)
    {
        orig(ref self, networkReader);

        var holder = ModdedDamageTypeHolder.ReadFromNetworkReader(networkReader);
        if (holder != null)
        {
            TempHolder = holder;
        }
    }

    private static void BlastAttackDamageInfoWrite(BlastAttackDamageInfoWriteOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter)
    {
        orig(ref self, networkWriter);

        var holder = TempHolder;
        if (holder == null)
        {
            networkWriter.Write((byte)0);
            return;
        }

        holder.WriteToNetworkWriter(networkWriter);
    }

    private static void BlastAttackPerformDamageServerIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        EmitGetTempHolder(c);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);
        EmitAssignHolderCall(c);
    }

    private static void BlastAttackHandleHitsIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(x => x.MatchCall<NetworkServer>("get_active"));

        c.Emit(OpCodes.Ldarg_0);
        EmitSetTempHolder(c);
    }
    #endregion

    #region OverlapAttack
    private static void OverlapAttackMessageDeserialize(On.RoR2.OverlapAttack.OverlapAttackMessage.orig_Deserialize orig, MessageBase self, NetworkReader reader)
    {
        orig(self, reader);

        var holder = ModdedDamageTypeHolder.ReadFromNetworkReader(reader);
        if (holder != null)
        {
            TempHolder = holder;
        }
    }

    private static void OverlapAttackMessageSerialize(On.RoR2.OverlapAttack.OverlapAttackMessage.orig_Serialize orig, MessageBase self, NetworkWriter writer)
    {
        orig(self, writer);


        var holder = TempHolder;
        if (holder == null)
        {
            writer.Write((byte)0);
            return;
        }

        holder.WriteToNetworkWriter(writer);
    }

    private static void OverlapAttackPerformDamageIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageInfoIndex = GotoDamageInfo(c);

        EmitGetTempHolder(c);
        c.Emit(OpCodes.Ldloc, damageInfoIndex);
        EmitAssignHolderCall(c);
    }

    private static void OverlapAttackProcessHitsIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(x => x.MatchCall<NetworkServer>("get_active"));

        c.Emit(OpCodes.Ldarg_0);
        EmitSetTempHolder(c);
    }
    #endregion

    #region GlobalEventManager
    private static void GlobalEventManagerOnHitAllIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.Behemoth)));

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<BlastAttack>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_1);

        EmitCopyInversedCall(c);
    }
    #endregion

    #region HealthComponent
    private static void DamageDealtMessageDeserialize(On.RoR2.DamageDealtMessage.orig_Deserialize orig, DamageDealtMessage self, NetworkReader reader)
    {
        orig(self, reader);

        var holder = ModdedDamageTypeHolder.ReadFromNetworkReader(reader);
        if (holder != null)
        {
            damageTypeHolders.Add(self, holder);
        }
    }

    private static void DamageDealtMessageSerialize(On.RoR2.DamageDealtMessage.orig_Serialize orig, DamageDealtMessage self, NetworkWriter writer)
    {
        orig(self, writer);

        if (!damageTypeHolders.TryGetValue(self, out var holder))
        {
            writer.Write((byte)0);
            return;
        }

        holder.WriteToNetworkWriter(writer);
    }

    private static void HealthComponentSendDamageDealtIL(ILContext il)
    {
        var c = new ILCursor(il);

        var damageDealtMessageIndex = -1;
        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageDealtMessage>(),
            x => x.MatchStloc(out damageDealtMessageIndex));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit<DamageReport>(OpCodes.Ldfld, nameof(DamageReport.damageInfo));
        c.Emit(OpCodes.Ldloc, damageDealtMessageIndex);
        EmitCopyCall(c);
    }
    #endregion

    #region DelayBlast
    private static void DelayBlastDetonateIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<BlastAttack>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentInversedCall(c);
    }
    #endregion

    #region ContactDamage
    private static void ContactDamageFireOverlapsIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ContactDamage>(nameof(ContactDamage.damageType)));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ContactDamage>(OpCodes.Ldfld, nameof(ContactDamage.overlapAttack));
        EmitCopyFromComponentCall(c);
    }
    #endregion

    #region Helpers
    private static void EmitGetTempHolder(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetGetMethod(true));

    private static void EmitSetTempHolder(ILCursor c)
    {
        c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(MakeCopyOfModdedDamageTypeFromObject)));
        c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetSetMethod(true));
    }

    private static int GotoDamageInfo(ILCursor c)
    {
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
    private static void CopyModdedDamageType(object from, object to)
    {
        if (from == null || to == null)
        {
            return;
        }
        if (damageTypeHolders.TryGetValue(from, out var holder))
        {
            holder.CopyToInternal(to);
        }
    }

    private static void EmitCopyFromComponentCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(CopyModdedDamageTypeFromComponent)));
    private static void EmitCopyFromComponentInversedCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(CopyModdedDamageTypeFromComponentInversed)));

    private static void CopyModdedDamageTypeFromComponentInversed(object to, Component from) => CopyModdedDamageTypeFromComponent(from, to);
    private static void CopyModdedDamageTypeFromComponent(Component from, object to)
    {
        if (!from || to == null)
        {
            return;
        }

        var holder = from.GetComponent<ModdedDamageTypeHolderComponent>();
        if (!holder)
        {
            return;
        }

        holder.CopyToInternal(to);
    }

    private static void EmitAssignHolderCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(AssignHolderToObject)));
    private static void AssignHolderToObject(ModdedDamageTypeHolder holder, object obj)
    {
        if (holder == null || obj == null)
        {
            return;
        }
        holder.CopyToInternal(obj);
    }

    private static ModdedDamageTypeHolder MakeCopyOfModdedDamageTypeFromObject(object from)
    {
        if (from == null)
        {
            return null;
        }

        if (damageTypeHolders.TryGetValue(from, out var holder))
        {
            return holder.MakeCopy();
        }

        return null;
    }
    #endregion
    #endregion

    #region Public
    /// <summary>
    /// Reserve ModdedDamageType to use it with
    /// <see cref="DamageAPI.AddModdedDamageType(DamageInfo, ModdedDamageType)"/>,
    /// <see cref="DamageAPI.RemoveModdedDamageType(DamageInfo, ModdedDamageType)"/> and
    /// <see cref="DamageAPI.HasModdedDamageType(DamageInfo, ModdedDamageType)"/>
    /// </summary>
    /// <returns></returns>
    public static ModdedDamageType ReserveDamageType()
    {
        DamageAPI.SetHooks();
        if (ModdedDamageTypeCount >= CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection)
        {
            //I doubt this is ever gonna happen, but just in case.
            throw new IndexOutOfRangeException($"Reached the limit of {CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection} ModdedDamageTypes. Please contact R2API developers to increase the limit");
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

    /// <summary>
    /// Adding ModdedDamageType to DotController.DotStack instance. You can add more than one damage type to one DotController.DotStack
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(dotStack, moddedDamageType);

    private static void AddModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        if (!damageTypeHolders.TryGetValue(obj, out var holder))
        {
            damageTypeHolders.Add(obj, holder = new ModdedDamageTypeHolder());
        }

        holder.Add(moddedDamageType);
    }
    #endregion

    #region RemoveModdedDamageType
    /// <summary>
    /// Removing ModdedDamageType from DamageInfo instance.
    /// </summary>
    /// <param name="damageInfo"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(damageInfo, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from BulletAttack instance.
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(bulletAttack, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from DamageOrb instance.
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(damageOrb, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from GenericDamageOrb instance.
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(genericDamageOrb, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from LightningOrb instance.
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(lightningOrb, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from BlastAttack instance.
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(blastAttack, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from OverlapAttack instance.
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(overlapAttack, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from DotController.DotStack instance.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(dotStack, moddedDamageType);

    private static bool RemoveModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        if (!damageTypeHolders.TryGetValue(obj, out var holder))
        {
            return false;
        }

        return holder.Remove(moddedDamageType);
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

    /// <summary>
    /// Checks if DotController.DotStack instance has ModdedDamageType assigned. One DotController.DotStack can have more than one damage type.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(dotStack, moddedDamageType);

    private static bool HasModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        if (!damageTypeHolders.TryGetValue(obj, out var holder))
        {
            return false;
        }

        return holder.Has(moddedDamageType);
    }
    #endregion

    #region GetModdedDamageTypeHolder
    /// <summary>
    /// Returns ModdedDamageTypeHolder for DamageInfo instance.
    /// </summary>
    /// <param name="damageInfo"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this DamageInfo damageInfo) => GetModdedDamageTypeHolderInternal(damageInfo);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for BulletAttack instance.
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this BulletAttack bulletAttack) => GetModdedDamageTypeHolderInternal(bulletAttack);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for DamageOrb instance.
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this DamageOrb damageOrb) => GetModdedDamageTypeHolderInternal(damageOrb);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for GenericDamageOrb instance.
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this GenericDamageOrb genericDamageOrb) => GetModdedDamageTypeHolderInternal(genericDamageOrb);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for LightningOrb instance.
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this LightningOrb lightningOrb) => GetModdedDamageTypeHolderInternal(lightningOrb);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for BlastAttack instance.
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this BlastAttack blastAttack) => GetModdedDamageTypeHolderInternal(blastAttack);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for OverlapAttack instance.
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this OverlapAttack overlapAttack) => GetModdedDamageTypeHolderInternal(overlapAttack);

    /// <summary>
    /// Returns ModdedDamageTypeHolder for DotController.DotStack instance.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this DotController.DotStack dotStack) => GetModdedDamageTypeHolderInternal(dotStack);

    private static ModdedDamageTypeHolder GetModdedDamageTypeHolderInternal(object obj)
    {
        SetHooks();

        if (!damageTypeHolders.TryGetValue(obj, out var holder))
        {
            return null;
        }

        return holder;
    }
    #endregion

    #region ExtraTypes
    /// <summary>
    /// Holds flag values of ModdedDamageType.
    /// </summary>
    public class ModdedDamageTypeHolder
    {
        private byte[] values;

        public ModdedDamageTypeHolder() { SetHooks(); }

        public ModdedDamageTypeHolder(byte[] values)
        {
            SetHooks();

            this.values = values?.ToArray();
        }

        /// <summary>
        /// Enable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        public void Add(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Add(ref values, (int)moddedDamageType);

        /// <summary>
        /// Disable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Remove(ref values, (int)moddedDamageType);

        /// <summary>
        /// Checks if ModdedDamageType is enabled
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Has(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Has(values, (int)moddedDamageType);

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
        /// Copies enabled ModdedDamageTypes to the BlastAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="blastAttack"></param>
        public void CopyTo(BlastAttack blastAttack) => CopyToInternal(blastAttack);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the OverlapAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="overlapAttack"></param>
        public void CopyTo(OverlapAttack overlapAttack) => CopyToInternal(overlapAttack);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the DotController.DotStack instance (completely replacing already set values)
        /// </summary>
        /// <param name="dotStack"></param>
        public void CopyTo(DotController.DotStack dotStack) => CopyToInternal(dotStack);

        internal void CopyToInternal(object obj)
        {
            damageTypeHolders.Remove(obj);
            damageTypeHolders.Add(obj, MakeCopy());
        }
        #endregion

        /// <summary>
        /// Makes a copy of this instance
        /// </summary>
        /// <returns></returns>
        public ModdedDamageTypeHolder MakeCopy()
        {
            DamageAPI.SetHooks();
            var holder = new ModdedDamageTypeHolder
            {
                values = values?.ToArray()
            };
            return holder;
        }

        /// <summary>
        /// Reads compressed value from the NerworkReader. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ModdedDamageTypeHolder ReadFromNetworkReader(NetworkReader reader)
        {
            DamageAPI.SetHooks();
            var values = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader);
            if (values == null)
            {
                return null;
            }

            return new ModdedDamageTypeHolder
            {
                values = values
            };
        }

        /// <summary>
        /// Writes compressed value to the NerworkWriter. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
        /// </summary>
        /// <param name="writer"></param>
        public void WriteToNetworkWriter(NetworkWriter writer) => CompressedFlagArrayUtilities.WriteToNetworkWriter(values, writer);
    }

    /// <summary>
    /// Holds flag values of ModdedDamageType. The main usage is for projectiles. Add this component to your prefab.
    /// </summary>
    public sealed class ModdedDamageTypeHolderComponent : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        //I can't just use ModdedDamageTypeHolder instead of byte[] because Unity can't serialize classes that come from assemblies
        //that are not present at startup (basically every mod) even if they use [Serializable] attribute.
        //Though Unity can serialize class if it inherit from MonoBehaviour or ScriptableObject.
        private byte[] values;

        /// <summary>
        /// Enable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        public void Add(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Add(ref values, (int)moddedDamageType);

        /// <summary>
        /// Disable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Remove(ref values, (int)moddedDamageType);

        /// <summary>
        /// Checks if ModdedDamageType is enabled
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Has(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Has(values, (int)moddedDamageType);

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
        /// Copies enabled ModdedDamageTypes to the BlastAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="blastAttack"></param>
        public void CopyTo(BlastAttack blastAttack) => CopyToInternal(blastAttack);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the OverlapAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="overlapAttack"></param>
        public void CopyTo(OverlapAttack overlapAttack) => CopyToInternal(overlapAttack);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the DotController.DotStack instance (completely replacing already set values)
        /// </summary>
        /// <param name="dotStack"></param>
        public void CopyTo(DotController.DotStack dotStack) => CopyToInternal(dotStack);

        internal void CopyToInternal(object obj)
        {
            damageTypeHolders.Remove(obj);
            damageTypeHolders.Add(obj, new ModdedDamageTypeHolder(values));
        }
        #endregion

        /// <summary>
        /// Create ModdedDamageTypeHolder using values of this instance
        /// </summary>
        /// <returns></returns>
        public ModdedDamageTypeHolder MakeHolder()
        {
            DamageAPI.SetHooks();
            return new ModdedDamageTypeHolder(values);
        }
    }
    #endregion
    #endregion

    #region Private
    private delegate void BlastAttackDamageInfoWriteDelegate(BlastAttackDamageInfoWriteOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter);
    private delegate void BlastAttackDamageInfoWriteOrig(ref BlastAttack.BlastAttackDamageInfo self, NetworkWriter networkWriter);

    private delegate void BlastAttackDamageInfoReadDelegate(BlastAttackDamageInfoReadOrig orig, ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader);
    private delegate void BlastAttackDamageInfoReadOrig(ref BlastAttack.BlastAttackDamageInfo self, NetworkReader networkReader);
    #endregion
}
