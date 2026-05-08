using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;
public static partial class FootstepAPI
{
    private static bool _hooksEnabled;
    internal static void SetHooks()
    {
        if (_hooksEnabled) return;
        _hooksEnabled = true;
        IL.RoR2.FootstepHandler.Footstep_string_GameObject += FootstepHandler_Footstep_string_GameObject;
    }
    public struct FootstepReport
    {
        public Transform childTransform;
        public int childIndex;
        public string childName;
        public FootstepHandler footstepHandler;
        public SurfaceDef surfaceDef;
        public RaycastHit raycastHit;
        public GameObject footstepEffect;
    }
    public delegate void Footstep(FootstepReport footstepReport);
    public static event Footstep OnFootstep;
    private static void FootstepHandler_Footstep_string_GameObject(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int childTransformLocal = 0;
        if (
             c.TryGotoNext(MoveType.After,
                 x => x.MatchCallvirt<ChildLocator>(nameof(ChildLocator.FindChild)),
                 x => x.MatchStloc(out childTransformLocal)
             ))
        {
            int childIndexLocal = 0;
            if (
                c.TryGotoNext(MoveType.After,
                    x => x.MatchCallvirt<ChildLocator>(nameof(ChildLocator.FindChildIndex)),
                    x => x.MatchStloc(out childIndexLocal)
                ))
            {
                int raycastHitLocal = 0;
                if (
                    c.TryGotoNext(MoveType.After,
                        x => x.MatchLdloca(out raycastHitLocal),
                        x => x.MatchInitobj<RaycastHit>()
                    ))
                {
                    int surfaceDefLocal = 0;
                    if (
                        c.TryGotoNext(MoveType.After,
                        x => x.MatchCall<SurfaceDefProvider>(nameof(SurfaceDefProvider.GetObjectSurfaceDef)),
                        x => x.MatchStloc(out surfaceDefLocal)

                        ))
                    {
                        c.Emit(OpCodes.Ldloc, childTransformLocal);
                        c.Emit(OpCodes.Ldloc, childIndexLocal);
                        c.Emit(OpCodes.Ldarg_1);
                        c.Emit(OpCodes.Ldloca, raycastHitLocal);
                        c.Emit(OpCodes.Ldloc, surfaceDefLocal);
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldarg_2);
                        c.EmitDelegate(HandleFootstep);
                    }
                    else
                    {
                        CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 4 failed!");
                    }
                }
                else
                {
                    CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 3 failed!");
                }
            }
            else
            {
                CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 2 failed!");
            }
        }
        else
        {
            CharacterBodyPlugin.Logger.LogError(il.Method.Name + " IL Hook 1 failed!");
        }
    }
    private static void HandleFootstep(Transform transform, int index, string name, ref RaycastHit raycastHit, SurfaceDef surfaceDef, FootstepHandler footstepHandler, GameObject footstepEffect)
    {
        FootstepReport footstepReport = new FootstepReport
        {
            childIndex = index,
            childName = name,
            childTransform = transform,
            raycastHit = raycastHit,
            footstepHandler = footstepHandler,
            surfaceDef = surfaceDef,
            footstepEffect = footstepEffect
        };
        OnFootstep?.Invoke(footstepReport);
    }
    internal static void UnsetHooks()
    {
        if (!_hooksEnabled) return;
        _hooksEnabled = false;
        IL.RoR2.FootstepHandler.Footstep_string_GameObject -= FootstepHandler_Footstep_string_GameObject;
    }
}
