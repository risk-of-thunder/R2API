using R2API.Utils;
using EntityStates;
using MonoMod.RuntimeDetour;
using System;
using RoR2;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Reflection;
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
            set => _loaded = value;
        }

        private static bool _loaded;

        #region Submodule Hooks
        private static readonly HashSet<SkillDef> AddedSkills = new HashSet<SkillDef>();
        private static readonly HashSet<SkillFamily> AddedSkillFamilies = new HashSet<SkillFamily>();
        private static readonly HashSet<SkinDef> AddedSkins = new HashSet<SkinDef>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            if (_detourSet_stateType == null) {
                _detourSet_stateType = new Hook(
                    typeof(SerializableEntityStateType).GetMethodCached("set_stateType"),
                    typeof(LoadoutAPI).GetMethodCached(nameof(Set_stateType_Hook))
                );
            }
            _detourSet_stateType.Apply();
            if (_detourSet_typeName == null) {
                _detourSet_typeName = new Hook(
                    typeof(SerializableEntityStateType).GetMethodCached("set_typeName"),
                    typeof(LoadoutAPI).GetMethodCached(nameof(Set_typeName_Hook))
                );
            }
            _detourSet_typeName.Apply();
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            _detourSet_stateType?.Undo();
            _detourSet_typeName?.Undo();
        }
        #endregion

        #region EntityState fixes
        // ReSharper disable InconsistentNaming
        private static Hook _detourSet_stateType;

        private static Hook _detourSet_typeName;
        // ReSharper restore InconsistentNaming

        private static Assembly Ror2Assembly {
            get {
                if (_ror2Assembly == null) _ror2Assembly = typeof(EntityState).Assembly;
                return _ror2Assembly;
            }
        }
        private static Assembly _ror2Assembly;

        internal static void Set_stateType_Hook(ref SerializableEntityStateType self, Type value) =>
            self.SetStructFieldValue("_typeName",
            IsValidEntityStateType(value)
            ? value.AssemblyQualifiedName
            : "");

        internal static void Set_typeName_Hook(ref SerializableEntityStateType self, string value) =>
            Set_stateType_Hook(ref self, Type.GetType(value) ?? GetTypeAllAssemblies(value));

        private static Type GetTypeAllAssemblies(string name) {
            Type type = Ror2Assembly.GetType(name);
            if (IsValidEntityStateType(type)) return type;

            type = Type.GetType(name);
            if (IsValidEntityStateType(type)) return type;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; ++i) {
                var asm = assemblies[i];
                if (asm == Ror2Assembly) continue;

                type = asm.GetType(name);
                if (IsValidEntityStateType(type)) return type;
            }

            R2API.Logger.LogError(String.Format("No matching entity state type found for name:\n{0}", name));
            return null;
        }

        private static bool IsValidEntityStateType(Type type) {
            return type != null && type.IsSubclassOf(typeof(EntityState)) && !type.IsAbstract;
        }
        #endregion

        #region Adding Skills
        /// <summary>
        /// Adds a type for a skill EntityState to the SkillsCatalog.
        /// State must derive from EntityStates.EntityState.
        /// Note that SkillDefs and SkillFamiles must also be added seperately.
        /// </summary>
        /// <param name="t">The type to add</param>
        /// <returns>True if succesfully added</returns>
        public static bool AddSkill(Type t) {
            if (!Loaded) {
                R2API.Logger.LogError("LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
                return false;
            }
            if (t == null || !t.IsSubclassOf(typeof(EntityState)) || t.IsAbstract) {
                R2API.Logger.LogError("Invalid skill type.");
                return false;
            }
            var stateTab = typeof(EntityState).Assembly.GetType("EntityStates.StateIndexTable");
            var id2State = stateTab.GetFieldValue<Type[]>("stateIndexToType");
            var name2Id = stateTab.GetFieldValue<string[]>("stateIndexToTypeName");
            var state2Id = stateTab.GetFieldValue<Dictionary<Type, short>>("stateTypeToIndex");
            int ogNum = id2State.Length;
            Array.Resize(ref id2State, ogNum + 1);
            Array.Resize(ref name2Id, ogNum + 1);
            id2State[ogNum] = t;
            name2Id[ogNum] = t.AssemblyQualifiedName;
            state2Id[t] = (short)ogNum;
            stateTab.SetFieldValue("stateIndexToType", id2State);
            stateTab.SetFieldValue("stateIndexToTypeName", name2Id);
            stateTab.SetFieldValue("stateTypeToIndex", state2Id);
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillDef to the SkillDefCatalog.
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="s">The SkillDef to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillDef(SkillDef s) {
            if (!Loaded) {
                R2API.Logger.LogError("LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
                return false;
            }
            if (!s) {
                R2API.Logger.LogError("Invalid SkillDef");
                return false;
            }
            AddedSkills.Add(s);
            SkillCatalog.getAdditionalSkillDefs += (list) => {
                list.Add(s);
            };
            return true;
        }

        /// <summary>
        /// Registers an event to add a SkillFamily to the SkillFamiliesCatalog
        /// Must be called before Catalog init (during Awake() or OnEnable())
        /// </summary>
        /// <param name="sf">The skillfamily to add</param>
        /// <returns>True if the event was registered</returns>
        public static bool AddSkillFamily(SkillFamily sf) {
            if (!Loaded) {
                R2API.Logger.LogError("LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
                return false;
            }
            if (!sf) {
                R2API.Logger.LogError("Invalid SkillFamily");
                return false;
            }
            AddedSkillFamilies.Add(sf);
            SkillCatalog.getAdditionalSkillFamilies += (list) => {
                list.Add(sf);
            };
            return true;
        }

        #endregion

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
            public SkinDef[] BaseSkins;
            public Sprite Icon;
            public string NameToken;
            public string UnlockableName;
            public GameObject RootObject;
            public CharacterModel.RendererInfo[] RendererInfos;
            public SkinDef.MeshReplacement[] MeshReplacements;
            public SkinDef.GameObjectActivation[] GameObjectActivations;
            public SkinDef.ProjectileGhostReplacement[] ProjectileGhostReplacements;
            public SkinDef.MinionSkinReplacement[] MinionSkinReplacements;
            public string Name;
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
                R2API.Logger.LogError("LoadoutAPI is not loaded. Please use [R2API.Utils.SubModuleDependency]");
                return null;
            }
            On.RoR2.SkinDef.Awake += DoNothing;

            var newSkin = ScriptableObject.CreateInstance<SkinDef>();

            newSkin.baseSkins = skin.BaseSkins;
            newSkin.icon = skin.Icon;
            newSkin.unlockableName = skin.UnlockableName;
            newSkin.rootObject = skin.RootObject;
            newSkin.rendererInfos = skin.RendererInfos;
            newSkin.gameObjectActivations = skin.GameObjectActivations;
            newSkin.meshReplacements = skin.MeshReplacements;
            newSkin.projectileGhostReplacements = skin.ProjectileGhostReplacements;
            newSkin.minionSkinReplacements = skin.MinionSkinReplacements;
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
        public static bool AddSkinToCharacter(GameObject bodyPrefab, SkinDefInfo skin) {
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
        public static bool AddSkinToCharacter(GameObject bodyPrefab, SkinDef skin) {
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

                var skinnedRenderer = charModel.GetFieldValue<SkinnedMeshRenderer>("mainSkinnedMeshRenderer");
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
                    Name = "skin"+bodyPrefab.name+"Default",
                    NameToken = bodyPrefab.name.ToUpper() + "_DEFAULT_SKIN_NAME",
                    RootObject = model.gameObject,
                    UnlockableName = "",
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
        #endregion
    }
}
