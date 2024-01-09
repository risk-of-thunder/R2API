using System;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// API for handling DamageTypes added by mods
/// </summary>

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class DamageAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".damagetype";
    public const string PluginName = R2API.PluginName + ".DamageType";

    public enum ModdedDamageType { };

    private static readonly FixedConditionalWeakTable<object, ModdedDamageTypeHolder> damageTypeHolders = new();

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

    private static DamageType signalDamageType = ((DamageType)0x80000000u);

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

        On.RoR2.CrocoDamageTypeController.GetDamageType += CrocoDamageTypeControllerGetDamageType;
        On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManagerInitializeProjectile;

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

        On.RoR2.CrocoDamageTypeController.GetDamageType -= CrocoDamageTypeControllerGetDamageType;
        On.RoR2.Projectile.ProjectileManager.InitializeProjectile -= ProjectileManagerInitializeProjectile;

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
    private static void BulletAttackDefaultHitCallbackIL(ILContext il) => GotoAndEmitCopyCallForNewDamageInfo(new ILCursor(il));
    #endregion

    #region Orbs
    private static void LightningOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);

        GotoAndEmitCopyCallForNewDamageInfo(c);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<LightningOrb>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyCall(c);
    }

    private static void GenericDamageOrbOnArrivalIL(ILContext il) => GotoAndEmitCopyCallForNewDamageInfo(new ILCursor(il));

    private static void DamageOrbOnArrivalIL(ILContext il) => GotoAndEmitCopyCallForNewDamageInfo(new ILCursor(il));

    private static void ChainGunOrbOnArrivalIL(ILContext il)
    {
        var c = new ILCursor(il);

        GotoAndEmitCopyCallForNewDamageInfo(c);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<ChainGunOrb>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyCall(c);
    }
    #endregion

    #region Projectiles
    private static void ProjectileProximityBeamControllerUpdateServerIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<LightningOrb>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileOverlapAttackResetOverlapAttackIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileOverlapAttack>(OpCodes.Ldfld, nameof(ProjectileOverlapAttack.attack));
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileOverlapAttackStartIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchStfld<ProjectileOverlapAttack>(nameof(ProjectileOverlapAttack.attack)));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileOverlapAttack>(OpCodes.Ldfld, nameof(ProjectileOverlapAttack.attack));
        c.Emit(OpCodes.Ldarg_0);
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
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileExplosionDetonateServerIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<BlastAttack>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileDotZoneResetOverlapIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            x => x.MatchStfld<ProjectileDotZone>(nameof(ProjectileDotZone.attack)));

        c.Emit(OpCodes.Ldarg_0);
        c.Emit<ProjectileDotZone>(OpCodes.Ldfld, nameof(ProjectileDotZone.attack));
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentCall(c);
    }

    private static void ProjectileManagerInitializeProjectile(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig,ProjectileController projectileController,FireProjectileInfo fireProjectileInfo)
    {
        orig(projectileController,fireProjectileInfo);
        var damageComponent = projectileController.GetComponent<ProjectileDamage>();
        if(!damageComponent || (damageComponent.damageType & signalDamageType) == 0){
          return;
        }
        var targetHolder = projectileController.GetComponent<ModdedDamageTypeHolderComponent>();
        var crocDamageType = projectileController.owner.GetComponent<CrocoDamageTypeController>();
        ModdedDamageTypeHolder fromHolder;
        if(damageTypeHolders.TryGetValue(crocDamageType,out fromHolder)){
          if(targetHolder){
            targetHolder.Add(fromHolder);
          }
          else
          {
            projectileController.gameObject.AddComponent<ModdedDamageTypeHolderComponent>().Add(fromHolder);
          }
          damageComponent.damageType &= ~signalDamageType;
        }
    }

    private static void ProjectileSingleTargetImpactOnProjectileImpactIL(ILContext il) => GotoAndEmitCopyComponentCallForNewDamageInfo(new ILCursor(il));

    private static void ProjectileGrantOnKillOnDestroyOnDestroyIL(ILContext il) => GotoAndEmitCopyComponentCallForNewDamageInfo(new ILCursor(il));

    private static void DeathProjectileFixedUpdateIL(ILContext il) => GotoAndEmitCopyComponentCallForNewDamageInfo(new ILCursor(il));
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

        c.Emit(OpCodes.Ldloc, pendingDamageIndex);
        EmitGetTempHolder(c);
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

        GotoAndEmitCopyCallForNewDamageInfo(new ILCursor(il));
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

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        EmitGetTempHolder(c);
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

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        EmitGetTempHolder(c);
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

        EmitCopyCall(c);
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

        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageDealtMessage>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit<DamageReport>(OpCodes.Ldfld, nameof(DamageReport.damageInfo));
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
        EmitCopyFromComponentCall(c);
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
        c.Emit<ContactDamage>(OpCodes.Ldfld, nameof(ContactDamage.overlapAttack));
        c.Emit(OpCodes.Ldarg_0);
        EmitCopyFromComponentCall(c);
    }
    #endregion

    #region Croco
    private static DamageType CrocoDamageTypeControllerGetDamageType(On.RoR2.CrocoDamageTypeController.orig_GetDamageType orig,CrocoDamageTypeController self){
       var returnValue = orig(self);
       if(damageTypeHolders.TryGetValue(self,out _)){
           returnValue |= signalDamageType;
       }
       return returnValue;
    }

    #endregion

    #region Helpers
    private static void EmitGetTempHolder(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetGetMethod(true));

    private static void EmitSetTempHolder(ILCursor c)
    {
        c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(MakeCopyOfModdedDamageTypeFromObject)));
        c.Emit(OpCodes.Call, typeof(DamageAPI).GetPropertyCached(nameof(TempHolder)).GetSetMethod(true));
    }

    private static void GotoAndEmitCopyCallForNewDamageInfo(ILCursor c)
    {
        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyCall(c);
    }

    private static void GotoAndEmitCopyComponentCallForNewDamageInfo(ILCursor c)
    {
        c.GotoNext(
            MoveType.After,
            x => x.MatchNewobj<DamageInfo>());

        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Ldarg_0);

        EmitCopyFromComponentCall(c);
    }

    private static void EmitCopyCall(ILCursor c) => c.Emit(OpCodes.Call, typeof(DamageAPI).GetMethodCached(nameof(CopyModdedDamageType)));

    private static void CopyModdedDamageType(object to, object from)
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

    private static void CopyModdedDamageTypeFromComponent(object to, Component from)
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
    private static void AssignHolderToObject(object obj, ModdedDamageTypeHolder holder)
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

    /// <summary>
    /// Adding ModdedDamageType to CrocoDamageTypeController instance. You can add more than one damage type to one CrocoDamageTypeController
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(croco, moddedDamageType);

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

    /// <summary>
    /// Removing ModdedDamageType from CrocoDamageTypeController instance.
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(croco, moddedDamageType);

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
    public static bool HasModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(damageInfo, moddedDamageType, ref damageInfo.damageType,damageInfo.attacker);

    /// <summary>
    /// Checks if BulletAttack instance has ModdedDamageType assigned. One BulletAttack can have more than one damage type.
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(bulletAttack, moddedDamageType, ref bulletAttack.damageType,bulletAttack.owner);

    /// <summary>
    /// Checks if DamageOrb instance has ModdedDamageType assigned. One DamageOrb can have more than one damage type.
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(damageOrb, moddedDamageType, ref damageOrb.orbDamageType,damageOrb.attacker);

    /// <summary>
    /// Checks if GenericDamageOrb instance has ModdedDamageType assigned. One GenericDamageOrb can have more than one damage type.
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(genericDamageOrb, moddedDamageType, ref genericDamageOrb.damageType,genericDamageOrb.attacker);

    /// <summary>
    /// Checks if LightningOrb instance has ModdedDamageType assigned. One LightningOrb can have more than one damage type.
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(lightningOrb, moddedDamageType, ref lightningOrb.damageType,lightningOrb.attacker);

    /// <summary>
    /// Checks if BlastAttack instance has ModdedDamageType assigned. One BlastAttack can have more than one damage type.
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(blastAttack, moddedDamageType, ref blastAttack.damageType,blastAttack.attacker);

    /// <summary>
    /// Checks if OverlapAttack instance has ModdedDamageType assigned. One OverlapAttack can have more than one damage type.
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(overlapAttack, moddedDamageType, ref overlapAttack.damageType,overlapAttack.attacker);

    /// <summary>
    /// Checks if DotController.DotStack instance has ModdedDamageType assigned. One DotController.DotStack can have more than one damage type.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(dotStack, moddedDamageType, ref dotStack.damageType,dotStack.attackerObject);

    /// <summary>
    /// Checks if CrocoDamageTypeController instance has ModdedDamageType assigned. One CrocoDamageTypeController can have more than one damage type.
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(croco, moddedDamageType);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool HasModdedDamageTypeInternal(object obj,ModdedDamageType moddedDamageType,GameObject owner = null)
    {
        DamageType dummy = default(DamageType);
        return HasModdedDamageTypeInternal(obj,moddedDamageType,ref dummy);
    }
    private static bool HasModdedDamageTypeInternal(object obj, ModdedDamageType moddedDamageType,ref DamageType vanillaDamageType,GameObject owner = null)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        bool signal = (vanillaDamageType & signalDamageType) != 0;
        if (!damageTypeHolders.TryGetValue(obj, out var holder) && (!signal || !owner))
        {
            return false;
        }
        if(signal && owner)
        {
            var crocoComp = owner.GetComponent<CrocoDamageTypeController>();
            ModdedDamageTypeHolder crocHolder;
            if(crocoComp && damageTypeHolders.TryGetValue(crocoComp,out crocHolder))
            {
                damageTypeHolders.GetOrCreateValue(obj).Add(crocHolder);
                vanillaDamageType &= ~signalDamageType;
            }
        }

        return (holder != null) ? holder.Has(moddedDamageType) : false;
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

    /// <summary>
    /// Returns ModdedDamageTypeHolder for CrocoDamageTypeController instance.
    /// </summary>
    /// <param name="croco"></param>
    /// <returns></returns>
    public static ModdedDamageTypeHolder GetModdedDamageTypeHolder(this CrocoDamageTypeController croco) => GetModdedDamageTypeHolderInternal(croco);

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
        internal byte[] values = Array.Empty<byte>();

        public ModdedDamageTypeHolder() { SetHooks(); }

        public ModdedDamageTypeHolder(byte[] values)
        {
            SetHooks();

            if (values.Length > 0)
            {
                this.values = values.ToArray();
            }
        }

        /// <summary>
        /// Enable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        public void Add(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Add(ref values, (int)moddedDamageType);

        /// <summary>
        /// Enable ModdedDamageTypes from argument for this instance
        /// </summary>
        /// <param name="moddedDamageTypeHolder"></param>
        public void Add(ModdedDamageTypeHolder moddedDamageTypeHolder) => CompressedFlagArrayUtilities.Add(ref values, moddedDamageTypeHolder.values);

        /// <summary>
        /// Disable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Remove(ref values, (int)moddedDamageType);

        /// <summary>
        /// Disable ModdedDamageTypes from argument for this instance
        /// </summary>
        /// <param name="moddedDamageTypeHolder"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageTypeHolder moddedDamageTypeHolder) => CompressedFlagArrayUtilities.Remove(ref values, moddedDamageTypeHolder.values);

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

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the CrocoDamageTypeController instance (completely replacing already set values)
        /// </summary>
        /// <param name="croco"></param>
        public void CopyTo(CrocoDamageTypeController croco) => CopyToInternal(croco);

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
                values = values.Length == 0 ? values : values.ToArray()
            };
            return holder;
        }

        /// <summary>
        /// Reads compressed value from the NerworkReader. More info about that can be found in the PRs:
        /// https://github.com/risk-of-thunder/R2API/pull/284
        /// https://github.com/risk-of-thunder/R2API/pull/464
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ModdedDamageTypeHolder ReadFromNetworkReader(NetworkReader reader)
        {
            DamageAPI.SetHooks();
            var values = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, ModdedDamageTypeCount);
            if (values.Length == 0)
            {
                return null;
            }

            return new ModdedDamageTypeHolder
            {
                values = values
            };
        }

        /// <summary>
        /// Writes compressed value to the NerworkWriter. More info about that can be found in the PRs:
        /// https://github.com/risk-of-thunder/R2API/pull/284
        /// https://github.com/risk-of-thunder/R2API/pull/464
        /// </summary>
        /// <param name="writer"></param>
        public void WriteToNetworkWriter(NetworkWriter writer) => CompressedFlagArrayUtilities.WriteToNetworkWriter(values, writer, ModdedDamageTypeCount);
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
        internal byte[] values = Array.Empty<byte>();

        /// <summary>
        /// Enable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        public void Add(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Add(ref values, (int)moddedDamageType);

        /// <summary>
        /// Enable ModdedDamageTypes from argument for this instance
        /// </summary>
        /// <param name="moddedDamageTypeHolder"></param>
        public void Add(ModdedDamageTypeHolder moddedDamageTypeHolder) => CompressedFlagArrayUtilities.Add(ref values, moddedDamageTypeHolder.values);

        /// <summary>
        /// Disable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageType moddedDamageType) => CompressedFlagArrayUtilities.Remove(ref values, (int)moddedDamageType);

        /// <summary>
        /// Disable ModdedDamageTypes from argument for this instance
        /// </summary>
        /// <param name="moddedDamageTypeHolder"></param>
        /// <returns></returns>
        public bool Remove(ModdedDamageTypeHolder moddedDamageTypeHolder) => CompressedFlagArrayUtilities.Remove(ref values, moddedDamageTypeHolder.values);

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

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the CrocoDamageTypeController instance (completely replacing already set values)
        /// </summary>
        /// <param name="croco"></param>
        public void CopyTo(CrocoDamageTypeController croco) => CopyToInternal(croco);

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
