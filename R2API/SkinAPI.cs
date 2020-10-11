// Nullable context not needed for deprecated APIs
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using System;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API {
    [Obsolete("Please use LoadoutAPI instead")]
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
        public static SkinDef CreateNewSkinDef(SkinDefInfo skin) {
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

        private static void DoNothing(On.RoR2.SkinDef.orig_Awake orig, SkinDef self) {
            //Intentionally do nothing
        }
    }
}
#pragma warning restore CS8605 // Unboxing a possibly null value.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
