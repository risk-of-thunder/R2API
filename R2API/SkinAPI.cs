using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace R2API {
    [Obsolete( "Please use LoadoutAPI instead" )]
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class SkinAPI {
        /// <summary>
        /// A container struct for all SkinDef parameters.
        /// Use this to set skinDef values, then call CreateNewSkinDef().
        /// </summary>
        public struct SkinDefInfo {
            public SkinDef[] baseSkins;
            public Sprite icon;
            public string nameToken;
            public string unlockableName;
            public GameObject rootObject;
            public CharacterModel.RendererInfo[] rendererInfos;
            public string name;
        }

        /// <summary>
        /// Creates a new SkinDef from a SkinDefInfo.
        /// Note that this prevents null-refs by disabling SkinDef awake while the SkinDef is being created.
        /// The things that occur during awake are performed when first applied to a character instead.
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static SkinDef CreateNewSkinDef( SkinDefInfo skin ) {
            On.RoR2.SkinDef.Awake += DoNothing;

            SkinDef newSkin = ScriptableObject.CreateInstance<SkinDef>();

            newSkin.baseSkins = skin.baseSkins;
            newSkin.icon = skin.icon;
            newSkin.unlockableName = skin.unlockableName;
            newSkin.rootObject = skin.rootObject;
            newSkin.rendererInfos = skin.rendererInfos;
            newSkin.nameToken = skin.nameToken;
            newSkin.name = skin.name;

            On.RoR2.SkinDef.Awake -= DoNothing;
            return newSkin;
        }

        private static void DoNothing( On.RoR2.SkinDef.orig_Awake orig, SkinDef self ) {
            //Intentionally do nothing
        }
    }
}
