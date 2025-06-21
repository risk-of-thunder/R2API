using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API;

public static class EliteRamp
{
    private static int EliteRampPropertyID => Shader.PropertyToID("_EliteRamp");

    private static readonly List<(EliteDef eliteDef, Texture2D ramp)> elitesAndRamps = [];
    private static readonly Dictionary<EliteIndex, Texture2D> eliteIndexToTexture = [];

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

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        IL.RoR2.CharacterModel.UpdateMaterials -= UpdateRampProperly;
        RoR2Application.onLoad -= SetupDictionary;

        _hooksEnabled = false;
    }

    private static void UpdateRampProperly(ILContext il)
    {
        var c = new ILCursor(il);
        if (!c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterModel>(nameof(CharacterModel.propertyStorage)),
                x => x.MatchLdsfld(typeof(CommonShaderProperties), nameof(CommonShaderProperties._EliteIndex))
            ))
        {
            ElitesPlugin.Logger.LogError($"Elite Ramp ILHook failed #1");
            return;
        }

        if (!c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<MaterialPropertyBlock>(nameof(MaterialPropertyBlock.SetFloat))
            ))
        {
            ElitesPlugin.Logger.LogError($"Elite Ramp ILHook failed #2");
            return;
        }

        c.Emit(OpCodes.Ldarg, 0);
        c.EmitDelegate(UpdateRampFromModel);
    }

    private static void UpdateRampFromModel(CharacterModel model)
    {
        if (eliteIndexToTexture.TryGetValue(model.myEliteIndex, out var ramp))
            model.propertyStorage.SetTexture(EliteRampPropertyID, ramp);
    }

    private static void SetupDictionary()
    {
        eliteIndexToTexture[EliteIndex.None] = Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_GlobalTextures.texRampElites_psd).WaitForCompletion();

        foreach ((var eliteDef, var texture) in elitesAndRamps)
        {
            eliteIndexToTexture[eliteDef.eliteIndex] = texture;
        }

        elitesAndRamps.Clear();
    }

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteDef"></param>
    /// <param name="ramp"></param>
    public static void AddRamp(EliteDef eliteDef, Texture2D ramp)
    {
        EliteRamp.SetHooks();

        if (eliteDef)
        {
            eliteDef.shaderEliteRampIndex = 0;
            elitesAndRamps.Add((eliteDef, ramp));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteDefs"></param>
    /// <param name="ramp"></param>
    public static void AddRampToMultipleElites(IEnumerable<EliteDef> eliteDefs, Texture2D ramp)
    {
        EliteRamp.SetHooks();

        foreach (EliteDef def in eliteDefs)
        {
            AddRamp(def, ramp);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eliteIndex"></param>
    /// <param name="ramp"></param>
    /// <returns></returns>
    public static bool TryGetRamp(EliteIndex eliteIndex, out Texture2D ramp)
    {
        EliteRamp.SetHooks();

        ramp = null;
        return eliteIndex != EliteIndex.None && eliteIndexToTexture.TryGetValue(eliteIndex, out ramp);
    }
}
