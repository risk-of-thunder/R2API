using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API;

public static class EliteRamp
{
    private static List<(EliteDef, Texture2D)> elitesAndRamps = new();
    private static Dictionary<EliteIndex, Texture2D> eliteIndexToTexture = new();
    private static Texture2D vanillaEliteRamp;
    private static int EliteRampPropertyID => Shader.PropertyToID("_EliteRamp");

    private static bool _hooksEnabled = false;

    #region Hooks
    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        IL.RoR2.CharacterModel.UpdateMaterials += UpdateRampProperly;
        RoR2Application.onLoad += SetupDictionary;
        vanillaEliteRamp = Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/ColorRamps/texRampElites.psd").WaitForCompletion();

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CharacterModel.UpdateMaterials -= UpdateRampProperly;
        RoR2Application.onLoad -= SetupDictionary;
        vanillaEliteRamp = null;

        _hooksEnabled = false;
    }

    private static void UpdateRampProperly(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        var firstMatchSuccesful = c.TryGotoNext(MoveType.After,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<CharacterModel>(nameof(CharacterModel.propertyStorage)),
            x => x.MatchLdsfld(typeof(CommonShaderProperties), nameof(CommonShaderProperties._EliteIndex)));

        var secondMatchSuccesful = c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<MaterialPropertyBlock>(nameof(MaterialPropertyBlock.SetFloat)));

        if (firstMatchSuccesful && secondMatchSuccesful)
        {
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Action<CharacterModel>>(UpdateRampProperly);
        }
        else
        {
            ElitesPlugin.Logger.LogError($"Elite Ramp ILHook failed");
        }

        static void UpdateRampProperly(CharacterModel charModel)
        {
            if (charModel.myEliteIndex != EliteIndex.None && eliteIndexToTexture.TryGetValue(charModel.myEliteIndex, out var ramp))
            {
                charModel.propertyStorage.SetTexture(EliteRampPropertyID, ramp);
                return;
            }
            charModel.propertyStorage.SetTexture(EliteRampPropertyID, vanillaEliteRamp);
        }
    }

    private static void SetupDictionary()
    {
        foreach ((var eliteDef, var texture) in elitesAndRamps)
        {
            eliteIndexToTexture[eliteDef.eliteIndex] = texture;
        }
        elitesAndRamps.Clear();
    }
    #endregion

    public static void AddRamp(EliteDef eliteDef, Texture2D ramp)
    {
        EliteRamp.SetHooks();
        try
        {
            eliteDef.shaderEliteRampIndex = 0;
            elitesAndRamps.Add((eliteDef, ramp));
        }
        catch (Exception ex)
        {
            ElitesPlugin.Logger.LogError(ex);
        }
    }

    public static void AddRampToMultipleElites(IEnumerable<EliteDef> eliteDefs, Texture2D ramp)
    {
        EliteRamp.SetHooks();
        foreach (EliteDef def in eliteDefs)
        {
            AddRamp(def, ramp);
        }
    }
}
