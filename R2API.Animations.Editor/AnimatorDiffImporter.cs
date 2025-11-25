using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using R2API.Models;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace R2API.Animations.Editor;

[ScriptedImporter(2, extension, 1000)]
public class AnimatorDiffImporter : ScriptedImporter
{
    private const string extension = "controllerdiff";
    private const string extensionWithDot = "." + extension;
    private const string copyButtonName = "Copy AnimatorController for modification";
    private const string copyButtonPath = $"Assets/R2API/Animation/{copyButtonName}";

    //Turns out referenced controllers will be added to an AssetBundle on build,
    //even though they are dependencies only in the editor.
    //Keeping the fields to upgrade them to a new system.
    [SerializeField, HideInInspector, Obsolete]
    private AnimatorController sourceController;
    [SerializeField, HideInInspector, Obsolete]
    private AnimatorController modifiedController;

    public string modifiedControllerGuid;

    [MenuItem("Assets/Create/R2API/Animation/AnimatorDiff", false, -1000)]
    public static void CreateDiff()
    {
        var path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        }
        var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/New Animator Diff{extensionWithDot}");

        var endAction = ScriptableObject.CreateInstance<CreateEmptyAsset>();
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, assetPathAndName, null, null);
    }

    [MenuItem(copyButtonPath, true, 19)]
    public static bool ValidateCreateCopyController()
    {
        return Selection.count == 1 && Selection.activeObject is AnimatorController;
    }

    [MenuItem(copyButtonPath, false, 19)]
    public static void CreateCopyController()
    {
        var sourceController = Selection.activeObject as AnimatorController;
        var sourcePath = AssetDatabase.GetAssetPath(sourceController);
        var path = Path.Combine(Path.GetDirectoryName(sourcePath), sourceController.name + "_modified.controller");
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CopyAsset(sourcePath, path);
        var copyController = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        var map = ScriptableObject.CreateInstance<AnimatorMap>();
        map.name = copyController.name + "_map";
        map.sourceController = sourceController;
        map.hideFlags = HideFlags.NotEditable;
        FillMap(map, sourceController, copyController);

        AssetDatabase.AddObjectToAsset(map, copyController);
        AssetDatabase.SaveAssets();

        Undo.RegisterCreatedObjectUndo(copyController, nameof(CreateCopyController));
        Selection.activeObject = copyController;
    }

    private static void FillMap(AnimatorMap map, AnimatorController source, AnimatorController target)
    {
        for (var i = 0; i < source.layers.Length; i++)
        {
            FillMap(map, source.layers[i].stateMachine, target.layers[i].stateMachine);
        }
    }

    private static void FillMap(AnimatorMap map, AnimatorStateMachine source, AnimatorStateMachine target)
    {
        for (var i = 0; i < source.entryTransitions.Length; i++)
        {
            map.sourceObjects.Add(source.entryTransitions[i]);
            map.modifiedObjects.Add(target.entryTransitions[i]);
        }
        for (var i = 0; i < source.anyStateTransitions.Length; i++)
        {
            map.sourceObjects.Add(source.anyStateTransitions[i]);
            map.modifiedObjects.Add(target.anyStateTransitions[i]);
        }
        for (var i = 0; i < source.states.Length; i++)
        {
            FillMap(map, source.states[i].state, target.states[i].state);
        }
        for (var i = 0; i < source.stateMachines.Length; i++)
        {
            FillMap(map, source.stateMachines[i].stateMachine, target.stateMachines[i].stateMachine);
        }
        for (var i = 0; i < source.behaviours.Length; i++)
        {
            map.sourceObjects.Add(source.behaviours[i]);
            map.modifiedObjects.Add(target.behaviours[i]);
        }
    }

    private static void FillMap(AnimatorMap map, AnimatorState source, AnimatorState target)
    {
        for (var i = 0; i < source.transitions.Length; i++)
        {
            map.sourceObjects.Add(source.transitions[i]);
            map.modifiedObjects.Add(target.transitions[i]);
        }
        for (var i = 0; i < source.behaviours.Length; i++)
        {
            map.sourceObjects.Add(source.behaviours[i]);
            map.modifiedObjects.Add(target.behaviours[i]);
        }
    }

    [OnOpenAsset]
    public static bool PreventDiffDoubleClick(int instanceID, int line)
    {
        return Path.GetExtension(AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(instanceID))).ToLower() == extensionWithDot;
    }

#pragma warning disable CS0612 // Type or member is obsolete
    public void OnValidate()
    {
        if (this.modifiedController)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this.modifiedController, out var guid, out long localId))
            {
                modifiedControllerGuid = guid;
                this.modifiedController = null;
            }
        }

        if (!sourceController)
        {
            return;
        }

        var modifiedController = AssetDatabase.LoadMainAssetAtGUID(new GUID(modifiedControllerGuid)) as AnimatorController;
        var map = AssetDatabase.LoadAssetAtPath<AnimatorMap>(AssetDatabase.GetAssetPath(modifiedController));
        if (map)
        {
            sourceController = null;
            return;
        }

        map = ScriptableObject.CreateInstance<AnimatorMap>();
        map.name = modifiedController.name + "_map";
        map.sourceController = sourceController;
        map.hideFlags = HideFlags.NotEditable;
        FillMap(map, sourceController, modifiedController);

        sourceController = null;
        AssetDatabase.AddObjectToAsset(map, modifiedController);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#pragma warning restore CS0612 // Type or member is obsolete

    public override void OnImportAsset(AssetImportContext ctx)
    {
        if (string.IsNullOrEmpty(modifiedControllerGuid))
        {
            return;
        }

        var modifiedController = AssetDatabase.LoadMainAssetAtGUID(new GUID(modifiedControllerGuid)) as AnimatorController;
        if (!modifiedController)
        {
            return;
        }

        var map = AssetDatabase.LoadAssetAtPath<AnimatorMap>(AssetDatabase.GetAssetPath(modifiedController));
        if (!map)
        {
            ctx.LogImportError($"Modified controller is missing AnimatorMap, you should use \"{copyButtonName}\" button to create modified controller from an AnimatorController", modifiedController);
            return;
        }

        var sourceController = map.sourceController;
        if (!sourceController)
        {
            return;
        }

        var stateParents = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
        foreach (var layer in modifiedController.layers)
        {
            if (layer.syncedLayerIndex == -1)
            {
                WriteParents(layer.stateMachine, stateParents);
            }
        }

        var root = ScriptableObject.CreateInstance<AnimatorDiff>();
        var extraObjects = new Dictionary<ScriptableObject, string>();

        for (var i = 0; i < modifiedController.layers.Length; i++)
        {
            var modifiedLayer = modifiedController.layers[i];
            var sourceLayer = sourceController.layers.FirstOrDefault(l => l.name == modifiedLayer.name);
            if (sourceLayer != null)
            {
                var stateMachine = modifiedLayer.syncedLayerIndex != -1 ? null : ConvertExistingStateMachine(modifiedLayer.stateMachine, sourceLayer.stateMachine, map, extraObjects, stateParents);
                var syncedBehaviours = new List<SyncedBehaviour>();
                foreach (var behaviour in modifiedLayer.m_Behaviours)
                {
                    var syncedBehaviour = ConvertSyncedBehaviour(behaviour, map, stateParents);
                    if (syncedBehaviour != null)
                    {
                        syncedBehaviours.Add(syncedBehaviour);
                    }
                }

                if (stateMachine != null
                    || syncedBehaviours.Count > 0)
                {
                    var layer = new ExistingLayer
                    {
                        Name = modifiedLayer.name,
                        StateMachine = stateMachine
                    };
                    layer.NewSyncedBehaviours.AddRangeNotNull(syncedBehaviours);
                    root.Layers.Add(layer);
                }
            }
            else
            {
                var previousLayerName = i == 0 ? null : modifiedController.layers[i - 1].name;
                root.NewLayers.Add(ConvertLayer(modifiedLayer, modifiedController, map, extraObjects, stateParents, previousLayerName));
            }
        }

        foreach (var parameter in modifiedController.parameters)
        {
            if (sourceController.parameters.Any(p => p.name != parameter.name))
            {
                continue;
            }

            root.NewParameters.Add(new Parameter
            {
                Name = parameter.name,
                Type = (ParameterType)parameter.type,
                Value = new ParameterValue
                {
                    IntValue = parameter.defaultInt,
                    BoolValue = parameter.defaultBool,
                    FloatValue = parameter.defaultFloat,
                }
            });
        }

        var fileName = Path.GetFileNameWithoutExtension(ctx.assetPath);
        root.name = fileName;
        ctx.AddObjectToAsset(fileName, root);
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sourceController, out var sourceControllerGuid, out long _))
        {
            ctx.DependsOnSourceAsset(new GUID(sourceControllerGuid));
        }

        ctx.DependsOnSourceAsset(new GUID(modifiedControllerGuid));

        foreach (var (obj, identifier) in extraObjects)
        {
            obj.hideFlags = HideFlags.HideInHierarchy;
            ctx.AddObjectToAsset(identifier, obj);
        }
    }

    private void WriteParents(AnimatorStateMachine stateMachine, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        foreach (var state in stateMachine.states)
        {
            stateParents[state.state] = stateMachine;
        }
        foreach (var subStateMachine in stateMachine.stateMachines)
        {
            stateParents[subStateMachine.stateMachine] = stateMachine;
            WriteParents(subStateMachine.stateMachine, stateParents);
        }
    }

    private Layer ConvertLayer(AnimatorControllerLayer layer, AnimatorController controller, AnimatorMap map, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents, string previousLayerName)
    {
        var newLayer = new Layer
        {
            Name = layer.name,
            PreviousLayerName = previousLayerName,
            AvatarMask = layer.avatarMask,
            BlendingMode = (R2API.Models.AnimatorLayerBlendingMode)layer.blendingMode,
            DefaultWeight = layer.defaultWeight,
            IKPass = layer.iKPass,
            SyncedLayerAffectsTiming = layer.syncedLayerAffectsTiming,
            SyncedLayerName = layer.syncedLayerIndex != -1 ? controller.layers[layer.syncedLayerIndex].name : null,
            StateMachine = layer.syncedLayerIndex == -1 ? ConvertStateMachine(layer.stateMachine, extraObjects, stateParents) : null,
        };

        foreach (var state in layer.m_Motions)
        {
            newLayer.SyncedMotions.Add(ConvertSyncedMotion(state, extraObjects, stateParents));
        }

        foreach (var behaviour in layer.m_Behaviours)
        {
            var syncedBehaviour = ConvertSyncedBehaviour(behaviour, map, stateParents);
            if (syncedBehaviour != null)
            {
                newLayer.SyncedBehaviours.Add(syncedBehaviour);
            }
        }

        return newLayer;
    }

    private SyncedBehaviour ConvertSyncedBehaviour(StateBehavioursPair behaviour, AnimatorMap map, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var behaviours = new List<StateMachineBehaviour>();
        foreach (var beh in behaviour.m_Behaviours)
        {
            if (beh is not StateMachineBehaviour stateMachineBehaviour || map.modifiedObjects.Contains(beh))
            {
                continue;
            }
            behaviours.Add(stateMachineBehaviour);
        }
        if (behaviours.Count == 0)
        {
            return null;
        }

        var newBehaviour = new SyncedBehaviour
        {
            StateName = behaviour.m_State?.name,
            StateMachinePath = GetStateMachinePath(behaviour.m_State, null, stateParents),
        };
        newBehaviour.Behaviours.AddRangeNotNull(behaviours);

        return newBehaviour;
    }

    private SyncedMotion ConvertSyncedMotion(StateMotionPair state, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var newState = new SyncedMotion
        {
            StateName = state.m_State?.name,
            StateMachinePath = GetStateMachinePath(state.m_State, null, stateParents),
        };
        if (state.m_Motion is AnimationClip clip)
        {
            newState.Clip = clip;
        }
        else if (state.m_Motion is UnityEditor.Animations.BlendTree blendTree)
        {
            newState.BlendTree = ConvertBlendTree(blendTree, extraObjects);
        }

        return newState;
    }

    private ExistingStateMachine ConvertExistingStateMachine(AnimatorStateMachine stateMachine, AnimatorStateMachine sourceStateMachine, AnimatorMap map, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var states = new List<State>();
        var existingStates = new List<ExistingState>();
        var anyStateTransitions = new List<Transition>();
        var subStateMachines = new List<StateMachine>();
        var entryTransitions = new List<EntryTransition>();
        var existingSubStateMachines = new List<ExistingStateMachine>();
        var behaviours = new List<StateMachineBehaviour>();

        foreach (var behaviour in stateMachine.behaviours)
        {
            if (map.modifiedObjects.Contains(behaviour))
            {
                continue;
            }

            behaviours.AddNotNull(ConvertBehaviour(behaviour, extraObjects));
        }

        var anySolo = stateMachine.anyStateTransitions.Any(t => t.solo);
        foreach (var anyStateTransition in stateMachine.anyStateTransitions)
        {
            if (map.modifiedObjects.Contains(anyStateTransition))
            {
                continue;
            }

            anyStateTransitions.AddNotNull(ConvertStateTransition(anyStateTransition, stateParents, anySolo));
        }

        foreach (var entryTransition in stateMachine.entryTransitions)
        {
            if (map.modifiedObjects.Contains(entryTransition))
            {
                continue;
            }

            entryTransitions.AddNotNull(ConvertEntryTransition(entryTransition, stateParents));
        }

        foreach (var subStateMachine in stateMachine.stateMachines.Select(t => t.stateMachine))
        {
            var sourceSubStateMachine = sourceStateMachine.stateMachines.Select(t => t.stateMachine).FirstOrDefault(t => t.name == subStateMachine.name);
            if (sourceSubStateMachine)
            {
                var existingSubStateMachine = ConvertExistingStateMachine(subStateMachine, sourceSubStateMachine, map, extraObjects, stateParents);
                if (existingSubStateMachine)
                {
                    existingSubStateMachine.name = subStateMachine.name;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(subStateMachine, out var guid, out long id);
                    extraObjects[existingSubStateMachine] = $"{guid}_{id}";
                    existingSubStateMachines.AddNotNull(existingSubStateMachine);
                }
                continue;
            }

            subStateMachines.AddNotNull(ConvertStateMachine(subStateMachine, extraObjects, stateParents));
        }

        foreach (var childState in stateMachine.states)
        {
            var state = childState.state;
            var sourceState = sourceStateMachine.states.FirstOrDefault(s => s.state.name == state.name).state;
            if (sourceState)
            {
                var existingState = ConvertExistingState(state, map, extraObjects, stateParents);
                if (existingState != null)
                {
                    existingStates.Add(existingState);
                }
            }
            else
            {
                states.Add(ConvertState(state, extraObjects, stateParents));
            }
        }

        if (states.Count > 0
            || existingStates.Count > 0
            || entryTransitions.Count > 0
            || anyStateTransitions.Count > 0
            || existingSubStateMachines.Count > 0
            || subStateMachines.Count > 0
            || behaviours.Count > 0)
        {
            var newStateMachine = ScriptableObject.CreateInstance<ExistingStateMachine>();
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(stateMachine, out var guid, out long id);
            extraObjects[newStateMachine] = $"{guid}_{id}";
            newStateMachine.name = stateMachine.name;
            newStateMachine.NewStates.AddRange(states);
            newStateMachine.NewAnyStateTransitions.AddRange(anyStateTransitions);
            newStateMachine.NewEntryTransitions.AddRange(entryTransitions);
            newStateMachine.SubStateMachines.AddRange(existingSubStateMachines);
            newStateMachine.NewSubStateMachines.AddRange(subStateMachines);
            newStateMachine.NewBehaviours.AddRange(behaviours);
            newStateMachine.States.AddRange(existingStates);

            return newStateMachine;
        }

        return null;
    }

    private StateMachineBehaviour ConvertBehaviour(StateMachineBehaviour behaviour, Dictionary<ScriptableObject, string> extraObjects)
    {
        var newBehaviour = Instantiate(behaviour);
        newBehaviour.name = behaviour.name;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(behaviour, out var guid, out long id);
        extraObjects[newBehaviour] = $"{guid}_{id}";

        return newBehaviour;
    }

    private ExistingState ConvertExistingState(AnimatorState state, AnimatorMap map, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var transitions = new List<Transition>();
        var behaviours = new List<StateMachineBehaviour>();
        var anySolo = state.transitions.Any(t => t.solo);
        foreach (var transition in state.transitions)
        {
            if (map.modifiedObjects.Contains(transition))
            {
                continue;
            }

            transitions.AddNotNull(ConvertStateTransition(transition, stateParents, anySolo));
        }

        foreach (var behaviour in state.behaviours)
        {
            if (map.modifiedObjects.Contains(behaviour))
            {
                continue;
            }

            behaviours.AddNotNull(ConvertBehaviour(behaviour, extraObjects));
        }

        if (transitions.Count > 0
            || behaviours.Count > 0)
        {
            var newState = new ExistingState
            {
                Name = state.name,
            };
            newState.NewTransitions.AddRange(transitions);
            newState.NewBehaviours.AddRange(behaviours);

            return newState;
        }

        return null;
    }

    private State ConvertState(AnimatorState state, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var newState = new State
        {
            Name = state.name,
            IKOnFeet = state.iKOnFeet,
            Tag = state.tag,
            WriteDefaultValues = state.writeDefaultValues
        };

        if (state.motion is AnimationClip clip)
        {
            newState.Clip = clip;
            newState.Loop = clip.isLooping;
        }
        else if (state.motion is UnityEditor.Animations.BlendTree blendTree)
        {
            newState.BlendTree = ConvertBlendTree(blendTree, extraObjects);
            newState.Loop = blendTree.isLooping;
        }
        else
        {
            newState.Loop = true;
        }

        newState.CycleOffset = state.cycleOffset;
        if (state.cycleOffsetParameterActive)
        {
            newState.CycleOffsetParam = state.cycleOffsetParameter;
        }

        newState.Mirror = state.mirror;
        if (state.mirrorParameterActive)
        {
            newState.MirrorParam = state.mirrorParameter;
        }

        newState.Speed = state.speed;
        if (state.speedParameterActive)
        {
            newState.SpeedParam = state.speedParameter;
        }

        if (state.timeParameterActive)
        {
            newState.TimeParam = state.timeParameter;
        }

        var anySolo = state.transitions.Any(t => t.solo);
        for (var i = 0; i < state.transitions.Length; i++)
        {
            var transition = state.transitions[i];
            newState.Transitions.AddNotNull(ConvertStateTransition(transition, stateParents, anySolo));
        }

        for (var i = 0; i < state.behaviours.Length; i++)
        {
            var behaviour = state.behaviours[i];
            newState.Behaviours.AddNotNull(ConvertBehaviour(behaviour, extraObjects));
        }

        return newState;
    }

    private StateMachine ConvertStateMachine(AnimatorStateMachine stateMachine, Dictionary<ScriptableObject, string> extraObjects, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var newStateMachine = ScriptableObject.CreateInstance<StateMachine>();
        newStateMachine.name = stateMachine.name;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(stateMachine, out var guid, out long id);
        extraObjects[newStateMachine] = $"{guid}_{id}";
        newStateMachine.DefaultStateName = stateMachine.defaultState?.name;
        newStateMachine.DefaultStateMachinePath = GetStateMachinePath(stateMachine.defaultState, null, stateParents);
        if (stateMachine.states != null)
        {
            newStateMachine.States.AddRange(stateMachine.states.Select(s => ConvertState(s.state, extraObjects, stateParents)));
        }
        if (stateMachine.stateMachines != null)
        {
            newStateMachine.SubStateMachines.AddRange(stateMachine.stateMachines.Select(s => ConvertStateMachine(s.stateMachine, extraObjects, stateParents)));
        }
        if (stateMachine.anyStateTransitions != null)
        {
            var anySolo = stateMachine.anyStateTransitions.Any(t => t.solo);
            newStateMachine.AnyStateTransitions.AddRangeNotNull(stateMachine.anyStateTransitions.Select(t => ConvertStateTransition(t, stateParents, anySolo)));
        }
        if (stateMachine.entryTransitions != null)
        {
            newStateMachine.EntryTransitions.AddRangeNotNull(stateMachine.entryTransitions.Select(t => ConvertEntryTransition(t, stateParents)));
        }
        if (stateMachine.behaviours != null)
        {
            newStateMachine.Behaviours.AddRangeNotNull(stateMachine.behaviours.Select(t => ConvertBehaviour(t, extraObjects)));
        }

        return newStateMachine;
    }

    private EntryTransition ConvertEntryTransition(AnimatorTransition transition, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        var newTransition = new EntryTransition
        {
            DestinationStateName = transition.destinationState?.name,
        };


        newTransition.DestinationStateMachinePath = GetStateMachinePath(transition.destinationState, transition.destinationStateMachine, stateParents);

        foreach (var condition in transition.conditions)
        {
            newTransition.Conditions.Add(ConvertCondition(condition));
        }

        return newTransition;
    }

    private R2API.Models.BlendTree ConvertBlendTree(UnityEditor.Animations.BlendTree blendTree, Dictionary<ScriptableObject, string> extraObjects)
    {
        var tree = ScriptableObject.CreateInstance<R2API.Models.BlendTree>();
        tree.name = blendTree.name;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(blendTree, out var guid, out long id);
        extraObjects[tree] = $"{guid}_{id}";
        tree.BlendParameter = blendTree.blendParameter;
        tree.BlendParameterY = blendTree.blendParameterY;
        tree.BlendType = (R2API.Models.BlendTreeType)blendTree.blendType;
        if (blendTree.children != null)
        {
            tree.Children.AddRangeNotNull(blendTree.children.Select(c => ConvertChildMotion(c, extraObjects)));
        }

        return tree;
    }

    private R2API.Models.ChildMotion ConvertChildMotion(UnityEditor.Animations.ChildMotion childMotion, Dictionary<ScriptableObject, string> extraObjects)
    {
        var motion = new R2API.Models.ChildMotion
        {
            DirectBlendParameter = childMotion.directBlendParameter,
            Mirror = childMotion.mirror,
            Position = childMotion.position,
            Threshold = childMotion.threshold,
            TimeScale = childMotion.timeScale,
        };

        if (childMotion.motion is AnimationClip clip)
        {
            motion.Clip = clip;
        }
        else if (childMotion.motion is UnityEditor.Animations.BlendTree blendTree)
        {
            motion.BlendTree = ConvertBlendTree(blendTree, extraObjects);
        }

        return motion;
    }

    private Transition ConvertStateTransition(AnimatorStateTransition transition, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents, bool anySolo)
    {
        if (transition.mute || (anySolo && !transition.solo))
        {
            return null;
        }

        var newTransition = new Transition
        {
            DestinationStateName = transition.destinationState?.name,
            ExitTime = transition.exitTime,
            HasExitTime = transition.hasExitTime,
            HasFixedDuration = transition.hasFixedDuration,
            Offset = transition.offset,
            TransitionDuration = transition.duration,
            InterruptionSource = (InterruptionSource)transition.interruptionSource,
            CanTransitionToSelf = transition.canTransitionToSelf,
            OrderedInterruption = transition.orderedInterruption,
            IsExit = transition.isExit,
        };

        newTransition.DestinationStateMachinePath = GetStateMachinePath(transition.destinationState, transition.destinationStateMachine, stateParents);

        foreach (var condition in transition.conditions)
        {
            newTransition.Conditions.Add(ConvertCondition(condition));
        }

        return newTransition;
    }

    private string GetStateMachinePath(AnimatorState state, AnimatorStateMachine stateMachine, Dictionary<UnityEngine.Object, UnityEngine.Object> stateParents)
    {
        UnityEngine.Object parent = null;

        if (state)
        {
            parent = stateParents[state];
        }
        else if (stateMachine)
        {
            parent = stateMachine;
        }

        string destinationPath = null;
        if (parent)
        {
            var path = new List<UnityEngine.Object>();
            do
            {
                path.Add(parent);
                parent = stateParents.GetValueOrDefault(parent);
            }
            while (parent);
            path.Reverse();
            destinationPath = string.Join(".", path.Select(p => p.name));
        }

        return destinationPath;
    }

    private Condition ConvertCondition(AnimatorCondition condition)
    {
        var newCondition = new Condition
        {
            ConditionMode = (ConditionMode)condition.mode,
            ParamName = condition.parameter,
            Value = condition.threshold
        };

        return newCondition;
    }

    private class CreateEmptyAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, "");
            AssetDatabase.ImportAsset(pathName);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pathName);
            CleanUp();
        }
    }
}
