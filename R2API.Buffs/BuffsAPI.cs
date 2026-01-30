using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using static R2API.BuffsAPI;
using static RoR2.BuffDef;

namespace R2API;

/// <summary>
/// API for adding various stuff to Character Body such as: Modded Body Flags
/// </summary>

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type

public static partial class BuffsAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".buffs";
    public const string PluginName = R2API.PluginName + ".Buffs";
    private static bool _hooksEnabled;
    /// <summary>
    /// Ready-to-use custom StackingDisplayMethod to hide stacking buff display text while also retaining its stacking functionality
    /// </summary>
    public static StackingDisplayMethod NoneStackingDisplayMethod;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled) return;
        _hooksEnabled = true;
        VanillaStackingDisplayMethodCount = Enum.GetValues(typeof(StackingDisplayMethod)).Length;
        IL.RoR2.UI.BuffIcon.UpdateIcon += BuffIcon_UpdateIcon;
        On.RoR2.UI.BuffIcon.OnEnable += BuffIcon_OnEnable;
        On.RoR2.BuffCatalog.SetBuffDefs += BuffCatalog_SetBuffDefs;
        if (NoneStackingDisplayMethod == StackingDisplayMethod.Default) NoneStackingDisplayMethod = RegisterStackingDisplayMethod(null);
    }
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
        IL.RoR2.UI.BuffIcon.UpdateIcon -= BuffIcon_UpdateIcon;
        On.RoR2.UI.BuffIcon.OnEnable -= BuffIcon_OnEnable;
        On.RoR2.BuffCatalog.SetBuffDefs -= BuffCatalog_SetBuffDefs;
    }
    private static void BuffIcon_OnEnable(On.RoR2.UI.BuffIcon.orig_OnEnable orig, BuffIcon self)
    {
        orig(self);
        if (_buffIconSimpleSpriteAnimator.TryGetValue(self, out SimpleSpriteAnimator simpleSpriteAnimator) && simpleSpriteAnimator.enabled) simpleSpriteAnimator.Tick();
    }

    private static byte[][] _moddedBuffFlags = [];
    private static CustomSimpleSpriteAnimation[] _buffDefSimpleSpriteAnimation = [];
    private static bool _init;
    private static void BuffCatalog_SetBuffDefs(On.RoR2.BuffCatalog.orig_SetBuffDefs orig, BuffDef[] newBuffDefs)
    {
        orig(newBuffDefs);
        if (_init) return;
        _moddedBuffFlags = new byte[BuffCatalog.buffCount][];
        _buffDefSimpleSpriteAnimation = new CustomSimpleSpriteAnimation[BuffCatalog.buffCount];
        foreach (var def in _pendingModdedBuffFlags)
        {
            BuffDef buffDef = def.Key;
            if (!buffDef) continue;
            byte[] bytes = def.Value;
            if (bytes == null || bytes.Length == 0) continue;
            _moddedBuffFlags[(int)buffDef.buffIndex] = bytes;
        }
        _pendingModdedBuffFlags.Clear();
        _pendingModdedBuffFlags = null;
        foreach (var def in _pendingSimpleSpriteAnimation)
        {
            BuffDef buffDef = def.Key;
            if (!buffDef) continue;
            CustomSimpleSpriteAnimation simpleSpriteAnimation = def.Value;
            if (simpleSpriteAnimation == null) continue;
            _buffDefSimpleSpriteAnimation[(int)buffDef.buffIndex] = simpleSpriteAnimation;
        }
        _pendingSimpleSpriteAnimation.Clear();
        _pendingSimpleSpriteAnimation = null;
        _init = true;
    }

    private static void BuffIcon_UpdateIcon(MonoMod.Cil.ILContext il)
    {
        ILCursor c = new ILCursor(il);
        if (
             !c.TryGotoNext(MoveType.Before,
                 x => x.MatchLdarg(0),
                 x => x.MatchLdfld<BuffIcon>(nameof(BuffIcon.iconImage)),
                 x => x.MatchLdloc(out _),
                 x => x.MatchCallvirt(typeof(Image).GetPropertySetter(nameof(Image.sprite)))
             ))
        {
            BuffsPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
        else
        {
            Instruction instruction1 = c.Next.Next.Next.Next;
            Instruction instruction2 = c.Next;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(CheckSimpleSpriteAnimation);
            c.Emit(OpCodes.Brfalse_S, instruction2);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, HarmonyLib.AccessTools.Field(typeof(BuffIcon), nameof(BuffIcon.iconImage)));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(HandleSimpleSpriteAnimation);
            c.Emit(OpCodes.Br_S, instruction1);
        }
        ILLabel iLLabel = null;
        if (
             !c.TryGotoNext(MoveType.After,
                 x => x.MatchLdarg(0),
                 x => x.MatchLdfld<BuffIcon>(nameof(BuffIcon.buffDef)),
                 x => x.MatchLdfld<BuffDef>(nameof(BuffDef.canStack)),
                 x => x.MatchBrfalse(out iLLabel)
             ))
        {
            BuffsPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
        }
        else
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, HarmonyLib.AccessTools.Field(typeof(BuffIcon), nameof(BuffIcon.stackCount)));
            c.Emit(OpCodes.Ldfld, HarmonyLib.AccessTools.Field(typeof(BuffDef), nameof(BuffDef.stackingDisplayMethod)));
            c.Emit(OpCodes.Ldsfld, HarmonyLib.AccessTools.Field(typeof(BuffsAPI), nameof(NoneStackingDisplayMethod)));
            c.Emit(OpCodes.Beq_S, iLLabel);
        }
        if (
             !c.TryGotoNext(MoveType.Before,
                 x => x.MatchLdarg(0),
                 x => x.MatchLdfld<BuffIcon>(nameof(BuffIcon.stackCount)),
                 x => x.MatchLdcI4(1),
                 x => x.MatchCallvirt(typeof(Behaviour).GetPropertySetter(nameof(Behaviour.enabled)))
             ))
        {
            BuffsPlugin.Logger.LogError(il.Method.Name + " IL Hook 3 failed!");
            return;
        }
        Instruction instruction = c.Next;
        int stackingDisplayMethodLoc = 0;
        if (
             !c.TryGotoPrev(MoveType.After,
                 x => x.MatchLdarg(0),
                 x => x.MatchLdfld<BuffIcon>(nameof(BuffIcon.buffDef)),
                 x => x.MatchLdfld<BuffDef>(nameof(BuffDef.stackingDisplayMethod)),
                 x => x.MatchStloc(out stackingDisplayMethodLoc)
             ))
        {
            BuffsPlugin.Logger.LogError(il.Method.Name + " IL Hook 4 failed!");
            return;
        }
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloc, stackingDisplayMethodLoc);
        c.EmitDelegate(HandleCustomStackingDisplayMethods);
        c.Emit(OpCodes.Brtrue_S, instruction);
    }

    #endregion;

    #region Public

    public static int VanillaStackingDisplayMethodCount;
    public static int CustomStackingDisplayMethodCount => CustomStackingDisplayMethods.Count;

    /// <summary>
    /// Create and reserve StackingDisplayMethod for custom buff display text. Modify <see cref="BuffIcon.sharedStringBuilder"/> inside of your CustomStackingDisplayMethod method
    /// </summary>
    public static StackingDisplayMethod RegisterStackingDisplayMethod(CustomStackingDisplayMethod customStackingDisplayMethod)
    {
        SetHooks();
        int dotDefIndex = VanillaStackingDisplayMethodCount + CustomStackingDisplayMethodCount;
        int customArrayIndex = _customStackingDisplayMethod.Length;
        Array.Resize(ref _customStackingDisplayMethod, _customStackingDisplayMethod.Length + 1);
        _customStackingDisplayMethod[customArrayIndex] = customStackingDisplayMethod;
        StackingDisplayMethod stackingDisplayMethod = (StackingDisplayMethod)dotDefIndex;
        CustomStackingDisplayMethods.Add(stackingDisplayMethod);
        return stackingDisplayMethod;
    }
    /// <summary>
    /// Set animated sprite for your BuffDef
    /// </summary>
    /// <param name="buffDef"></param>
    /// <param name="simpleSpriteAnimation"></param>
    public static void SetSimpleSpriteAnimation(this BuffDef buffDef, SimpleSpriteAnimation simpleSpriteAnimation)
    {
        SetHooks();
        SingleSimpleSpriteAnimationDisplayClass singleSimpleSpriteAnimationDisplayClass = new SingleSimpleSpriteAnimationDisplayClass();
        singleSimpleSpriteAnimationDisplayClass.simpleSpriteAnimation = simpleSpriteAnimation;
        buffDef.SetCustomSimpleSpriteAnimation(singleSimpleSpriteAnimationDisplayClass.SingleCustomSimpleSpriteAnimation);
    }
    public delegate SimpleSpriteAnimation CustomSimpleSpriteAnimation(BuffIcon buffIcon);
    /// <summary>
    /// Set custom animated sprite for your BuffDef
    /// </summary>
    /// <param name="buffDef"></param>
    /// <param name="customSimpleSpriteAnimation"></param>
    public static void SetCustomSimpleSpriteAnimation(this BuffDef buffDef, CustomSimpleSpriteAnimation customSimpleSpriteAnimation)
    {
        SetHooks();
        if (_init)
        {
            _buffDefSimpleSpriteAnimation[(int)buffDef.buffIndex] = customSimpleSpriteAnimation;
            return;
        }
        if (_pendingSimpleSpriteAnimation.ContainsKey(buffDef))
        {
            _pendingSimpleSpriteAnimation[buffDef] = customSimpleSpriteAnimation;
        }
        else
        {
            _pendingSimpleSpriteAnimation.Add(buffDef, customSimpleSpriteAnimation);
        }
    }
    /// <summary>
    /// Get custom animated sprite of your BuffDef
    /// </summary>
    /// <param name="buffDef"></param>
    public static CustomSimpleSpriteAnimation GetCustomSimpleSpriteAnimation(this BuffDef buffDef)
    {
        SetHooks();
        CustomSimpleSpriteAnimation customSimpleSpriteAnimation;
        if (_init)
        {
            return _buffDefSimpleSpriteAnimation[(int)buffDef.buffIndex];
        }
        if (_pendingSimpleSpriteAnimation.TryGetValue(buffDef, out customSimpleSpriteAnimation))
        {
            return customSimpleSpriteAnimation;
        }
        return null;
    }
    public enum ModdedBuffFlag { };
    /// <summary>
    /// Reserve ModdedBodyFlag to use it with
    /// <see cref="AddModdedBuffFlag(BuffDef, ModdedBuffFlag)"/>,
    /// <see cref="RemoveModdedBuffFlag(BuffDef, ModdedBuffFlag)"/> and
    /// <see cref="HasModdedBuffFlag(BuffDef, ModdedBuffFlag))"/>
    /// </summary>
    /// <returns></returns>
    public static ModdedBuffFlag ReserveBuffFlag()
    {
        SetHooks();
        if (ModdedBuffFlagCount >= CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection)
        {
            //I doubt this is ever gonna happen, but just in case.
            throw new IndexOutOfRangeException($"Reached the limit of {CompressedFlagArrayUtilities.sectionsCount * CompressedFlagArrayUtilities.flagsPerSection} ModdedBuffFlags. Please contact R2API developers to increase the limit");
        }

        ModdedBuffFlagCount++;

        return (ModdedBuffFlag)ModdedBuffFlagCount;
    }
    /// <summary>
    /// Reserved ModdedBuffFlagCount count
    /// </summary>
    public static int ModdedBuffFlagCount { get; private set; }
    /// <summary>
    /// Adding ModdedBuffFlag to BuffDef. You can add more than one body flag to one BuffDef
    /// </summary>
    /// <param name="buffDef"></param>
    /// <param name="moddedBuffFlag"></param>
    public static void AddModdedBuffFlag(this BuffDef buffDef, ModdedBuffFlag moddedBuffFlag) => AddModdedBuffFlagInternal(buffDef, moddedBuffFlag);
    /// <summary>
    /// Removing ModdedBuffFlag from BuffDef instance.
    /// </summary>
    /// <param name="buffDef"></param>
    /// <param name="moddedBuffFlag"></param>
    public static bool RemoveModdedBuffFlag(this BuffDef buffDef, ModdedBuffFlag moddedBuffFlag) => RemoveModdedBuffFlagInternal(buffDef, moddedBuffFlag);
    /// <summary>
    /// Checks if BuffDef instance has any ModdedBuffFlag assigned. One BuffDef can have more than one buff flag.
    /// </summary>
    /// <param name="buffDef"></param>
    /// <returns></returns>
    public static bool HasAnyModdedBodyFlag(this BuffDef buffDef)
    {
        SetHooks();
        byte[] bytes;
        if (_init)
        {
            bytes = _moddedBuffFlags[(int)buffDef.buffIndex];
            return bytes is not null && bytes.Length > 0;
        }
        if (_pendingModdedBuffFlags.TryGetValue(buffDef, out bytes))
        {
            return bytes is not null && bytes.Length > 0;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if BuffDef instance has ModdedBuffFlag assigned. One BuffDef can have more than one buff flag.
    /// </summary>
    /// <param name="buffDef"></param>
    /// <param name="moddedBuffFlag"></param>
    /// <returns></returns>
    public static bool HasModdedBuffFlag(this BuffDef buffDef, ModdedBuffFlag moddedBuffFlag) => HasModdedBuffFlagInternal(buffDef, moddedBuffFlag);
    #endregion

    #region Internal
    private static void AddModdedBuffFlagInternal(BuffDef buffDef, ModdedBuffFlag moddedBuffFlag)
    {
        SetHooks();
        if (!CheckRange(moddedBuffFlag)) return;
        byte[] bytes;
        if (_init)
        {
            bytes = _moddedBuffFlags[(int)buffDef.buffIndex];
            if (bytes == null)
            {
                bytes = new byte[0];
            }
            CompressedFlagArrayUtilities.AddImmutable(ref bytes, (int)moddedBuffFlag - 1);
            _moddedBuffFlags[(int)buffDef.buffIndex] = bytes;
            return;
        }
        if (_pendingModdedBuffFlags.TryGetValue(buffDef, out bytes))
        {
            CompressedFlagArrayUtilities.AddImmutable(ref bytes, (int)moddedBuffFlag - 1);
            _pendingModdedBuffFlags[buffDef] = bytes;
        }
        else
        {
            bytes = new byte[0];
            CompressedFlagArrayUtilities.AddImmutable(ref bytes, (int)moddedBuffFlag - 1);
            _pendingModdedBuffFlags.Add(buffDef, bytes);
        }
    }
    private static bool RemoveModdedBuffFlagInternal(BuffDef buffDef, ModdedBuffFlag moddedBuffFlag)
    {
        SetHooks();
        if (!CheckRange(moddedBuffFlag)) return false;
        byte[] bytes;
        if (_init)
        {
            bytes = _moddedBuffFlags[(int)buffDef.buffIndex];
            if (bytes == null) return false;
            var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref bytes, (int)moddedBuffFlag - 1);
            _moddedBuffFlags[(int)buffDef.buffIndex] = bytes;
            return removed;
        }
        if (_pendingModdedBuffFlags.TryGetValue(buffDef, out bytes))
        {
            var removed = CompressedFlagArrayUtilities.RemoveImmutable(ref bytes, (int)moddedBuffFlag - 1);
            _pendingModdedBuffFlags[buffDef] = bytes;
            return removed;
        }
        else
        {
            return false;
        }
    }
    private static bool HasModdedBuffFlagInternal(BuffDef buffDef, ModdedBuffFlag moddedBuffFlag)
    {
        SetHooks();
        if (!CheckRange(moddedBuffFlag)) return false;
        byte[] bytes;
        if (_init)
        {
            bytes = _moddedBuffFlags[(int)buffDef.buffIndex];
            if (bytes == null) return false;
            return CompressedFlagArrayUtilities.Has(bytes, (int)moddedBuffFlag - 1);
        }
        if (_pendingModdedBuffFlags.TryGetValue(buffDef, out bytes))
        {
            return CompressedFlagArrayUtilities.Has(bytes, (int)moddedBuffFlag - 1);
        }
        else
        {
            return false;
        }
        
    }
    private static bool CheckRange(ModdedBuffFlag moddedBuffFlag)
    {
        if ((int)moddedBuffFlag > ModdedBuffFlagCount || (int)moddedBuffFlag < 1)
        {
            BuffsPlugin.Logger.LogError($"Parameter '{nameof(moddedBuffFlag)}' with value {moddedBuffFlag} is out of range of registered types (1-{ModdedBuffFlagCount})\n{new StackTrace(true)}");
            return false;
        }
        return true;
    }
    private static readonly List<StackingDisplayMethod> CustomStackingDisplayMethods = new List<StackingDisplayMethod>();
    public delegate void CustomStackingDisplayMethod(BuffIcon buffIcon);
    private static CustomStackingDisplayMethod[] _customStackingDisplayMethod = new CustomStackingDisplayMethod[0];
    private static Dictionary<BuffDef, byte[]> _pendingModdedBuffFlags = [];
    private static Dictionary<BuffDef, CustomSimpleSpriteAnimation> _pendingSimpleSpriteAnimation = [];
    private static FixedConditionalWeakTable<BuffIcon, SimpleSpriteAnimator> _buffIconSimpleSpriteAnimator = [];
    private static void DisableSimpleSpriteAnimator(BuffIcon buffIcon)
    {
        if (_buffIconSimpleSpriteAnimator.TryGetValue(buffIcon, out SimpleSpriteAnimator simpleSpriteAnimator) && simpleSpriteAnimator.enabled) simpleSpriteAnimator.enabled = false;
    }
    private static bool CheckSimpleSpriteAnimation(BuffIcon buffIcon)
    {
        CustomSimpleSpriteAnimation customSimpleSpriteAnimation = buffIcon.buffDef.GetCustomSimpleSpriteAnimation();
        SimpleSpriteAnimation simpleSpriteAnimation = customSimpleSpriteAnimation == null ? null : customSimpleSpriteAnimation.Invoke(buffIcon);
        if (simpleSpriteAnimation) return true;
        DisableSimpleSpriteAnimator(buffIcon);
        return false;
    }
    private static Sprite HandleSimpleSpriteAnimation(BuffIcon buffIcon)
    {
        CustomSimpleSpriteAnimation customSimpleSpriteAnimation = buffIcon.buffDef.GetCustomSimpleSpriteAnimation();
        SimpleSpriteAnimation simpleSpriteAnimation = customSimpleSpriteAnimation == null ? null : customSimpleSpriteAnimation.Invoke(buffIcon);
        if (simpleSpriteAnimation)
        {
            if (!_buffIconSimpleSpriteAnimator.TryGetValue(buffIcon, out SimpleSpriteAnimator simpleSpriteAnimator))
            {
                simpleSpriteAnimator = buffIcon.gameObject.AddComponent<SimpleSpriteAnimator>();
                simpleSpriteAnimator.target = buffIcon.iconImage;
                simpleSpriteAnimator.animation = simpleSpriteAnimation;
                simpleSpriteAnimator.Tick();
                _buffIconSimpleSpriteAnimator.Add(buffIcon, simpleSpriteAnimator);
            }
            else
            {
                if (simpleSpriteAnimator.animation != simpleSpriteAnimation)
                {
                    simpleSpriteAnimator.animation = simpleSpriteAnimation;
                    simpleSpriteAnimator.Tick();
                }
                if (!simpleSpriteAnimator.enabled) simpleSpriteAnimator.enabled = true;
            }
        }
        else
        {
            DisableSimpleSpriteAnimator(buffIcon);
        }
        return buffIcon.iconImage.sprite;
    }
    private static bool HandleCustomStackingDisplayMethods(BuffIcon buffIcon, StackingDisplayMethod stackingDisplayMethod)
    {
        if ((int)stackingDisplayMethod >= VanillaStackingDisplayMethodCount)
        {
            _customStackingDisplayMethod[(int)stackingDisplayMethod - VanillaStackingDisplayMethodCount]?.Invoke(buffIcon);
            return true;
        }
        return false;
    }
    private class SingleSimpleSpriteAnimationDisplayClass
    {
        public SimpleSpriteAnimation simpleSpriteAnimation;
        public SimpleSpriteAnimation SingleCustomSimpleSpriteAnimation(BuffIcon buffIcon) => simpleSpriteAnimation;
    }
    #endregion
}
