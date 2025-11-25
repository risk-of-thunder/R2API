using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using R2API.Animations.Models.Interfaces;
using R2API.Models;
using UnityEngine;

namespace R2API;

internal partial class ModificationsBundleCreator
{
    /*
    Known edge cases:
    * Adding a new state to a state machine that didn't have any, will result is issues.
        But this is very unlikely because there is no reason to create a state machine without any states.
    */

    private readonly AssetsManager manager;
    private readonly AssetsFileInstance assetFile;
    private readonly long sourceAnimatorControllerPathID;
    private readonly AssetsFileInstance dummyAssetFile;
    private readonly BundleFileInstance dummyBundleFile;
    private readonly long dummyAnimatorControllerPathID;
    private readonly List<AnimatorModifications> modifications;
    private readonly string modifiedBundlePath;
    private readonly List<string> dependencies = [];
    private readonly List<string> newNames = [];
    private readonly Dictionary<uint, string> hashToName = [];
    private readonly Dictionary<string, uint> nameToHash = [];
    private readonly List<Action> delayedActions = [];

    private AssetTypeValueField baseField;
    private AnimatorModifications currentModification;

    public ModificationsBundleCreator(
        AssetsManager manager,
        AssetsFileInstance assetFile,
        long sourceAnimatorControllerPathID,
        AssetsFileInstance dummyAssetFile,
        BundleFileInstance dummyBundleFile,
        long dummyAnimatorControllerPathID,
        List<AnimatorModifications> modifications,
        string modifiedBundlePath)
    {
        this.manager = manager;
        this.assetFile = assetFile;
        this.sourceAnimatorControllerPathID = sourceAnimatorControllerPathID;
        this.dummyAssetFile = dummyAssetFile;
        this.dummyBundleFile = dummyBundleFile;
        this.dummyAnimatorControllerPathID = dummyAnimatorControllerPathID;
        this.modifications = modifications;
        this.modifiedBundlePath = modifiedBundlePath;
    }

    public void Run()
    {
        var controllerAsset = manager.GetExtAsset(assetFile, 0, sourceAnimatorControllerPathID, false);

        baseField = controllerAsset.baseField;

        GatherNames();
        RemapPPtrs();

        AddLayers();
        AddStates();
        AddTransitions();

        //Adding transitions has to be done after all states have been added.
        //There probably is a better way to do it, but all I can think about would
        //require storing all the information for different actions,
        //which I would rather just offload to the compiler by using lambdas
        foreach (var delayedAction in delayedActions)
        {
            delayedAction();
        };

        AddBehaviours();

        AddParameters();
        AddTOS();

        SetExternals();
        SaveBundle();
    }

    /// <summary>
    /// Collecting known names from source controller
    /// </summary>
    private void GatherNames()
    {
        var tos = baseField["m_TOS"]["Array"];
        foreach (var field in tos)
        {
            var hash = field["first"].AsUInt;
            var name = field["second"].AsString;

            hashToName[hash] = name;
            nameToHash[name] = hash;
        }
    }

    /// <summary>
    /// Processing <see cref="AnimatorModifications.Layers"/> and <see cref="AnimatorModifications.NewLayers"/> fields.
    /// </summary>
    private void AddLayers()
    {
        var animationClips = baseField["m_AnimationClips"]["Array"];
        var controller = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;
        var layerArray = controller["m_LayerArray"]["Array"];
        var stateMachineArray = controller["m_StateMachineArray"]["Array"];
        var stateMachineBehaviourVectorDescription = baseField["m_StateMachineBehaviourVectorDescription"];
        var stateMachineBehaviourRangesArray = stateMachineBehaviourVectorDescription["m_StateMachineBehaviourRanges"]["Array"];

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var layer in modification.Layers)
            {
                var layerHash = GetOrAddName(layer.Name);
                var layerField = controller["m_LayerArray"]["Array"].FirstOrDefault(f => f["data"]["m_Binding"].AsUInt == layerHash);
                if (layerField is null)
                {
                    LogError($"Layer \"{layer.Name}\" not found for a controller \"{controllerName}\".");
                    continue;
                }

                if (layer.StateMachine)
                {
                    var stateMachineIndex = layerField["data"]["m_StateMachineIndex"].AsUInt;
                    var stateMachineField = stateMachineArray[(int)stateMachineIndex];
                    ModifyStateMachine(animationClips, layer.StateMachine, stateMachineField, "");
                }
            }

            foreach (var layer in modification.NewLayers.OrderBy(l => string.IsNullOrEmpty(l.SyncedLayerName)))
            {
                var layerField = ValueBuilder.DefaultValueFieldFromTemplate(layerArray.TemplateField.Children[1]);
                var dataField = layerField["data"];
                CreateBodyMaskFromAvatarMask(dataField["m_BodyMask"], layer.AvatarMask);
                var skeletonMaskArrayField = dataField["m_SkeletonMask"]["data"]["m_Data"]["Array"];
                CreateSkeletonMaskFromAvatarMask(skeletonMaskArrayField, layer.AvatarMask);
                var bindingHash = GetOrAddName(layer.Name);
                dataField["m_Binding"].AsUInt = bindingHash;
                dataField["m_DefaultWeight"].AsFloat = layer.DefaultWeight;
                dataField["m_IKPass"].AsBool = layer.IKPass;
                dataField["m_SyncedLayerAffectsTiming"].AsBool = layer.SyncedLayerAffectsTiming;
                dataField["(int&)m_LayerBlendingMode"].AsInt = (int)layer.BlendingMode;

                if (!string.IsNullOrEmpty(layer.SyncedLayerName))
                {
                    var syncedLayerHash = GetOrAddName(layer.SyncedLayerName);
                    var syncedLayer = layerArray.FirstOrDefault(f => f["data"]["m_Binding"].AsUInt == syncedLayerHash);
                    var syncedStateMachineIndex = syncedLayer["data"]["m_StateMachineIndex"].AsUInt;
                    var syncedStateMachine = stateMachineArray[(int)syncedStateMachineIndex];
                    var synchronizedLayerCountField = syncedStateMachine["data"]["m_SynchronizedLayerCount"];
                    
                    dataField["m_StateMachineIndex"].AsUInt = syncedStateMachineIndex;
                    dataField["m_StateMachineSynchronizedLayerIndex"].AsUInt = synchronizedLayerCountField.AsUInt;

                    synchronizedLayerCountField.AsUInt++;
                    var syncedStates = layer.SyncedMotions.ToDictionary(f =>
                    {
                        return GetOrAddName($"{f.StateMachinePath}.{f.StateName}");
                    });

                    foreach (var stateField in syncedStateMachine["data"]["m_StateConstantArray"]["Array"])
                    {
                        var blendTreeIndexArray = stateField["data"]["m_BlendTreeConstantIndexArray"]["Array"];
                        var blendTreeConstantArray = stateField["data"]["m_BlendTreeConstantArray"]["Array"];
                        var stateNameHash = stateField["data"]["m_FullPathID"].AsUInt;
                        var syncedState = syncedStates.GetValueOrDefault(stateNameHash);
                        CreateBlendTreeFromMotion(animationClips, syncedState, blendTreeIndexArray, blendTreeConstantArray);
                    }
                }
                else
                {
                    dataField["m_StateMachineIndex"].AsUInt = CreateStateMachine(layer.StateMachine, animationClips, stateMachineArray);
                    dataField["m_StateMachineSynchronizedLayerIndex"].AsUInt = 0;
                }

                var previousLayerIndex = -1;
                if (!string.IsNullOrEmpty(layer.PreviousLayerName))
                {
                    var previousLayerHash = GetOrAddName(layer.PreviousLayerName);
                    previousLayerIndex = layerArray.IndexOf(f => f["data"]["m_Binding"].AsUInt == previousLayerHash);
                }

                foreach (var range in stateMachineBehaviourRangesArray)
                {
                    var layerIndexField = range["first"]["m_LayerIndex"];
                    if (layerIndexField.AsInt > previousLayerIndex)
                    {
                        layerIndexField.AsInt++;
                    }
                }
                layerArray.Children.Insert(previousLayerIndex + 1, layerField);
            }
        }
        currentModification = null;
    }

    /// <summary>
    /// Applies changes to an existing state machine
    /// </summary>
    /// <param name="animationClips"></param>
    /// <param name="stateMachine"></param>
    /// <param name="stateMachineField"></param>
    /// <param name="stateMachinePath"></param>
    private void ModifyStateMachine(AssetTypeValueField animationClips, IExistingStateMachine stateMachine, AssetTypeValueField stateMachineField, string stateMachinePath)
    {
        var synchronizedLayerCount = stateMachineField["data"]["m_SynchronizedLayerCount"].AsUInt;
        var statesArray = stateMachineField["data"]["m_StateConstantArray"]["Array"];
        var anyStateTransitionsArray = stateMachineField["data"]["m_AnyStateTransitionConstantArray"]["Array"];
        var selectorStatesArray = stateMachineField["data"]["m_SelectorStateConstantArray"]["Array"];

        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";

        delayedActions.Add(() =>
        {
            foreach (var entryTransition in stateMachine.NewEntryTransitions)
            {
                CreateSelectorTransition(newStateMachinePath, entryTransition, statesArray, selectorStatesArray);
            }
            foreach (var anyStateTransition in stateMachine.NewAnyStateTransitions)
            {
                CreateTransition(newStateMachinePath, "AnyState", anyStateTransition, anyStateTransitionsArray, statesArray, selectorStatesArray, "Entry");
            }
        });

        foreach (var state in stateMachine.States)
        {
            ModifyState(newStateMachinePath, statesArray, selectorStatesArray, state);
        }
        foreach (var state in stateMachine.NewStates)
        {
            CreateState(animationClips, newStateMachinePath, stateMachine.Name, synchronizedLayerCount, statesArray, selectorStatesArray, state);
        }
        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            ModifyStateMachine(animationClips, subStateMachine, stateMachineField, newStateMachinePath);
        }
        foreach (var subStateMachine in stateMachine.NewSubStateMachines)
        {
            CreateSubStateMachine(newStateMachinePath, subStateMachine, stateMachineField, animationClips);
        }
    }

    /// <summary>
    /// Creates new sub StateMachine
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="stateMachine"></param>
    /// <param name="stateMachineField"></param>
    /// <param name="animationClips"></param>
    private void CreateSubStateMachine(string stateMachinePath, IStateMachine stateMachine, AssetTypeValueField stateMachineField, AssetTypeValueField animationClips)
    {
        var synchronizedLayerCount = stateMachineField["data"]["m_SynchronizedLayerCount"].AsUInt;
        var statesArray = stateMachineField["data"]["m_StateConstantArray"]["Array"];
        var anyStateTransitionsArray = stateMachineField["data"]["m_AnyStateTransitionConstantArray"]["Array"];
        var selectorStatesArray = stateMachineField["data"]["m_SelectorStateConstantArray"]["Array"];

        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";
        var newStateMachinePathHash = GetOrAddName(newStateMachinePath);

        var entrySelectorState = CreateSelectorState(newStateMachinePathHash, selectorStatesArray, true);
        var exitSelectorState = CreateSelectorState(newStateMachinePathHash, selectorStatesArray, false);
        delayedActions.Add(() =>
        {
            if (string.IsNullOrEmpty(stateMachine.Name) && string.IsNullOrEmpty(stateMachine.DefaultStateMachinePath))
            {
                CreateSelectorTransition(uint.MaxValue, exitSelectorState);
            }
            else
            {
                CreateSelectorTransition(newStateMachinePath, new EntryTransition { DestinationStateName = stateMachine.DefaultStateName, DestinationStateMachinePath = stateMachine.DefaultStateMachinePath }, statesArray, selectorStatesArray);
            }

            if (string.IsNullOrEmpty(stateMachinePath))
            {
                CreateSelectorTransition(30000, exitSelectorState);
            }
            else
            {
                var parentStateMachinePathHash = GetOrAddName(stateMachinePath);
                var parentEntryState = 30000u;
                for (var i = 0; i < selectorStatesArray.Children.Count; i++)
                {
                    var data = selectorStatesArray[i]["data"];
                    if (data["m_FullPathID"].AsUInt == parentStateMachinePathHash
                        && data["m_IsEntry"].AsBool == true)
                    {
                        parentEntryState = data["m_TransitionConstantArray"]["Array"][0]["data"]["m_Destination"].AsUInt;
                        break;
                    }
                }

                CreateSelectorTransition(parentEntryState, exitSelectorState);
            }

            foreach (var entryTransition in stateMachine.EntryTransitions)
            {
                CreateSelectorTransition(newStateMachinePath, entryTransition, statesArray, selectorStatesArray);
            }
            foreach (var anyStateTransition in stateMachine.AnyStateTransitions)
            {
                CreateTransition(newStateMachinePath, "AnyState", anyStateTransition, anyStateTransitionsArray, statesArray, selectorStatesArray, "Entry");
            }
        });

        foreach (var state in stateMachine.States)
        {
            CreateState(animationClips, newStateMachinePath, stateMachine.Name, synchronizedLayerCount, statesArray, selectorStatesArray, state);
        }
        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            CreateSubStateMachine(newStateMachinePath, subStateMachine, stateMachineField, selectorStatesArray);
        }
    }

    /// <summary>
    /// Creates a SelectorState for a StateMachine
    /// </summary>
    /// <param name="stateMachinePathHash"></param>
    /// <param name="selectorStatesArray"></param>
    /// <param name="isEntry"></param>
    /// <returns></returns>
    private AssetTypeValueField CreateSelectorState(uint stateMachinePathHash, AssetTypeValueField selectorStatesArray, bool isEntry)
    {
        var selectorStateField = ValueBuilder.DefaultValueFieldFromTemplate(selectorStatesArray.TemplateField.Children[1]);
        selectorStatesArray.Children.Add(selectorStateField);
        var selectorStateDataField = selectorStateField["data"];
        selectorStateDataField["m_FullPathID"].AsUInt = stateMachinePathHash;
        selectorStateDataField["m_IsEntry"].AsBool = isEntry;

        return selectorStateField;
    }

    /// <summary>
    /// Creates a SelectorState Transition for a StateMachine
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="entryTransition"></param>
    /// <param name="statesArray"></param>
    /// <param name="selectorStatesArray"></param>
    private void CreateSelectorTransition(string stateMachinePath, ISimpleTransition entryTransition, AssetTypeValueField statesArray, AssetTypeValueField selectorStatesArray)
    {
        var destinationStateMachinePath = string.IsNullOrEmpty(entryTransition.DestinationStateMachinePath) ? stateMachinePath : entryTransition.DestinationStateMachinePath;
        var destinationStateFullPathName = $"{destinationStateMachinePath}.{entryTransition.DestinationStateName}";
        var destinationStateFullPathHash = GetOrAddName(destinationStateFullPathName);
        var transitionFullPathHash = GetOrAddName(stateMachinePath);

        var destinationState = GetTransitionDestinationState(entryTransition.DestinationStateName, statesArray, selectorStatesArray, destinationStateFullPathHash);
        if (destinationState == uint.MaxValue)
        {
            LogError($"Destination state by path \"{destinationStateFullPathName}\" was not found.");
            return;
        }

        var selectorState = selectorStatesArray.FirstOrDefault(s => s["data"]["m_FullPathID"].AsUInt == transitionFullPathHash && s["data"]["m_IsEntry"].AsBool);
        if (selectorState is null)
        {
            LogError($"StateMachine \"{stateMachinePath}\" was not found.");
            return;
        }

        var transitionField = CreateSelectorTransition(destinationState, selectorState);

        var conditions = transitionField["data"]["m_ConditionConstantArray"]["Array"];
        foreach (var condition in entryTransition.Conditions)
        {
            var parameterHash = GetOrAddName(condition.ParamName);
            var conditionField = ValueBuilder.DefaultValueFieldFromTemplate(conditions.TemplateField.Children[1]);
            var conditionFieldData = conditionField["data"];
            conditionFieldData["m_ConditionMode"].AsUInt = (uint)condition.ConditionMode;
            conditionFieldData["m_EventID"].AsUInt = parameterHash;
            conditionFieldData["m_EventThreshold"].AsFloat = condition.Value;
            conditionFieldData["m_ExitTime"].AsFloat = 0f;
            conditions.Children.Add(conditionField);
        }
    }

    /// <summary>
    /// Creates a SelectorState Transition for a StateMachine
    /// </summary>
    /// <param name="destinationState"></param>
    /// <param name="selectorState"></param>
    /// <returns></returns>
    private AssetTypeValueField CreateSelectorTransition(uint destinationState, AssetTypeValueField selectorState)
    {
        var transitions = selectorState["data"]["m_TransitionConstantArray"]["Array"];
        var transitionField = ValueBuilder.DefaultValueFieldFromTemplate(transitions.TemplateField.Children[1]);
        var transitionDataField = transitionField["data"];
        transitionDataField["m_Destination"].AsUInt = destinationState;
        transitions.Children.Add(transitionField);

        return transitionField;
    }

    /// <summary>
    /// Applies changes to an existing State
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="statesArray"></param>
    /// <param name="selectorStatesArray"></param>
    /// <param name="state"></param>
    private void ModifyState(string stateMachinePath, AssetTypeValueField statesArray, AssetTypeValueField selectorStatesArray, IExistingState state)
    {
        var fullPath = $"{stateMachinePath}.{state.Name}";
        var fullPathID = GetOrAddName(fullPath);
        var stateField = statesArray.FirstOrDefault(s => s["data"]["m_FullPathID"].AsUInt == fullPathID);
        if (stateField is null)
        {
            LogError($"State \"{fullPath}\" was not found.");
            return;
        }

        delayedActions.Add(() =>
        {
            var transitionsArrayField = stateField["data"]["m_TransitionConstantArray"]["Array"];
            foreach (var transition in state.NewTransitions)
            {
                CreateTransition(stateMachinePath, state.Name, transition, transitionsArrayField, statesArray, selectorStatesArray);
            }
        });
    }

    /// <summary>
    /// Creates a new StateMachine
    /// </summary>
    /// <param name="stateMachine"></param>
    /// <param name="animationClips"></param>
    /// <param name="stateMachineArray"></param>
    /// <returns></returns>
    private uint CreateStateMachine(IStateMachine stateMachine, AssetTypeValueField animationClips, AssetTypeValueField stateMachineArray)
    {
        var stateMachineField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineArray.TemplateField.Children[1]);
        var index = stateMachineArray.Children.Count;
        stateMachineArray.Children.Add(stateMachineField);
        var stateMachineDataField = stateMachineField["data"];
        var statesArray = stateMachineDataField["m_StateConstantArray"]["Array"];
        var selectorStatesArray = stateMachineDataField["m_SelectorStateConstantArray"]["Array"];
        stateMachineDataField["m_SynchronizedLayerCount"].AsUInt = 1u;
        CreateSubStateMachine("", stateMachine, stateMachineField, animationClips);
        if (string.IsNullOrEmpty(stateMachine.DefaultStateName))
        {
            var stateMachinePath = string.IsNullOrEmpty(stateMachine.DefaultStateMachinePath) ? stateMachine.Name : stateMachine.DefaultStateMachinePath;
            stateMachineDataField["m_DefaultState"].AsUInt = GetTransitionDestinationState(stateMachine.DefaultStateName, statesArray, selectorStatesArray, GetOrAddName(stateMachinePath));
        }
        else
        {
            stateMachineDataField["m_DefaultState"].AsUInt = 0;
        }

        return (uint)index;
    }

    /// <summary>
    /// Creates SkeletonMask from AvatarMask
    /// </summary>
    /// <param name="skeletonMaskArrayField"></param>
    /// <param name="avatarMask"></param>
    private void CreateSkeletonMaskFromAvatarMask(AssetTypeValueField skeletonMaskArrayField, AvatarMask avatarMask)
    {
        if (!avatarMask)
        {
            return;
        }

        var count = avatarMask.transformCount;
        for (var i = 0; i < count; i++)
        {
            var skeletonMaskField = ValueBuilder.DefaultValueFieldFromTemplate(skeletonMaskArrayField.TemplateField.Children[1]);
            skeletonMaskArrayField.Children.Add(skeletonMaskField);
            skeletonMaskField["m_PathHash"].AsUInt = GetOrAddName(avatarMask.GetTransformPath(i));
            skeletonMaskField["m_Weight"].AsFloat = avatarMask.GetTransformWeight(i);
        }
    }

    /// <summary>
    /// Creates Humanoid body mask from AvatarMask
    /// </summary>
    /// <param name="bodyMask"></param>
    /// <param name="avatarMask"></param>
    private void CreateBodyMaskFromAvatarMask(AssetTypeValueField bodyMask, AvatarMask avatarMask)
    {
        var word0 = 0u;
        var word1 = 0u;
        var word2 = 0u;

        //There is no available function to convert avatar mask to humanoid body mask.
        //These values are from building with individual body parts enabled.
        if (avatarMask)
        {
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Root))
            {
                word0 |= 1;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Body))
            {
                word0 |= 1022u;
                word1 |= 3221225472u;
                word2 |= 1u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.Head))
            {
                word0 |= 4193280u;
                word2 |= 6u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg))
            {
                word0 |= 1069547520u;
                word2 |= 120u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg))
            {
                word0 |= 3221225472u;
                word1 |= 63u;
                word2 |= 1920u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm))
            {
                word1 |= 32704u;
                word2 |= 30720u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm))
            {
                word1 |= 16744448u;
                word2 |= 491520u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers))
            {
                word1 |= 268435456u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers))
            {
                word1 |= 536870912u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK))
            {
                word1 |= 16777216u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK))
            {
                word1 |= 33554432u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK))
            {
                word1 |= 67108864u;
            }
            if (avatarMask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK))
            {
                word1 |= 134217728u;
            }
        }
        else
        {
            word0 = uint.MaxValue;
            word1 = uint.MaxValue;
            word2 = 524287u;
        }

        bodyMask["word0"].Value.AsUInt = word0;
        bodyMask["word1"].Value.AsUInt = word1;
        bodyMask["word2"].Value.AsUInt = word2;
    }

    /// <summary>
    /// Processing <see cref="AnimatorModifications.NewParameters"/> field.
    /// </summary>
    private void AddParameters()
    {
        var existingParameters = new HashSet<uint>();
        var mController = baseField["m_Controller"];
        var values = mController["m_Values"]["data"]["m_ValueArray"]["Array"];
        var defaultValues = mController["m_DefaultValues"]["data"];

        var boolValues = defaultValues["m_BoolValues"]["Array"];
        var floatValues = defaultValues["m_FloatValues"]["Array"];
        var intValues = defaultValues["m_IntValues"]["Array"];

        foreach (var value in values)
        {
            existingParameters.Add(value["m_ID"].AsUInt);
        }

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var parameter in modification.NewParameters)
            {
                var id = GetOrAddName(parameter.Name);
                if (!existingParameters.Add(id))
                {
                    LogError($"Parameter {parameter.Name} already exists for {baseField["m_Name"].AsString}.");
                    continue;
                }

                int index;
                switch (parameter.Type)
                {
                    case ParameterType.Float:
                    {
                        index = floatValues.Children.Count;

                        var floatValueField = ValueBuilder.DefaultValueFieldFromTemplate(floatValues.TemplateField.Children[1]);
                        floatValueField.AsFloat = parameter.Value;
                        floatValues.Children.Add(floatValueField);
                        break;
                    }
                    case ParameterType.Int:
                    {
                        index = intValues.Children.Count;

                        var intValueField = ValueBuilder.DefaultValueFieldFromTemplate(intValues.TemplateField.Children[1]);
                        intValueField.AsInt = parameter.Value;
                        intValues.Children.Add(intValueField);
                        break;
                    }
                    case ParameterType.Trigger:
                    case ParameterType.Bool:
                    {
                        index = boolValues.Children.Count;

                        var boolValueField = ValueBuilder.DefaultValueFieldFromTemplate(boolValues.TemplateField.Children[1]);
                        boolValueField.AsBool = parameter.Value;
                        boolValues.Children.Add(boolValueField);
                        break;
                    }
                    default:
                        LogError($"Not supported parameter type {parameter.Type} for parameter \"{parameter.Name}\".");
                        continue;
                }

                var valueField = ValueBuilder.DefaultValueFieldFromTemplate(values.TemplateField.Children[1]);
                valueField["m_ID"].AsUInt = id;
                valueField["m_Type"].AsUInt = (uint)parameter.Type;
                valueField["m_Index"].AsUInt = (uint)index;
                values.Children.Add(valueField);
            }
        }
        currentModification = null;
    }

    /// <summary>
    /// Processing <see cref="AnimatorModifications.NewStates"/> field.
    /// </summary>
    private void AddStates()
    {
        var animationClips = baseField["m_AnimationClips"]["Array"];
        var mController = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var (layerName, states) in modification.NewStates)
            {
                var layerHash = GetOrAddName(layerName);
                var layer = mController["m_LayerArray"]["Array"].FirstOrDefault(f => f["data"]["m_Binding"].AsUInt == layerHash);
                if (layer is null)
                {
                    LogError($"Layer \"{layerName}\" not found for a controller \"{controllerName}\".");
                    continue;
                }

                var stateMachineIndex = layer["data"]["m_StateMachineIndex"].AsUInt;
                var stateMachine = mController["m_StateMachineArray"]["Array"][(int)stateMachineIndex];
                var synchronizedLayerCount = stateMachine["data"]["m_SynchronizedLayerCount"].AsUInt;
                var statesArray = stateMachine["data"]["m_StateConstantArray"]["Array"];
                var selectorStatesArray = stateMachine["data"]["m_SelectorStateConstantArray"]["Array"];

                foreach (var state in states)
                {
                    CreateState(animationClips, layerName, layerName, synchronizedLayerCount, statesArray, selectorStatesArray, state);
                }
            }
        }
        currentModification = null;
    }

    /// <summary>
    /// Creates a new State for a StateMachine
    /// </summary>
    /// <param name="animationClips"></param>
    /// <param name="stateMachinePath"></param>
    /// <param name="stateMachineName"></param>
    /// <param name="synchronizedLayerCount"></param>
    /// <param name="statesArray"></param>
    /// <param name="selectorStatesArray"></param>
    /// <param name="state"></param>
    private void CreateState(AssetTypeValueField animationClips, string stateMachinePath, string stateMachineName, uint synchronizedLayerCount, AssetTypeValueField statesArray, AssetTypeValueField selectorStatesArray, IState state)
    {
        var stateHash = GetOrAddName(state.Name);
        var statePathName = $"{stateMachineName}.{state.Name}";
        var statePathHash = GetOrAddName(statePathName);
        var stateFullPathName = $"{stateMachinePath}.{state.Name}";
        var stateFullPathHash = GetOrAddName(stateFullPathName);
        var tagHash = GetOrAddName(state.Tag);
        var speedHash = GetOrAddName(state.SpeedParam);
        var mirrorHash = GetOrAddName(state.MirrorParam);
        var cycleOffsetHash = GetOrAddName(state.CycleOffsetParam);
        var timeHash = GetOrAddName(state.TimeParam);

        var stateField = ValueBuilder.DefaultValueFieldFromTemplate(statesArray.TemplateField.Children[1]);
        var stateDataField = stateField["data"];
        stateDataField["m_NameID"].AsUInt = stateHash;
        stateDataField["m_PathID"].AsUInt = statePathHash;
        stateDataField["m_FullPathID"].AsUInt = stateFullPathHash;
        stateDataField["m_TagID"].AsUInt = tagHash;
        stateDataField["m_SpeedParamID"].AsUInt = speedHash;
        stateDataField["m_MirrorParamID"].AsUInt = mirrorHash;
        stateDataField["m_CycleOffsetParamID"].AsUInt = cycleOffsetHash;
        stateDataField["m_TimeParamID"].AsUInt = timeHash;
        stateDataField["m_Speed"].AsFloat = state.Speed;
        stateDataField["m_CycleOffset"].AsFloat = state.CycleOffset;
        stateDataField["m_IKOnFeet"].AsBool = state.IKOnFeet;
        stateDataField["m_WriteDefaultValues"].AsBool = state.WriteDefaultValues;
        stateDataField["m_Loop"].AsBool = state.Loop;
        stateDataField["m_Mirror"].AsBool = state.Mirror;
        statesArray.Children.Add(stateField);

        delayedActions.Add(() =>
        {
            var transitions = stateField["data"]["m_TransitionConstantArray"]["Array"];
            foreach (var transition in state.Transitions)
            {
                CreateTransition(stateMachinePath, state.Name, transition, transitions, statesArray, selectorStatesArray);
            }
        });

        var blendTreeIndexArray = stateField["data"]["m_BlendTreeConstantIndexArray"]["Array"];
        var blendTreeConstantArray = stateField["data"]["m_BlendTreeConstantArray"]["Array"];
        CreateBlendTreeFromMotion(animationClips, state, blendTreeIndexArray, blendTreeConstantArray);

        for (var i = 1; i < synchronizedLayerCount; i++)
        {
            CreateBlendTreeFromMotion(animationClips, null, blendTreeIndexArray, blendTreeConstantArray);
        }
    }

    /// <summary>
    /// Adds dependency and returns it's reference
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="fileID"></param>
    /// <param name="pathID"></param>
    /// <returns></returns>
    private bool TryAddDependency(UnityEngine.Object obj, out int fileID, out long pathID)
    {
        pathID = NativeHelpers.GetAssetPathID(obj);
        fileID = 0;
        if (pathID == 0)
        {
            return false;
        }

        var depPath = NativeHelpers.GetPathName(obj);
        if (string.IsNullOrEmpty(depPath))
        {
            return false;
        }

        fileID = dependencies.IndexOf(depPath);
        if (fileID == -1)
        {
            fileID = dependencies.Count;
            dependencies.Add(depPath);
        }
        fileID++;

        return true;
    }

    /// <summary>
    /// Processing <see cref="AnimatorModifications.NewTransitions"/> field.
    /// </summary>
    private void AddTransitions()
    {
        var mController = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var ((layerName, stateName), transitions) in modification.NewTransitions)
            {
                var layerHash = GetOrAddName(layerName);
                var stateHash = GetOrAddName(stateName);
                var layer = mController["m_LayerArray"]["Array"].FirstOrDefault(f => f["data"]["m_Binding"].AsUInt == layerHash);
                if (layer is null)
                {
                    LogError($"Layer \"{layerName}\" not found for controller {controllerName}.");
                    continue;
                }

                var stateMachineIndex = layer["data"]["m_StateMachineIndex"].AsUInt;

                var stateMachine = mController["m_StateMachineArray"]["Array"][(int)stateMachineIndex];
                var states = stateMachine["data"]["m_StateConstantArray"]["Array"];
                var selectorStates = stateMachine["data"]["m_SelectorStateConstantArray"]["Array"];
                var stateField = states.FirstOrDefault(f => f["data"]["m_NameID"].AsUInt == stateHash);
                if (stateField is null)
                {
                    LogError($"State \"{stateName}\" not found for a layer \"{layerName}\" for a controller \"{controllerName}\".");
                    continue;
                }

                delayedActions.Add(() =>
                {
                    var transitionsArray = stateField["data"]["m_TransitionConstantArray"]["Array"];
                    foreach (var transition in transitions)
                    {
                        CreateTransition(layerName, stateName, transition, transitionsArray, states, selectorStates);
                    }
                });
            }
        }
        currentModification = null;
    }

    /// <summary>
    /// Creates a Transition from a State
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="stateName"></param>
    /// <param name="transition"></param>
    /// <param name="transitions"></param>
    /// <param name="states"></param>
    /// <param name="selectorStates"></param>
    /// <param name="fullPathOverride"></param>
    private void CreateTransition(string stateMachinePath, string stateName, ITransition transition, AssetTypeValueField transitions, AssetTypeValueField states, AssetTypeValueField selectorStates, string fullPathOverride = null)
    {
        var stateFullPathName = fullPathOverride ?? $"{stateMachinePath}.{stateName}";
        var destinationStateMachinePath = string.IsNullOrEmpty(transition.DestinationStateMachinePath) ? stateMachinePath : transition.DestinationStateMachinePath;

        var destinationName = transition.DestinationStateName;
        if (string.IsNullOrEmpty(destinationName))
        {
            var dotIndex = destinationStateMachinePath.LastIndexOf('.');
            if (dotIndex == -1)
            {
                destinationName = destinationStateMachinePath;
            }
            else
            {
                destinationName = destinationStateMachinePath[(dotIndex + 1)..];
            }
        }

        var destinationStateFullPathName = string.IsNullOrEmpty(transition.DestinationStateName) ? destinationStateMachinePath : $"{destinationStateMachinePath}.{transition.DestinationStateName}";
        var destinationStateFullPathHash = GetOrAddName(destinationStateFullPathName);

        var destinationState = GetTransitionDestinationState(transition.DestinationStateName, states, selectorStates, destinationStateFullPathHash, transition.IsExit);
        if (destinationState == uint.MaxValue)
        {
            LogError($"Destination state by path \"{destinationStateFullPathName}\" was not found.");
            return;
        }

        var transitionName = $"{stateName} -> {(transition.IsExit ? "Exit" : destinationName)}";
        var transitionHash = GetOrAddName(transitionName);

        var transitionFullPathName = $"{stateFullPathName} -> {(destinationState >= 30000 ? "Exit" : destinationStateFullPathName)}";
        var transitionFullPathHash = GetOrAddName(transitionFullPathName);

        var transitionField = ValueBuilder.DefaultValueFieldFromTemplate(transitions.TemplateField.Children[1]);
        var transitionDataField = transitionField["data"];
        transitionDataField["m_DestinationState"].AsUInt = (uint)destinationState;
        transitionDataField["m_FullPathID"].AsUInt = transitionFullPathHash;
        transitionDataField["m_ID"].AsUInt = transitionHash;
        transitionDataField["m_UserID"].AsUInt = 0u;
        transitionDataField["m_TransitionOffset"].AsFloat = transition.Offset;
        transitionDataField["m_TransitionDuration"].AsFloat = transition.TransitionDuration;
        transitionDataField["m_HasFixedDuration"].AsBool = transition.HasFixedDuration;
        transitionDataField["m_HasExitTime"].AsBool = transition.HasExitTime;
        transitionDataField["m_ExitTime"].AsFloat = transition.ExitTime;
        transitionDataField["m_InterruptionSource"].AsInt = (int)transition.InterruptionSource;
        transitionDataField["m_CanTransitionToSelf"].AsBool = transition.CanTransitionToSelf;
        transitionDataField["m_OrderedInterruption"].AsBool = transition.OrderedInterruption;
        transitions.Children.Add(transitionField);

        var conditions = transitionDataField["m_ConditionConstantArray"]["Array"];
        foreach (var condition in transition.Conditions)
        {
            var parameterHash = GetOrAddName(condition.ParamName);
            var conditionField = ValueBuilder.DefaultValueFieldFromTemplate(conditions.TemplateField.Children[1]);
            var conditionFieldData = conditionField["data"];
            conditionFieldData["m_ConditionMode"].AsUInt = (uint)condition.ConditionMode;
            conditionFieldData["m_EventID"].AsUInt = parameterHash;
            conditionFieldData["m_EventThreshold"].AsFloat = condition.Value;
            conditionFieldData["m_ExitTime"].AsFloat = 0f;
            conditions.Children.Add(conditionField);
        }
    }

    /// <summary>
    /// Finds correct destination index for Transition
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="states"></param>
    /// <param name="selectorStates"></param>
    /// <param name="stateFullPathHash"></param>
    /// <param name="isExit"></param>
    /// <returns></returns>
    private static uint GetTransitionDestinationState(string stateName, AssetTypeValueField states, AssetTypeValueField selectorStates, uint stateFullPathHash, bool isExit = false)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            for (var i = 0; i < selectorStates.Children.Count; i++)
            {
                var data = selectorStates[i]["data"];
                if (data["m_FullPathID"].AsUInt == stateFullPathHash
                    && data["m_IsEntry"].AsBool != isExit)
                {
                    return (uint)(i + 30000);
                }
            }
        }
        else
        {
            for (var i = 0; i < states.Children.Count; i++)
            {
                if (states[i]["data"]["m_FullPathID"].AsUInt == stateFullPathHash)
                {
                    return (uint)i;
                }
            }
        }

        return uint.MaxValue;
    }

    /// <summary>
    /// Adds new names to TOS array
    /// </summary>
    private void AddTOS()
    {
        var tos = baseField["m_TOS"]["Array"];
        foreach (var name in newNames)
        {
            var hash = nameToHash[name];
            var tosField = ValueBuilder.DefaultValueFieldFromTemplate(tos.TemplateField.Children[1]);
            tosField["first"].AsUInt = hash;
            tosField["second"].AsString = name;
            tos.Children.Add(tosField);
        }
    }

    /// <summary>
    /// Saves new AssetBundle with modified controller
    /// </summary>
    private void SaveBundle()
    {
        var dummyControllerAsset = manager.GetExtAsset(dummyAssetFile, 0, dummyAnimatorControllerPathID, false);
        baseField["m_Name"].AsString = $"{baseField["m_Name"].AsString} (Modified)";
        dummyControllerAsset.info.SetNewData(baseField);

        var directoryInfo = dummyBundleFile.file.BlockAndDirInfo.DirectoryInfos[0];
        directoryInfo.SetNewData(dummyAssetFile.file);
        directoryInfo.Name = Path.GetFileNameWithoutExtension(modifiedBundlePath);

        var assetBundleAsset = manager.GetExtAsset(dummyAssetFile, 0, 1, false);
        var assetBundleBaseField = assetBundleAsset.baseField;
        assetBundleBaseField["m_Name"].AsString = directoryInfo.Name;
        assetBundleAsset.info.SetNewData(assetBundleBaseField);

        var directoryPath = Path.GetDirectoryName(modifiedBundlePath);
        Directory.CreateDirectory(directoryPath);

        using (var newFile = File.Open(modifiedBundlePath, FileMode.Create, FileAccess.Write))
        using (var writer = new AssetsFileWriter(newFile))
        {
            dummyBundleFile.file.Write(writer);
        }
    }

    /// <summary>
    /// Adds AssetBundle dependencies to metadata
    /// </summary>
    private void SetExternals()
    {
        var externals = dummyAssetFile.file.Metadata.Externals;
        foreach (var dependency in dependencies)
        {
            externals.Add(
                new AssetsFileExternal
                {
                    PathName = dependency,
                    Type = AssetsFileExternalType.Normal,
                    VirtualAssetPathName = ""
                });
        }
    }

    /// <summary>
    /// Increments FileID for all references 
    /// </summary>
    private void RemapPPtrs()
    {
        var fields = new List<AssetTypeValueField>();
        GatherPPtrFileIDFields(baseField, fields);

        var dependencyIndices = new HashSet<int>();
        foreach (var field in fields)
        {
            dependencyIndices.Add(field.AsInt);
        }

        var newOrder = 1;
        var remaps = new Dictionary<int, int>();

        foreach (var index in dependencyIndices.OrderBy(d => d))
        {
            remaps[index] = newOrder++;
            if (index == 0)
            {
                var mainDepPath = $"archive:/{assetFile.name}/{assetFile.name}";
                dependencies.Add(mainDepPath);
            }
            else
            {
                dependencies.Add(assetFile.file.Metadata.Externals[index - 1].PathName);
            }
        }

        foreach (var field in fields)
        {
            field.AsInt = remaps[field.AsInt];
        }
    }

    /// <summary>
    /// Collects all reference fields
    /// </summary>
    /// <param name="field"></param>
    /// <param name="fields"></param>
    private void GatherPPtrFileIDFields(AssetTypeValueField field, List<AssetTypeValueField> fields)
    {
        if (field.TypeName.StartsWith("PPtr<"))
        {
            var fileIDField = field["m_FileID"];
            fields.Add(fileIDField);
            return;
        }

        foreach (var child in field.Children)
        {
            GatherPPtrFileIDFields(child, fields);
        }
    }

    /// <summary>
    /// Returns hash for a name and stores it if it wasn't already stored.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private uint GetOrAddName(string name)
    {
        if (name is null)
        {
            return 0;
        }

        if (nameToHash.TryGetValue(name, out var hash))
        {
            return hash;
        }

        hash = (uint)Animator.StringToHash(name);

        newNames.Add(name);
        hashToName[hash] = name;
        nameToHash[name] = hash;

        return hash;
    }

    /// <summary>
    /// Logs an error adding current modification to the message
    /// </summary>
    /// <param name="message"></param>
    private void LogError(string message)
    {
        if (currentModification is not null)
        {
            AnimationsPlugin.Logger.LogError($"{message} | Mod: {currentModification.Key}");
        }
        else
        {
            AnimationsPlugin.Logger.LogError(message);
        }
    }
}
