using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API;

/// <summary>
/// API for adding damage over time effects to the game.
/// </summary>
#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class DotAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".dot";
    public const string PluginName = R2API.PluginName + ".Dot";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;

    private static DotController.DotDef[] DotDefs
    {
        get => DotController.dotDefs;
        set => DotController.dotDefs = value;
    }

    private static void ResizeDotDefs(int newSize)
    {
        var dotDefs = DotDefs;
        Array.Resize(ref dotDefs, newSize);
        DotDefs = dotDefs;
    }

    private static readonly List<DotController.DotDef> CustomDots = new List<DotController.DotDef>();

    public static int VanillaDotCount;
    public static int CustomDotCount => CustomDots.Count;


    private static readonly Dictionary<DotController, bool[]> ActiveCustomDots = new Dictionary<DotController, bool[]>();

    /// <summary>
    /// Allows for custom behaviours when applying the dot. EG, percentburn. <see cref="DotController.AddDot(GameObject, float, DotController.DotIndex, float, uint?, float?, DotController.DotIndex?)"/>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="dotStack"></param>
    public delegate void CustomDotBehaviour(DotController self, DotController.DotStack dotStack);

    private static CustomDotBehaviour[] _customDotBehaviours = new CustomDotBehaviour[0];

    /// <summary>
    /// Allows custom visuals for your buff. think bleeding etc. <see cref="DotController.FixedUpdate"/>
    /// </summary>
    /// <param name="self"></param>
    public delegate void CustomDotVisual(DotController self);

    private static CustomDotVisual[] _customDotVisuals = new CustomDotVisual[0];

    /// <summary>
    /// Allows for custom damage evaluation on dot damage evaluation. <see cref="DotController.EvaluateDotStacksForType(DotController.DotIndex, float, out int)"/>
    /// </summary>
    /// <param name="self"></param>
    /// <param name="pendingDamage"></param>
    public delegate void CustomDotDamageEvaluation(DotController self, DotController.PendingDamage pendingDamage);

    private static CustomDotDamageEvaluation[] _customDotDamageEvaluations = new CustomDotDamageEvaluation[0];


    /// <summary>
    /// customDotBehaviour code will be executed when the dot is added to the target.
    /// Please refer to the game AddDot() method for example use case.
    /// customDotVisual code will be executed in the FixedUpdate of the DotController.
    /// Please refer to the game FixedUpdate() method for example use case.
    /// customDotDamageEvaluation code will be executed instead of TakeDamage in the EvaluateDotStacksForType of the DotController.
    /// </summary>
    /// <param name="dotDef"></param>
    /// <param name="customDotBehaviour"></param>
    /// <param name="customDotVisual"></param>
    /// <param name="customDotDamageEvaluation"></param>
    /// <returns></returns>
    public static DotController.DotIndex RegisterDotDef(DotController.DotDef? dotDef,
        CustomDotBehaviour? customDotBehaviour = null, CustomDotVisual? customDotVisual = null, CustomDotDamageEvaluation? customDotDamageEvaluation = null)
    {
        DotAPI.SetHooks();
        var dotDefIndex = VanillaDotCount + CustomDotCount;

        if (DotDefs != null)
        {
            ResizeDotDefs(dotDefIndex + 1);
            DotDefs[dotDefIndex] = dotDef;
        }

        CustomDots.Add(dotDef);

        var customArrayIndex = _customDotBehaviours.Length;
        Array.Resize(ref _customDotBehaviours, _customDotBehaviours.Length + 1);
        _customDotBehaviours[customArrayIndex] = customDotBehaviour;

        Array.Resize(ref _customDotVisuals, _customDotVisuals.Length + 1);
        _customDotVisuals[customArrayIndex] = customDotVisual;

        Array.Resize(ref _customDotDamageEvaluations, _customDotDamageEvaluations.Length + 1);
        _customDotDamageEvaluations[customArrayIndex] = customDotDamageEvaluation;

        if (dotDef.associatedBuff != null)
        {
            DotPlugin.Logger.LogInfo($"Custom Dot (Index: {dotDefIndex}) that uses Buff : {dotDef.associatedBuff.name} added");
        }
        else
        {
            DotPlugin.Logger.LogInfo($"Custom Dot (Index: {dotDefIndex}) with no associated Buff added");
        }


        return (DotController.DotIndex)dotDefIndex;
    }
    /// <summary>
    /// customDotBehaviour code will be executed when the dot is added to the target.
    /// Please refer to the game AddDot() method for example use case.
    /// customDotVisual code will be executed in the FixedUpdate of the DotController.
    /// Please refer to the game FixedUpdate() method for example use case.
    /// </summary>
    /// <param name="dotDef"></param>
    /// <param name="customDotBehaviour"></param>
    /// <param name="customDotVisual"></param>
    /// <returns></returns>
    public static DotController.DotIndex RegisterDotDef(DotController.DotDef? dotDef,
        CustomDotBehaviour? customDotBehaviour, CustomDotVisual? customDotVisual)
    {
        DotAPI.SetHooks();
        return RegisterDotDef(dotDef, customDotBehaviour, customDotVisual, null);
    }
    /// <summary>
    /// Unrolled version of RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual)
    /// <see cref="RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual, CustomDotDamageEvaluation)"/>
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="damageCoefficient"></param>
    /// <param name="colorIndex"></param>
    /// <param name="associatedBuff">The buff associated with the DOT, can be null</param>
    /// <param name="customDotBehaviour"></param>
    /// <param name="customDotVisual"></param>
    /// <param name="customDotDamageEvaluation"></param>
    /// <returns></returns>
    public static DotController.DotIndex RegisterDotDef(float interval, float damageCoefficient,
        DamageColorIndex colorIndex, BuffDef associatedBuff = null, CustomDotBehaviour customDotBehaviour = null,
        CustomDotVisual customDotVisual = null, CustomDotDamageEvaluation customDotDamageEvaluation = null)
    {
        DotAPI.SetHooks();
        var dotDef = new DotController.DotDef
        {
            associatedBuff = associatedBuff,
            damageCoefficient = damageCoefficient,
            interval = interval,
            damageColorIndex = colorIndex
        };
        return RegisterDotDef(dotDef, customDotBehaviour, customDotVisual);
    }
    /// <summary>
    /// Unrolled version of RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual)
    /// <see cref="RegisterDotDef(DotController.DotDef, CustomDotBehaviour, CustomDotVisual)"/>
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="damageCoefficient"></param>
    /// <param name="colorIndex"></param>
    /// <param name="associatedBuff">The buff associated with the DOT, can be null</param>
    /// <param name="customDotBehaviour"></param>
    /// <param name="customDotVisual"></param>
    /// <returns></returns>
    public static DotController.DotIndex RegisterDotDef(float interval, float damageCoefficient,
        DamageColorIndex colorIndex, BuffDef associatedBuff, CustomDotBehaviour customDotBehaviour,
        CustomDotVisual customDotVisual)
    {
        DotAPI.SetHooks();
        return RegisterDotDef(interval, damageCoefficient, colorIndex, associatedBuff, customDotBehaviour, customDotVisual, null);
    }

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.DotController.InitDotCatalog += RetrieveVanillaCount;
        IL.RoR2.DotController.Awake += ResizeTimerArray;
        On.RoR2.DotController.InitDotCatalog += AddCustomDots;
        On.RoR2.DotController.Awake += TrackActiveCustomDots;
        On.RoR2.DotController.OnDestroy += TrackActiveCustomDots2;
        On.RoR2.DotController.GetDotDef += GetDotDefSupportCustomDefs;
        On.RoR2.DotController.FixedUpdate += FixedUpdateEvaluateCustomDotStacksAndVisuals;
        IL.RoR2.DotController.InflictDot_refInflictDotInfo += FixInflictDotReturnCheck;
        IL.RoR2.DotController.AddDot_GameObject_float_HurtBox_DotIndex_float_Nullable1_Nullable1_Nullable1 += CallCustomDotBehaviours;
        On.RoR2.DotController.HasDotActive += OnHasDotActive;
        IL.RoR2.DotController.EvaluateDotStacksForType += EvaluateDotStacksForType;

        IL.RoR2.GlobalEventManager.ProcDeathMark += FixDeathMark;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.DotController.InitDotCatalog -= RetrieveVanillaCount;
        IL.RoR2.DotController.Awake -= ResizeTimerArray;
        On.RoR2.DotController.Awake -= TrackActiveCustomDots;
        On.RoR2.DotController.OnDestroy -= TrackActiveCustomDots2;
        On.RoR2.DotController.GetDotDef -= GetDotDefSupportCustomDefs;
        On.RoR2.DotController.FixedUpdate -= FixedUpdateEvaluateCustomDotStacksAndVisuals;
        IL.RoR2.DotController.InflictDot_refInflictDotInfo -= FixInflictDotReturnCheck;
        IL.RoR2.DotController.AddDot_GameObject_float_HurtBox_DotIndex_float_Nullable1_Nullable1_Nullable1 -= CallCustomDotBehaviours;
        On.RoR2.DotController.HasDotActive -= OnHasDotActive;
        IL.RoR2.DotController.EvaluateDotStacksForType -= EvaluateDotStacksForType;

        IL.RoR2.GlobalEventManager.ProcDeathMark -= FixDeathMark;

        _hooksEnabled = false;
    }

    private static void EvaluateDotStacksForType(ILContext il)
    {
        var c = new ILCursor(il);
        int pendingDamageLocNumber = 0;

        // ReSharper disable once InconsistentNaming
        static void ILFailMessage(int index)
        {
            DotPlugin.Logger.LogError(
                $"Failed finding IL Instructions. Aborting OnEvaluateDotStacksForType IL Hook {index}");
        }

        if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt(typeof(DotController).GetPropertyGetter(nameof(DotController.victimHealthComponent))),
                i => i.MatchLdloc(out _),
                i => i.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.TakeDamage))
                ))
        {
            Instruction instruction2 = c.Next;
            Instruction instruction = c.Next.Next.Next.Next.Next;
            if (c.TryGotoPrev(MoveType.Before,
                    i => i.MatchStloc(out pendingDamageLocNumber),
                    i => i.MatchBr(out _),
                    i => i.MatchLdloc(out _),
                    i => i.MatchLdloc(out _)))
            {
                c.Goto(instruction);
                instruction = c.Emit(OpCodes.Ldarg_0).Prev;
                c.Emit(OpCodes.Ldarg_1);    
                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc, pendingDamageLocNumber);
                // Harmony method of getting DotController.PendingDamage
                //c.Emit(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<DotController.PendingDamage>), "Item"));
                c.EmitDelegate<Func<List<DotController.PendingDamage>, int, DotController.PendingDamage>>((list, index) => { return list[index]; });
                c.EmitDelegate<Action<DotController, DotController.DotIndex, DotController.PendingDamage>>((self, dotIndex, pendingDamage) =>
                {
                    if ((int)dotIndex >= VanillaDotCount)
                    {
                        var customDotIndex = (int)dotIndex - VanillaDotCount;
                        _customDotDamageEvaluations[customDotIndex]?.Invoke(self, pendingDamage);
                    }
                });
                c.Goto(instruction2);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<DotController.DotIndex, bool>>((dotIndex) =>
                {
                    bool baseDamageEvaluation = true;
                    if ((int)dotIndex >= VanillaDotCount)
                    {
                        var customDotIndex = (int)dotIndex - VanillaDotCount;
                        baseDamageEvaluation = _customDotDamageEvaluations[customDotIndex] == null;
                    }
                    return baseDamageEvaluation;
                });
                c.Emit(OpCodes.Brfalse_S, instruction);
            }
            else
            {
                ILFailMessage(2);
            }
        }
        else
        {
            ILFailMessage(1);
        }
    }

    private static void RetrieveVanillaCount(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
                i => i.MatchLdcI4(out VanillaDotCount),
                i => i.MatchNewarr<DotController.DotDef>()))
        {
        }
        else
        {
            DotPlugin.Logger.LogError("Failed finding IL Instructions. Aborting RetrieveVanillaCount IL Hook");
        }
    }

    private static void ResizeTimerArray(ILContext il)
    {
        var c = new ILCursor(il);
        if (c.TryGotoNext(
                i => i.MatchLdcI4(VanillaDotCount),
                i => i.MatchNewarr<float>()))
        {
            c.Index++;
            c.EmitDelegate<Func<int, int>>(i => DotDefs.Length);
        }
        else
        {
            DotPlugin.Logger.LogError("Failed finding IL Instructions. Aborting ResizeTimerArray IL Hook");
        }
    }

    private static void AddCustomDots(On.RoR2.DotController.orig_InitDotCatalog orig)
    {
        orig();

        DotController.dotDefs = DotController.dotDefs.Concat(CustomDots).ToArray();
    }

    private static void TrackActiveCustomDots(On.RoR2.DotController.orig_Awake orig, DotController self)
    {
        orig(self);

        ActiveCustomDots.Add(self, new bool[CustomDotCount]);
    }

    private static void TrackActiveCustomDots2(On.RoR2.DotController.orig_OnDestroy orig, DotController self)
    {
        orig(self);

        ActiveCustomDots.Remove(self);
    }

    private static DotController.DotDef GetDotDefSupportCustomDefs(On.RoR2.DotController.orig_GetDotDef orig, DotController.DotIndex dotIndex)
    {
        return DotDefs[(int)dotIndex];
    }

    private static void FixedUpdateEvaluateCustomDotStacksAndVisuals(On.RoR2.DotController.orig_FixedUpdate orig, DotController self)
    {
        orig(self);

        if (NetworkServer.active)
        {
            for (var i = VanillaDotCount; i < DotDefs.Length; i++)
            {
                var dotDef = DotDefs[i];
                var dotTimers = self.dotTimers;

                float dotProcTimer = dotTimers[i] - Time.fixedDeltaTime;
                if (dotProcTimer <= 0f)
                {
                    dotProcTimer += dotDef.interval;

                    self.EvaluateDotStacksForType((DotController.DotIndex)i, dotDef.interval, out var remainingActive);

                    ActiveCustomDots[self][i - VanillaDotCount] = remainingActive != 0;
                }

                dotTimers[i] = dotProcTimer;
            }
        }

        for (var i = 0; i < CustomDotCount; i++)
        {
            try
            {
                if (ActiveCustomDots[self][i])
                {
                    _customDotVisuals[i]?.Invoke(self);
                }
            }
            catch (Exception e)
            {
                DotPlugin.Logger.LogError(e);
            }
        }
    }

    private static void FixInflictDotReturnCheck(ILContext il)
    {
        var c = new ILCursor(il);

        // ReSharper disable once InconsistentNaming
        static void ILFailMessage(int index)
        {
            DotPlugin.Logger.LogError(
                $"Failed finding IL Instructions. Aborting FixInflictDotReturnCheck IL Hook {index}");
        }


        if (c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(InflictDotInfo), nameof(InflictDotInfo.dotIndex)),
                x => x.MatchLdcI4(VanillaDotCount)
            ))
        {
            c.Prev.OpCode = OpCodes.Ldc_I4;
            c.Prev.Operand = int.MaxValue;
        }
        else
        {
            ILFailMessage(1);
        }
    }

    private static void CallCustomDotBehaviours(ILContext il)
    {
        var c = new ILCursor(il);
        int dotStackLoc = 0;

        // ReSharper disable once InconsistentNaming
        static void ILFailMessage(int index)
        {
            DotPlugin.Logger.LogError(
                $"Failed finding IL Instructions. Aborting OnAddDot IL Hook {index}");
        }

        if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdsfld<DotController>(nameof(DotController.dotStackPool)),
                i => i.MatchCallOrCallvirt(out _),
                i => i.MatchStloc(out dotStackLoc)))
        {
            if (c.TryGotoNext(
                    i => i.MatchLdarg(out _),
                    i => i.MatchSwitch(out _)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, dotStackLoc);
                c.EmitDelegate<Action<DotController, DotController.DotStack>>((self, dotStack) =>
                {
                    if ((int)dotStack.dotIndex >= VanillaDotCount)
                    {
                        var customDotIndex = (int)dotStack.dotIndex - VanillaDotCount;
                        _customDotBehaviours[customDotIndex]?.Invoke(self, dotStack);
                        ActiveCustomDots[self][customDotIndex] = true;
                    }
                });
            }
            else
            {
                ILFailMessage(2);
            }
        }
        else
        {
            ILFailMessage(1);
        }
    }

    private static bool OnHasDotActive(On.RoR2.DotController.orig_HasDotActive orig, DotController self,
        DotController.DotIndex dotIndex)
    {
        if ((int)dotIndex >= VanillaDotCount)
        {
            if (ActiveCustomDots.TryGetValue(self, out var activeDots))
            {
                return activeDots[(int)dotIndex - VanillaDotCount];
            }

            return false;
        }

        return orig(self, dotIndex);
    }

    private static void FixDeathMark(ILContext il)
    {
        var c = new ILCursor(il);
        int dotControllerLoc = 0;
        int numberOfDebuffAndDotLoc = 0;

        // ReSharper disable once InconsistentNaming
        static void ILFailMessage(int index)
        {
            DotPlugin.Logger.LogError(
                $"Failed finding IL Instructions. Aborting FixDeathMark IL Hook {index}");
        }

        if (c.TryGotoNext(i => i.MatchCallOrCallvirt(typeof(DotController), nameof(DotController.HasDotActive))))
        {
            if (c.TryGotoNext(i => i.MatchLdloc(out numberOfDebuffAndDotLoc)))
            {

            }
            else
            {
                ILFailMessage(2);
            }
        }
        else
        {
            ILFailMessage(1);
        }

        if (c.TryGotoPrev(MoveType.After,
                i => i.MatchCallOrCallvirt(typeof(DotController), nameof(DotController.FindDotController)),
                i => i.MatchStloc(out dotControllerLoc)))
        {

            static int CountCustomDots(DotController dotController, int numberOfDebuffAndDotLoc)
            {
                if (dotController)
                {
                    for (var i = VanillaDotCount; i < VanillaDotCount + CustomDotCount; i++)
                    {
                        var dotIndex = (DotController.DotIndex)i;
                        if (dotController.HasDotActive(dotIndex))
                        {
                            numberOfDebuffAndDotLoc++;
                        }
                    }
                }

                return numberOfDebuffAndDotLoc;
            }

            c.Emit(OpCodes.Ldloc, dotControllerLoc);
            c.Emit(OpCodes.Ldloc, numberOfDebuffAndDotLoc);
            c.EmitDelegate(CountCustomDots);
            c.Emit(OpCodes.Stloc, numberOfDebuffAndDotLoc);
        }
        else
        {
            ILFailMessage(3);
        }
    }
}
