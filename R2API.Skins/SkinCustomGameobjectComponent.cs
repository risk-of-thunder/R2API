using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;
public class SkinCustomGameobjectComponent : MonoBehaviour
{
    [Tooltip("CharacterModel of a character this object is instantiating to for custom uses. This field is null in Awake and first OnEnable")]
    [HideInInspector] public CharacterModel characterModel;
    [Tooltip("RendererInfos that will be added to CharacterModel.baseRendererInfos")]
    public CharacterModel.RendererInfo[] extraRendererInfos;
    [Tooltip("LightInfos that will be added to CharacterModel.baseLightInfos")]
    public CharacterModel.LightInfo[] extraLightInfos;
}
