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

[ScriptedImporter(1, extension, 1000)]
public class AnimatorDiffImporter : ScriptedImporter
{
    private const string extension = "controllerdiff";
    private const string extensionWithDot = "." + extension;

    public AnimatorController sourceController;
    public AnimatorController modifiedController;

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

    [OnOpenAsset]
    public static bool PreventDiffDoubleClick(int instanceID, int line)
    {
        return Path.GetExtension(AssetDatabase.GetAssetPath(EditorUtility.InstanceIDToObject(instanceID))).ToLower() == extensionWithDot;
    }

    public override void OnImportAsset(AssetImportContext ctx)
    {
        if (!sourceController || !modifiedController)
        {
            return;
        }

        var root = ScriptableObject.CreateInstance<AnimatorDiff>();
        var extraObjects = new Dictionary<ScriptableObject, string>();

        for (var i = 0; i < modifiedController.layers.Length; i++)
        {
            var modifiedLayer = modifiedController.layers[i];
            var sourceLayer = sourceController.layers[i];

            var states = new List<State>();
            var statesTransitions = new Dictionary<string, List<Transition>>();

            foreach (var childState in modifiedLayer.stateMachine.states)
            {
                var state = childState.state;
                var sourceState = sourceLayer.stateMachine.states.FirstOrDefault(s => s.state.name == state.name).state;
                if (sourceState)
                {
                    List<Transition> transitions = null;
                    foreach (var transition in state.transitions)
                    {
                        if (sourceState.transitions.Any(t => t.name == transition.name))
                        {
                            continue;
                        }

                        transitions ??= new List<Transition>();
                        transitions.Add(ConvertTransition(transition));
                    }

                    if (transitions != null)
                    {
                        statesTransitions[state.name] = transitions;
                    }

                    continue;
                }

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

                for (var j = 0; j < state.transitions.Length; j++)
                {
                    var transition = state.transitions[j];
                    newState.Transitions.Add(ConvertTransition(transition));
                }

                states.Add(newState);
            }

            if (states.Count > 0 || statesTransitions.Count > 0)
            {
                var layer = new ExistingLayer();
                root.Layers.Add(layer);
                layer.Name = modifiedLayer.name;
                layer.NewStates.AddRange(states);
                foreach (var (stateName, transitions) in statesTransitions)
                {
                    var state = new ExistingState
                    {
                        Name = stateName,
                    };
                    state.NewTransitions.AddRange(transitions);
                    layer.ExistingStates.Add(state);
                }
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

        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(modifiedController, out var modifiedControllerGuid, out long _))
        {
            ctx.DependsOnSourceAsset(new GUID(modifiedControllerGuid));
        }

        foreach (var (obj, identifier) in extraObjects)
        {
            obj.hideFlags = HideFlags.HideInHierarchy;
            ctx.AddObjectToAsset(identifier, obj);
        }
    }
    private Models.BlendTree ConvertBlendTree(UnityEditor.Animations.BlendTree blendTree, Dictionary<ScriptableObject, string> extraObjects)
    {
        var tree = ScriptableObject.CreateInstance<Models.BlendTree>();
        tree.name = blendTree.name;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(blendTree, out var guid, out long id);
        extraObjects[tree] = $"{guid}_{id}";
        tree.BlendParameter = blendTree.blendParameter;
        tree.BlendParameterY = blendTree.blendParameterY;
        tree.BlendType = (Models.BlendTreeType)blendTree.blendType;
        if (blendTree.children != null)
        {
            tree.Children.AddRange(blendTree.children.Select(c => ConvertChildMotion(c, extraObjects)));
        }

        return tree;
    }

    private Models.ChildMotion ConvertChildMotion(UnityEditor.Animations.ChildMotion childMotion, Dictionary<ScriptableObject, string> extraObjects)
    {
        var motion = new Models.ChildMotion
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

    private Transition ConvertTransition(AnimatorStateTransition transition)
    {
        var newTransition = new Transition
        {
            DestinationStateName = transition.destinationState.name,
            ExitTime = transition.exitTime,
            HasExitTime = transition.hasExitTime,
            HasFixedDuration = transition.hasFixedDuration,
            Offset = transition.offset,
            TransitionDuration = transition.duration,
            InterruptionSource = (InterruptionSource)transition.interruptionSource,
            CanTransitionToSelf = transition.canTransitionToSelf,
            OrderedInterruption = transition.orderedInterruption,
        };

        foreach (var condition in transition.conditions)
        {
            newTransition.Conditions.Add(ConvertCondition(condition));
        }

        return newTransition;
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
