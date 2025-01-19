using System;
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

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class DamageAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".damagetype";
    public const string PluginName = R2API.PluginName + ".DamageType";

    public enum ModdedDamageType { };

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

        On.RoR2.NetworkExtensions.WriteDamageType += WriteDamageType;
        On.RoR2.NetworkExtensions.ReadDamageType += ReadDamageType;

        On.Unity.GeneratedNetworkCode._ReadDamageTypeCombo_None += ReadDamageTypeCombo;
        On.Unity.GeneratedNetworkCode._WriteDamageTypeCombo_None += WriteDamageTypeCombo;

        On.RoR2.CrocoDamageTypeController.GetDamageType += CrocoDamageTypeControllerGetDamageType;

        IL.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManagerInitializeProjectile;

        //There are also 2 more operations (^ and ~) but they are never used in vanilla
        //and are not really applicable to modded damage types, so we skip them.
        //For & doing the same thing as |, since we don't hook ~ which leads to
        //missing modded damage types when mods remove some vanilla damage types.
        HookEndpointManager.Add(
            typeof(DamageTypeCombo).GetMethodCached("op_BitwiseAnd"),
            DamageTypeComboOpBitwiseOr);
        HookEndpointManager.Add(
            typeof(DamageTypeCombo).GetMethodCached("op_BitwiseOr"),
            DamageTypeComboOpBitwiseOr);

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo -= WriteDamageInfo;
        On.RoR2.NetworkExtensions.ReadDamageInfo -= ReadDamageInfo;

        On.RoR2.NetworkExtensions.WriteDamageType -= WriteDamageType;
        On.RoR2.NetworkExtensions.ReadDamageType -= ReadDamageType;

        On.Unity.GeneratedNetworkCode._ReadDamageTypeCombo_None -= ReadDamageTypeCombo;
        On.Unity.GeneratedNetworkCode._WriteDamageTypeCombo_None -= WriteDamageTypeCombo;

        On.RoR2.CrocoDamageTypeController.GetDamageType -= CrocoDamageTypeControllerGetDamageType;

        IL.RoR2.Projectile.ProjectileManager.InitializeProjectile -= ProjectileManagerInitializeProjectile;

        HookEndpointManager.Remove(
            typeof(DamageTypeCombo).GetMethodCached("op_BitwiseAnd"),
            DamageTypeComboOpBitwiseOr);
        HookEndpointManager.Remove(
            typeof(DamageTypeCombo).GetMethodCached("op_BitwiseOr"),
            DamageTypeComboOpBitwiseOr);

        _hooksEnabled = false;
    }

    #region Network
    private static DamageInfo ReadDamageInfo(On.RoR2.NetworkExtensions.orig_ReadDamageInfo orig, NetworkReader reader)
    {
        var damageInfo = orig(reader);

        var values = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, ModdedDamageTypeCount);
        DamageTypeComboInterop.SetModdedDamageTypes(ref damageInfo.damageType, values);

        return damageInfo;
    }

    private static void WriteDamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, NetworkWriter writer, DamageInfo damageInfo)
    {
        orig(writer, damageInfo);

        var values = DamageTypeComboInterop.GetModdedDamageTypes(damageInfo.damageType);
        CompressedFlagArrayUtilities.WriteToNetworkWriter(values ?? [], writer, ModdedDamageTypeCount);
    }

    private static DamageTypeCombo ReadDamageTypeCombo(On.Unity.GeneratedNetworkCode.orig__ReadDamageTypeCombo_None orig, NetworkReader reader)
    {
        var damageTypeCombo = orig(reader);

        var values = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, ModdedDamageTypeCount);
        DamageTypeComboInterop.SetModdedDamageTypes(ref damageTypeCombo, values);

        return damageTypeCombo;
    }

    private static void WriteDamageTypeCombo(On.Unity.GeneratedNetworkCode.orig__WriteDamageTypeCombo_None orig, NetworkWriter writer, DamageTypeCombo value)
    {
        orig(writer, value);

        var values = DamageTypeComboInterop.GetModdedDamageTypes(value);
        CompressedFlagArrayUtilities.WriteToNetworkWriter(values ?? [], writer, ModdedDamageTypeCount);
    }

    private static DamageTypeCombo ReadDamageType(On.RoR2.NetworkExtensions.orig_ReadDamageType orig, NetworkReader reader)
    {
        var damageTypeCombo = orig(reader);

        var values = CompressedFlagArrayUtilities.ReadFromNetworkReader(reader, ModdedDamageTypeCount);
        DamageTypeComboInterop.SetModdedDamageTypes(ref damageTypeCombo, values);

        return damageTypeCombo;
    }

    private static void WriteDamageType(On.RoR2.NetworkExtensions.orig_WriteDamageType orig, NetworkWriter writer, DamageTypeCombo damageType)
    {
        orig(writer, damageType);

        var values = DamageTypeComboInterop.GetModdedDamageTypes(damageType);
        CompressedFlagArrayUtilities.WriteToNetworkWriter(values ?? [], writer, ModdedDamageTypeCount);
    }
    #endregion

    #region Croco
    private static DamageTypeCombo CrocoDamageTypeControllerGetDamageType(On.RoR2.CrocoDamageTypeController.orig_GetDamageType orig, CrocoDamageTypeController self)
    {
        var returnValue = orig(self);

        var damageTypes = CrocoDamageTypeControllerInterop.GetModdedDamageTypes(self);
        DamageTypeComboInterop.SetModdedDamageTypes(ref returnValue, damageTypes);

        return returnValue;
    }
    #endregion

    #region DamageTypeCombo
    private static DamageTypeCombo DamageTypeComboOpBitwiseOr(Func<DamageTypeCombo, DamageTypeCombo, DamageTypeCombo> orig, DamageTypeCombo operand1, DamageTypeCombo operand2)
    {
        var newDamageType = orig(operand1, operand2);

        var moddedDamageTypes1 = DamageTypeComboInterop.GetModdedDamageTypes(operand1);
        var moddedDamageTypes2 = DamageTypeComboInterop.GetModdedDamageTypes(operand2);

        if (moddedDamageTypes1 is null && moddedDamageTypes2 is null)
        {
            return newDamageType;
        }

        if (moddedDamageTypes1 is null)
        {
            DamageTypeComboInterop.SetModdedDamageTypes(ref newDamageType, moddedDamageTypes2);
            return newDamageType;
        }

        if (moddedDamageTypes2 is null)
        {
            DamageTypeComboInterop.SetModdedDamageTypes(ref newDamageType, moddedDamageTypes1);
            return newDamageType;
        }

        byte[] minLengthArray;
        byte[] maxLengthArray;
        if (moddedDamageTypes1.Length < moddedDamageTypes2.Length)
        {
            minLengthArray = moddedDamageTypes1;
            maxLengthArray = moddedDamageTypes2;
        }
        else
        {
            minLengthArray = moddedDamageTypes2;
            maxLengthArray = moddedDamageTypes1;
        }

        var newModdedDamageTypes = new byte[maxLengthArray.Length];
        for (var i = 0; i < minLengthArray.Length; i++)
        {
            newModdedDamageTypes[i] = (byte)(minLengthArray[i] | maxLengthArray[i]);
        }
        for (var i = minLengthArray.Length; i < maxLengthArray.Length; i++)
        {
            newModdedDamageTypes[i] = maxLengthArray[i];
        }

        DamageTypeComboInterop.SetModdedDamageTypes(ref newDamageType, newModdedDamageTypes);
        return newDamageType;
    }
    #endregion

    private static void ProjectileManagerInitializeProjectile(MonoMod.Cil.ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel ifBodyLabel = null;
        if (c.TryGotoNext(MoveType.After,
            x => x.MatchLdfld<DamageTypeCombo>(nameof(DamageTypeCombo.damageType)),
            x => x.MatchBrtrue(out ifBodyLabel)))
        {
            c.Emit(OpCodes.Ldarg, 1); //fireProjectileInfo
            c.EmitDelegate(FireProjectileInfoHasModdedDamageType);
            c.Emit(OpCodes.Brtrue, ifBodyLabel);
        }
        else
        {
            DamageTypePlugin.Logger.LogError($"Failed to apply {nameof(ProjectileManagerInitializeProjectile)} hook");
        }
    }

    private static bool FireProjectileInfoHasModdedDamageType(FireProjectileInfo fireProjectileInfo)
    {
        var damageType = fireProjectileInfo.damageTypeOverride.GetValueOrDefault();
        return damageType.HasAnyModdedDamageType();
    }

    #endregion

    #region Public
    /// <summary>
    /// Reserve ModdedDamageType to use it with
    /// <see cref="DamageAPI.AddModdedDamageType(ref DamageTypeCombo, ModdedDamageType)"/>,
    /// <see cref="DamageAPI.RemoveModdedDamageType(ref DamageTypeCombo, ModdedDamageType)"/> and
    /// <see cref="DamageAPI.HasModdedDamageType(ref DamageTypeCombo, ModdedDamageType)"/>
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
    /// Adding ModdedDamageType to DamageTypeCombo. You can add more than one damage type to one DamageTypeCombo
    /// </summary>
    /// <param name="damageType"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to DamageInfo instance. You can add more than one damage type to one DamageInfo
    /// </summary>
    /// <param name="damageInfo"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref damageInfo.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to BulletAttack instance. You can add more than one damage type to one BulletAttack
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref bulletAttack.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to DamageOrb instance. You can add more than one damage type to one DamageOrb
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref damageOrb.orbDamageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to GenericDamageOrb instance. You can add more than one damage type to one GenericDamageOrb
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref genericDamageOrb.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to LightningOrb instance. You can add more than one damage type to one LightningOrb
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref lightningOrb.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to BlastAttack instance. You can add more than one damage type to one BlastAttack
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref blastAttack.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to OverlapAttack instance. You can add more than one damage type to one OverlapAttack
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref overlapAttack.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to DotController.DotStack instance. You can add more than one damage type to one DotController.DotStack
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => AddModdedDamageTypeInternal(ref dotStack.damageType, moddedDamageType);

    /// <summary>
    /// Adding ModdedDamageType to CrocoDamageTypeController instance. You can add more than one damage type to one CrocoDamageTypeController
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    public static void AddModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = CrocoDamageTypeControllerInterop.GetModdedDamageTypes(croco);
        CompressedFlagArrayUtilities.AddImmutable(ref damageTypes, (int)moddedDamageType);
        CrocoDamageTypeControllerInterop.SetModdedDamageTypes(croco, damageTypes);
    }

    private static void AddModdedDamageTypeInternal(ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = DamageTypeComboInterop.GetModdedDamageTypes(damageType);
        CompressedFlagArrayUtilities.AddImmutable(ref damageTypes, (int)moddedDamageType);
        DamageTypeComboInterop.SetModdedDamageTypes(ref damageType, damageTypes);
    }
    #endregion

    #region RemoveModdedDamageType
    /// <summary>
    /// Removing ModdedDamageType from DamageTypeCombo instance.
    /// </summary>
    /// <param name="damageType"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from DamageInfo instance.
    /// </summary>
    /// <param name="damageInfo"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref damageInfo.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from BulletAttack instance.
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref bulletAttack.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from DamageOrb instance.
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref damageOrb.orbDamageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from GenericDamageOrb instance.
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref genericDamageOrb.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from LightningOrb instance.
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref lightningOrb.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from BlastAttack instance.
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref blastAttack.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from OverlapAttack instance.
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref overlapAttack.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from DotController.DotStack instance.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => RemoveModdedDamageTypeInternal(ref dotStack.damageType, moddedDamageType);

    /// <summary>
    /// Removing ModdedDamageType from CrocoDamageTypeController instance.
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    public static bool RemoveModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = CrocoDamageTypeControllerInterop.GetModdedDamageTypes(croco);
        var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref damageTypes, (int)moddedDamageType);
        CrocoDamageTypeControllerInterop.SetModdedDamageTypes(croco, damageTypes);

        return removed;
    }

    private static bool RemoveModdedDamageTypeInternal(ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = DamageTypeComboInterop.GetModdedDamageTypes(damageType);
        var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref damageTypes, (int)moddedDamageType);
        DamageTypeComboInterop.SetModdedDamageTypes(ref damageType, damageTypes);

        return removed;
    }
    #endregion

    #region HasModdedDamageType
    /// <summary>
    /// Checks if DamageTypeCombo instance has any ModdedDamageType assigned. One DamageTypeCombo can have more than one damage type.
    /// </summary>
    /// <param name="damageType"></param>
    /// <returns></returns>
    public static bool HasAnyModdedDamageType(this ref DamageTypeCombo damageType)
    {
        SetHooks();

        var damageTypes = DamageTypeComboInterop.GetModdedDamageTypes(damageType);
        return damageTypes is not null && damageTypes.Length > 0;
    }

    /// <summary>
    /// Checks if DamageTypeCombo instance has ModdedDamageType assigned. One DamageTypeCombo can have more than one damage type.
    /// </summary>
    /// <param name="damageType"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref damageType, moddedDamageType);

    /// <summary>
    /// Checks if DamageInfo instance has ModdedDamageType assigned. One DamageInfo can have more than one damage type.
    /// </summary>
    /// <param name="damageInfo"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref damageInfo.damageType, moddedDamageType);

    /// <summary>
    /// Checks if BulletAttack instance has ModdedDamageType assigned. One BulletAttack can have more than one damage type.
    /// </summary>
    /// <param name="bulletAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this BulletAttack bulletAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref bulletAttack.damageType, moddedDamageType);

    /// <summary>
    /// Checks if DamageOrb instance has ModdedDamageType assigned. One DamageOrb can have more than one damage type.
    /// </summary>
    /// <param name="damageOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DamageOrb damageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref damageOrb.orbDamageType, moddedDamageType);

    /// <summary>
    /// Checks if GenericDamageOrb instance has ModdedDamageType assigned. One GenericDamageOrb can have more than one damage type.
    /// </summary>
    /// <param name="genericDamageOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this GenericDamageOrb genericDamageOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref genericDamageOrb.damageType, moddedDamageType);

    /// <summary>
    /// Checks if LightningOrb instance has ModdedDamageType assigned. One LightningOrb can have more than one damage type.
    /// </summary>
    /// <param name="lightningOrb"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this LightningOrb lightningOrb, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref lightningOrb.damageType, moddedDamageType);

    /// <summary>
    /// Checks if BlastAttack instance has ModdedDamageType assigned. One BlastAttack can have more than one damage type.
    /// </summary>
    /// <param name="blastAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this BlastAttack blastAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref blastAttack.damageType, moddedDamageType);

    /// <summary>
    /// Checks if OverlapAttack instance has ModdedDamageType assigned. One OverlapAttack can have more than one damage type.
    /// </summary>
    /// <param name="overlapAttack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this OverlapAttack overlapAttack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref overlapAttack.damageType, moddedDamageType);

    /// <summary>
    /// Checks if DotController.DotStack instance has ModdedDamageType assigned. One DotController.DotStack can have more than one damage type.
    /// </summary>
    /// <param name="dotStack"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this DotController.DotStack dotStack, ModdedDamageType moddedDamageType) => HasModdedDamageTypeInternal(ref dotStack.damageType, moddedDamageType);

    /// <summary>
    /// Checks if CrocoDamageTypeController instance has ModdedDamageType assigned. One CrocoDamageTypeController can have more than one damage type.
    /// </summary>
    /// <param name="croco"></param>
    /// <param name="moddedDamageType"></param>
    /// <returns></returns>
    public static bool HasModdedDamageType(this CrocoDamageTypeController croco, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = CrocoDamageTypeControllerInterop.GetModdedDamageTypes(croco);
        return CompressedFlagArrayUtilities.Has(damageTypes, (int)moddedDamageType);
    }

    private static bool HasModdedDamageTypeInternal(ref DamageTypeCombo damageType, ModdedDamageType moddedDamageType)
    {
        SetHooks();

        if ((int)moddedDamageType >= ModdedDamageTypeCount || (int)moddedDamageType < 0)
        {
            throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types (0-{ModdedDamageTypeCount - 1})");
        }

        var damageTypes = DamageTypeComboInterop.GetModdedDamageTypes(damageType);
        return CompressedFlagArrayUtilities.Has(damageTypes, (int)moddedDamageType);
    }
    #endregion

    #region ExtraTypes
    /// <summary>
    /// Holds flag values of ModdedDamageType. The main usage is for projectiles. Add this component to your prefab.
    /// </summary>
    [Obsolete("Use ProjectileDamage component damageType field directly to work with ModdedDamageTypes")]
    public sealed class ModdedDamageTypeHolderComponent : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        internal byte[] values = Array.Empty<byte>();

        [SerializeField]
        private ProjectileDamage projectileDamage;

        private void Awake()
        {
           SetHooks();

            if (!projectileDamage)
            {
                projectileDamage = GetComponent<ProjectileDamage>();
                if (projectileDamage && values.Length > 0)
                {
                    DamageTypeComboInterop.SetModdedDamageTypes(ref projectileDamage.damageType, [..values]);
                }
            }
        }

        /// <summary>
        /// Enable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        [Obsolete("Add your ModdedDamageType directly to ProjectileDamage component damageType field")]
        public void Add(ModdedDamageType moddedDamageType)
        {
            SetHooks();

            if (!projectileDamage)
            {
                projectileDamage = GetComponent<ProjectileDamage>();
            }

            if (projectileDamage)
            {
                projectileDamage.damageType.AddModdedDamageType(moddedDamageType);
            }

            CompressedFlagArrayUtilities.AddImmutable(ref values, (int)moddedDamageType);
        }

        /// <summary>
        /// Disable ModdedDamageType for this instance
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        [Obsolete("Remove your ModdedDamageType directly from ProjectileDamage component damageType field")]
        public bool Remove(ModdedDamageType moddedDamageType)
        {
            SetHooks();

            if (!projectileDamage)
            {
                projectileDamage = GetComponent<ProjectileDamage>();
            }

            if (projectileDamage)
            {
                return projectileDamage.damageType.RemoveModdedDamageType(moddedDamageType);
            }

            return CompressedFlagArrayUtilities.RemoveImmutable(ref values, (int)moddedDamageType);
        }

        /// <summary>
        /// Checks if ModdedDamageType is enabled
        /// </summary>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        [Obsolete("Check your ModdedDamageType directly on ProjectileDamage component damageType field")]
        public bool Has(ModdedDamageType moddedDamageType)
        {
            SetHooks();

            if (!projectileDamage)
            {
                projectileDamage = GetComponent<ProjectileDamage>();
            }

            if (projectileDamage)
            {
                return projectileDamage.damageType.HasModdedDamageType(moddedDamageType);
            }

            return CompressedFlagArrayUtilities.Has(values, (int)moddedDamageType);
        }

        #region CopyTo
        /// <summary>
        /// Copies enabled ModdedDamageTypes to the DamageInfo instance (completely replacing already set values)
        /// </summary>
        /// <param name="damageInfo"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(DamageInfo damageInfo) => CopyToInternal(ref damageInfo.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the BulletAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="bulletAttack"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(BulletAttack bulletAttack) => CopyToInternal(ref bulletAttack.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the DamageOrb instance (completely replacing already set values)
        /// </summary>
        /// <param name="damageOrb"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(DamageOrb damageOrb) => CopyToInternal(ref damageOrb.orbDamageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the GenericDamageOrb instance (completely replacing already set values)
        /// </summary>
        /// <param name="genericDamageOrb"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(GenericDamageOrb genericDamageOrb) => CopyToInternal(ref genericDamageOrb.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the LightningOrb instance (completely replacing already set values)
        /// </summary>
        /// <param name="lightningOrb"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(LightningOrb lightningOrb) => CopyToInternal(ref lightningOrb.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the BlastAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="blastAttack"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(BlastAttack blastAttack) => CopyToInternal(ref blastAttack.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the OverlapAttack instance (completely replacing already set values)
        /// </summary>
        /// <param name="overlapAttack"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(OverlapAttack overlapAttack) => CopyToInternal(ref overlapAttack.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the DotController.DotStack instance (completely replacing already set values)
        /// </summary>
        /// <param name="dotStack"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(DotController.DotStack dotStack) => CopyToInternal(ref dotStack.damageType, values);

        /// <summary>
        /// Copies enabled ModdedDamageTypes to the CrocoDamageTypeController instance (completely replacing already set values)
        /// </summary>
        /// <param name="croco"></param>
        [Obsolete("Apply ModdedDamageType directly to ProjectileDamage component when creating a prefab then copy damageType from that component")]
        public void CopyTo(CrocoDamageTypeController croco)
        {
            SetHooks();
            CrocoDamageTypeControllerInterop.SetModdedDamageTypes(croco, values);
        }

        private void CopyToInternal(ref DamageTypeCombo damageType, byte[] values)
        {
            SetHooks();
            DamageTypeComboInterop.SetModdedDamageTypes(ref damageType, values);
        }

        #endregion
    }
    #endregion
    #endregion
}
