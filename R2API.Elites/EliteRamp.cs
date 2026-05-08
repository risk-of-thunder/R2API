using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API;

public static class EliteRamp
{
    private static readonly Dictionary<EliteDef, Texture2D> _elitesAndRamps = [];
    private static readonly Dictionary<EliteIndex, Texture2D> _eliteIndexToTexture = [];
    private static int _eliteRampPropertyID;

    private static bool _hooksEnabled = false;

    #region Hooks

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        Addressables.LoadAssetAsync<Texture2D>(RoR2_Base_Common_GlobalTextures.texRampElites_psd).Completed +=
            (task) => _eliteIndexToTexture[EliteIndex.None] = task.Result;

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
        if (!_eliteIndexToTexture.TryGetValue(model.myEliteIndex, out var ramp))
            ramp = _eliteIndexToTexture[EliteIndex.None];

        model.propertyStorage.SetTexture(_eliteRampPropertyID, ramp);
    }

    private static void SetupDictionary()
    {
        _eliteRampPropertyID = Shader.PropertyToID("_EliteRamp");

        foreach ((var eliteDef, var texture) in _elitesAndRamps)
        {
            if (eliteDef && eliteDef.eliteIndex != EliteIndex.None)
                _eliteIndexToTexture[eliteDef.eliteIndex] = texture;
        }

        _elitesAndRamps.Clear();
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

        if (!eliteDef)
        {
            ElitesPlugin.Logger.LogError("Attempted to set a texture ramp for a null elite def!");
            return;
        }

        if (_elitesAndRamps.TryGetValue(eliteDef, out var existingRamp) && existingRamp != ramp)
        {
            ElitesPlugin.Logger.LogWarning($"Texture ramp for {eliteDef.name ?? eliteDef.modifierToken} already exists. The new texture will be used instead.");
        }

        eliteDef.shaderEliteRampIndex = 0;
        _elitesAndRamps[eliteDef] = ramp;
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
        return eliteIndex != EliteIndex.None && _eliteIndexToTexture.TryGetValue(eliteIndex, out ramp);
    }
}
