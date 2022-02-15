using EntityStates;
using R2API.ContentManagment;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace R2API {

    [R2APISubmodule]
    public static class LoadoutAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        // ReSharper disable once ConvertToAutoProperty
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }

        private static bool _loaded;

        private static readonly HashSet<SkinDef> AddedSkins = new HashSet<SkinDef>();

        #region Adding Skills

        /// <summary>
        /// Adds a type for a skill EntityState to the SkillsCatalog.
        /// State must derive from EntityStates.EntityState.
        /// Note that SkillDefs and SkillFamiles must also be added seperately.
        /// </summary>
        /// <param name="t">The type to add</param>
        /// <returns>True if succesfully added</returns>
        public static bool AddSkill(Type? t) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            if (t == null || !t.IsSubclassOf(typeof(EntityState)) || t.IsAbstract) {
                R2API.Logger.LogError("Invalid skill type.");
                return false;
            }

            R2APIContentManager.HandleEntityState(Assembly.GetCallingAssembly(), t);
            return true;
        }

        /// <summary>
        /// Creates a SerializableEntityStateType with a much simpler syntax
        /// Effectively the same as new SerializableEntityStateType(typeof(T))
        /// </summary>
        /// <typeparam name="T">The state type</typeparam>
        /// <returns>The created SerializableEntityStateType</returns>
        public static SerializableEntityStateType StateTypeOf<T>()
            where T : EntityState, new() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            R2APIContentManager.HandleEntityState(Assembly.GetCallingAssembly(), typeof(T));
            return new SerializableEntityStateType(typeof(T));
        }

        /// <summary>
        /// Registers an event to add a SkillDef to the SkillDefCatalog.
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="s">The SkillDef to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillDef(SkillDef? s) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            if (!s) {
                R2API.Logger.LogError("Invalid SkillDef");
                return false;
            }
            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), s);
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillFamily to the SkillFamiliesCatalog
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="sf">The skillfamily to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillFamily(SkillFamily? sf) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            if (!sf) {
                R2API.Logger.LogError("Invalid SkillFamily");
                return false;
            }
            R2APIContentManager.HandleContentAddition(Assembly.GetCallingAssembly(), sf);
            return true;
        }

        #endregion Adding Skills

        #region Adding Skins

        /// <summary>
        /// Creates a skin icon sprite styled after the ones already in the game.
        /// </summary>
        /// <param name="top">The color of the top portion</param>
        /// <param name="right">The color of the right portion</param>
        /// <param name="bottom">The color of the bottom portion</param>
        /// <param name="left">The color of the left portion</param>
        /// <returns>The icon sprite</returns>
        public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            return CreateSkinIcon(top, right, bottom, left, new Color(0.6f, 0.6f, 0.6f));
        }

        /// <summary>
        /// Creates a skin icon sprite styled after the ones already in the game.
        /// </summary>
        /// <param name="top">The color of the top portion</param>
        /// <param name="right">The color of the right portion</param>
        /// <param name="bottom">The color of the bottom portion</param>
        /// <param name="left">The color of the left portion</param>
        /// <param name="line">The color of the dividing lines</param>
        /// <returns></returns>
        public static Sprite CreateSkinIcon(Color top, Color right, Color bottom, Color left, Color line) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            new IconTexJob {
                Top = top,
                Bottom = bottom,
                Right = right,
                Left = left,
                Line = line,
                TexOutput = tex.GetRawTextureData<Color32>()
            }.Schedule(16384, 1).Complete();
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
        }

        private struct IconTexJob : IJobParallelFor {

            [ReadOnly]
            public Color32 Top;

            [ReadOnly]
            public Color32 Right;

            [ReadOnly]
            public Color32 Bottom;

            [ReadOnly]
            public Color32 Left;

            [ReadOnly]
            public Color32 Line;

            public NativeArray<Color32> TexOutput;

            public void Execute(int index) {
                int x = index % 128 - 64;
                int y = index / 128 - 64;

                if (Math.Abs(Math.Abs(y) - Math.Abs(x)) <= 2) {
                    TexOutput[index] = Line;
                    return;
                }
                if (y > x && y > -x) {
                    TexOutput[index] = Top;
                    return;
                }
                if (y < x && y < -x) {
                    TexOutput[index] = Bottom;
                    return;
                }
                if (y > x && y < -x) {
                    TexOutput[index] = Left;
                    return;
                }
                if (y < x && y > -x) {
                    TexOutput[index] = Right;
                }
            }
        }

        /// <summary>
        /// A container struct for all SkinDef parameters.
        /// Use this to set skinDef values, then call CreateNewSkinDef().
        /// </summary>
        public struct SkinDefInfo {
            public SkinDef?[]? BaseSkins;
            public Sprite? Icon;
            public string? NameToken;
            public UnlockableDef? UnlockableDef;
            public GameObject? RootObject;
            public CharacterModel.RendererInfo[]? RendererInfos;
            public SkinDef.MeshReplacement[]? MeshReplacements;
            public SkinDef.GameObjectActivation[]? GameObjectActivations;
            public SkinDef.ProjectileGhostReplacement[]? ProjectileGhostReplacements;
            public SkinDef.MinionSkinReplacement[]? MinionSkinReplacements;
            public string? Name;
        }

        /// <summary>
        /// Creates a new SkinDef from a SkinDefInfo.
        /// Note that this prevents null-refs by disabling SkinDef awake while the SkinDef is being created.
        /// The things that occur during awake are performed when first applied to a character instead.
        /// </summary>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static SkinDef CreateNewSkinDef(SkinDefInfo skin) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            On.RoR2.SkinDef.Awake += DoNothing;

            var newSkin = ScriptableObject.CreateInstance<SkinDef>();

            newSkin.baseSkins = skin.BaseSkins ?? Array.Empty<SkinDef>();
            newSkin.icon = skin.Icon;
            newSkin.unlockableDef = skin.UnlockableDef;
            newSkin.rootObject = skin.RootObject;
            newSkin.rendererInfos = skin.RendererInfos ?? Array.Empty<CharacterModel.RendererInfo>();
            newSkin.gameObjectActivations = skin.GameObjectActivations ?? Array.Empty<SkinDef.GameObjectActivation>();
            newSkin.meshReplacements = skin.MeshReplacements ?? Array.Empty<SkinDef.MeshReplacement>();
            newSkin.projectileGhostReplacements = skin.ProjectileGhostReplacements ?? Array.Empty<SkinDef.ProjectileGhostReplacement>();
            newSkin.minionSkinReplacements = skin.MinionSkinReplacements ?? Array.Empty<SkinDef.MinionSkinReplacement>();
            newSkin.nameToken = skin.NameToken;
            newSkin.name = skin.Name;

            On.RoR2.SkinDef.Awake -= DoNothing;

            AddedSkins.Add(newSkin);
            return newSkin;
        }

        /// <summary>
        /// Adds a skin to the body prefab for a character.
        /// Will attempt to create a default skin if one is not present.
        /// Must be called during plugin Awake or OnEnable. If called afterwards the new skins must be added to bodycatalog manually.
        /// </summary>
        /// <param name="bodyPrefab">The body to add the skin to</param>
        /// <param name="skin">The SkinDefInfo for the skin to add</param>
        /// <returns>True if successful</returns>
        public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDefInfo skin) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            var skinDef = CreateNewSkinDef(skin);
            return AddSkinToCharacter(bodyPrefab, skinDef);
        }

        /// <summary>
        /// Adds a skin to the body prefab for a character.
        /// Will attempt to create a default skin if one is not present.
        /// Must be called during plugin Awake or OnEnable. If called afterwards the new skins must be added to bodycatalog manually.
        /// </summary>
        /// <param name="bodyPrefab">The body to add the skin to</param>
        /// <param name="skin">The SkinDef to add</param>
        /// <returns>True if successful</returns>
        public static bool AddSkinToCharacter(GameObject? bodyPrefab, SkinDef? skin) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(LoadoutAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(LoadoutAPI)})]");
            }
            if (bodyPrefab == null) {
                R2API.Logger.LogError("Tried to add skin to null body prefab.");
                return false;
            }

            if (skin == null) {
                R2API.Logger.LogError("Tried to add invalid skin.");
                return false;
            }
            AddedSkins.Add(skin);

            var modelLocator = bodyPrefab.GetComponent<ModelLocator>();
            if (modelLocator == null) {
                R2API.Logger.LogError("Tried to add skin to invalid body prefab (No ModelLocator).");
                return false;
            }

            var model = modelLocator.modelTransform;
            if (model == null) {
                R2API.Logger.LogError("Tried to add skin to body prefab with no modelTransform.");
                return false;
            }

            if (skin.rootObject != model.gameObject) {
                R2API.Logger.LogError("Tried to add skin with improper root object set.");
                return false;
            }

            var modelSkins = model.GetComponent<ModelSkinController>();
            if (modelSkins == null) {
                R2API.Logger.LogWarning(bodyPrefab.name + " does not have a modelSkinController.\nAdding a new one and attempting to populate the default skin.\nHighly recommended you set the controller up manually.");
                var charModel = model.GetComponent<CharacterModel>();
                if (charModel == null) {
                    R2API.Logger.LogError("Unable to locate CharacterModel, default skin creation aborted.");
                    return false;
                }

                var skinnedRenderer = charModel.mainSkinnedMeshRenderer;
                if (skinnedRenderer == null) {
                    R2API.Logger.LogError("CharacterModel did not contain a main SkinnedMeshRenderer, default skin creation aborted.");
                    return false;
                }

                var baseRenderInfos = charModel.baseRendererInfos;
                if (baseRenderInfos == null || baseRenderInfos.Length == 0) {
                    R2API.Logger.LogError("CharacterModel rendererInfos are invalid, default skin creation aborted.");
                    return false;
                }

                modelSkins = model.gameObject.AddComponent<ModelSkinController>();

                var skinDefInfo = new SkinDefInfo {
                    BaseSkins = Array.Empty<SkinDef>(),
                    GameObjectActivations = Array.Empty<SkinDef.GameObjectActivation>(),
                    Icon = CreateDefaultSkinIcon(),
                    Name = "skin" + bodyPrefab.name + "Default",
                    NameToken = bodyPrefab.name.ToUpper() + "_DEFAULT_SKIN_NAME",
                    RootObject = model.gameObject,
                    UnlockableDef = null,
                    MeshReplacements = new[]
                    {
                        new SkinDef.MeshReplacement {
                            renderer = skinnedRenderer,
                            mesh = skinnedRenderer.sharedMesh
                        }
                    },
                    RendererInfos = charModel.baseRendererInfos,
                    ProjectileGhostReplacements = Array.Empty<SkinDef.ProjectileGhostReplacement>(),
                    MinionSkinReplacements = Array.Empty<SkinDef.MinionSkinReplacement>()
                };

                var defaultSkinDef = CreateNewSkinDef(skinDefInfo);

                modelSkins.skins = new[] {
                    defaultSkinDef
                };
            }

            var skinsArray = modelSkins.skins;
            var index = skinsArray.Length;
            Array.Resize(ref skinsArray, index + 1);
            skinsArray[index] = skin;
            modelSkins.skins = skinsArray;
            return true;
        }

        private static Sprite CreateDefaultSkinIcon() {
            return CreateSkinIcon(Color.red, Color.green, Color.blue, Color.black);
        }

        private static void DoNothing(On.RoR2.SkinDef.orig_Awake orig, SkinDef self) {
            //Intentionally do nothing
        }

        #endregion Adding Skins
    }
}
