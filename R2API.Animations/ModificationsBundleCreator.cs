using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using R2API.Models;
using UnityEngine;

namespace R2API;

internal class ModificationsBundleCreator
{
    private readonly AssetsManager manager;
    private readonly AssetsFileInstance assetFile;
    private readonly long sourceAnimatorControllerPathID;
    private readonly AssetsFileInstance dummyAssetFile;
    private readonly BundleFileInstance dummyBundleFile;
    private readonly long dummyAnimatorControllerPathID;
    private readonly List<AnimatorModifications> modifications;
    private readonly string modifiedBundlePath;
    private readonly List<string> dependencies = [];
    private readonly HashSet<string> names = [];
    private AssetTypeValueField baseField;

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

        RemapPPtrs();

        AddParameters();
        AddStates();
        AddTransitions();
        AddTOS();

        SetExternals();

        SaveBundle();
    }

    private void AddParameters()
    {
        var existingParameters = new HashSet<uint>();
        var mController = baseField["m_Controller"];
        var values = mController["m_Values.data.m_ValueArray.Array"];
        var defaultValues = mController["m_DefaultValues.data"];

        var boolValues = defaultValues["m_BoolValues.Array"];
        var floatValues = defaultValues["m_FloatValues.Array"];
        var intValues = defaultValues["m_IntValues.Array"];

        foreach (var value in values)
        {
            existingParameters.Add(value["m_ID"].AsUInt);
        }

        foreach (var modification in modifications)
        {
            var parameters = modification.NewParameters;
            if (parameters is null)
            {
                continue;
            }

            foreach (var parameter in parameters)
            {
                var id = (uint)Animator.StringToHash(parameter.Name);
                if (!existingParameters.Add(id))
                {
                    AnimationsPlugin.Logger.LogError($"Parameter {parameter.Name} already exists for {baseField["m_Name"].AsString}");
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
                        AnimationsPlugin.Logger.LogError($"Not supported parameter type {parameter.Type}");
                        continue;
                }

                var valueField = ValueBuilder.DefaultValueFieldFromTemplate(values.TemplateField.Children[1]);
                valueField["m_ID"].AsUInt = id;
                valueField["m_Type"].AsUInt = (uint)parameter.Type;
                valueField["m_Index"].AsUInt = (uint)index;
                values.Children.Add(valueField);
                names.Add(parameter.Name);
            }
        }
    }

    private void AddStates()
    {
        var animationClips = baseField["m_AnimationClips.Array"];
        var mController = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;

        foreach (var modification in modifications)
        {
            foreach ((string layerName, State state) in modification.NewStates)
            {
                var layerHash = (uint)Animator.StringToHash(layerName);
                var layer = mController["m_LayerArray.Array"].FirstOrDefault(f => f["data.m_Binding"].AsUInt == layerHash);
                if (layer is null)
                {
                    AnimationsPlugin.Logger.LogError($"Layer \"{layerName}\" not found for a controller \"{controllerName}\". Mod: {modification.Key}");
                    continue;
                }

                var stateMachineIndex = layer["data.m_StateMachineIndex"].AsUInt;
                var clipPathID = NativeHelpers.GetAssetPathID(state.Clip);
                var clipBundleFile = manager.LoadBundleFile(state.ClipBundlePath);
                var clipAssetFile = manager.LoadAssetsFileFromBundle(clipBundleFile, 0, false);

                var depPath = $"archive:/{clipAssetFile.name}/{clipAssetFile.name}";
                var fileID = dependencies.IndexOf(depPath);
                if (fileID == -1)
                {
                    fileID = dependencies.Count;
                    dependencies.Add(depPath);
                }
                fileID++;

                var clipID = animationClips.Children.Count;
                var clipField = ValueBuilder.DefaultValueFieldFromTemplate(animationClips.TemplateField.Children[1]);
                clipField["m_FileID"].AsInt = fileID;
                clipField["m_PathID"].AsLong = clipPathID;
                animationClips.Children.Add(clipField);

                var stateMachine = mController["m_StateMachineArray.Array"][(int)stateMachineIndex];
                var stateHash = (uint)Animator.StringToHash(state.Name);
                var stateFullPathName = $"{layerName}.{state.Name}";
                var stateFullPathHash = (uint)Animator.StringToHash(stateFullPathName);
                var tagHash = (uint)Animator.StringToHash(state.Tag);
                var speedHash = (uint)Animator.StringToHash(state.SpeedParam);
                var mirrorHash = (uint)Animator.StringToHash(state.MirrorParam);
                var cycleOffsetHash = (uint)Animator.StringToHash(state.CycleOffsetParam);
                var timeHash = (uint)Animator.StringToHash(state.TimeParam);

                var states = stateMachine["data.m_StateConstantArray.Array"];
                var stateField = ValueBuilder.DefaultValueFieldFromTemplate(states.TemplateField.Children[1]);
                var stateDataField = stateField["data"];
                stateDataField["m_NameID"].AsUInt = stateHash;
                stateDataField["m_PathID"].AsUInt = stateFullPathHash;
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
                states.Children.Add(stateField);

                var transitions = stateField["data.m_TransitionConstantArray.Array"];
                foreach (var transition in state.Transitions)
                {
                    AddTransition(layerName, state.Name, transition, transitions, states);
                }

                var blendTreeIndex = stateField["data.m_BlendTreeConstantIndexArray.Array"];
                var blendTreeIndexField = ValueBuilder.DefaultValueFieldFromTemplate(blendTreeIndex.TemplateField.Children[1]);
                blendTreeIndexField.AsInt = 0;
                blendTreeIndex.Children.Add(blendTreeIndexField);

                var blendTree = stateField["data.m_BlendTreeConstantArray.Array"];
                var blendTreeField = ValueBuilder.DefaultValueFieldFromTemplate(blendTree.TemplateField.Children[1]);
                blendTree.Children.Add(blendTreeField);

                var nodeArray = blendTreeField["data.m_NodeArray.Array"];
                var nodeField = ValueBuilder.DefaultValueFieldFromTemplate(nodeArray.TemplateField.Children[1]);
                var nodeDataField = nodeField["data"];
                nodeDataField["m_BlendEventID"].AsUInt = 0xffffffffu;
                nodeDataField["m_BlendEventYID"].AsUInt = 0xffffffffu;
                nodeDataField["m_ClipID"].AsInt = clipID;
                nodeDataField["m_Duration"].AsFloat = 1;
                nodeDataField["m_CycleOffset"].AsFloat = 0;
                nodeArray.Children.Add(nodeField);

                names.Add(stateFullPathName);
                names.Add(state.Name);
                if (!string.IsNullOrWhiteSpace(state.Tag))
                {
                    names.Add(state.Tag);
                }
            }
        }
    }

    private void AddTransitions()
    {
        var mController = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;

        foreach (var modification in modifications)
        {
            foreach (var ((layerName, stateName), transition) in modification.NewTransitions)
            {
                var layerHash = (uint)Animator.StringToHash(layerName);
                var stateHash = (uint)Animator.StringToHash(stateName);
                var layer = mController["m_LayerArray.Array"].FirstOrDefault(f => f["data.m_Binding"].AsUInt == layerHash);
                if (layer is null)
                {
                    AnimationsPlugin.Logger.LogError($"Layer \"{layerName}\" not found for controller {controllerName}. Mod: {modification.Key}");
                    continue;
                }

                var stateMachineIndex = layer["data.m_StateMachineIndex"].AsUInt;

                var stateMachine = mController["m_StateMachineArray.Array"][(int)stateMachineIndex];
                var states = stateMachine["data.m_StateConstantArray.Array"];
                var stateField = states.FirstOrDefault(f => f["data.m_NameID"].AsUInt == stateHash);
                if (stateField is null)
                {
                    AnimationsPlugin.Logger.LogError($"State \"{stateName}\" not found for a layer \"{layerName}\" for a controller \"{controllerName}\". Mod: {modification.Key}");
                }
                var transitions = stateField["data.m_TransitionConstantArray.Array"];

                AddTransition(layerName, stateName, transition, transitions, states);
            }
        }
    }

    private void AddTransition(string layerName, string stateName, Transition transition, AssetTypeValueField transitions, AssetTypeValueField states)
    {
        var stateFullPathName = $"{layerName}.{stateName}";
        var destinationStateHash = (uint)Animator.StringToHash(transition.DestinationStateName);
        var transitionName = $"{stateName} -> {transition.DestinationStateName}";
        var transitionHash = (uint)Animator.StringToHash(transitionName);
        var transitionFullPathName = $"{stateFullPathName} -> {layerName}.{transition.DestinationStateName}";
        var transitionFullPathHash = (uint)Animator.StringToHash(transitionFullPathName);
        var destinationState = 0u;
        for (var i = 0; i < states.Children.Count; i++)
        {
            if (states[i]["data.m_NameID"].AsUInt == destinationStateHash)
            {
                destinationState = (uint)i;
                break;
            }
        }

        var transitionField = ValueBuilder.DefaultValueFieldFromTemplate(transitions.TemplateField.Children[1]);
        var transitionDataField = transitionField["data"];
        transitionDataField["m_DestinationState"].AsUInt = destinationState;
        transitionDataField["m_FullPathID"].AsUInt = transitionFullPathHash;
        transitionDataField["m_ID"].AsUInt = transitionHash;
        transitionDataField["m_UserID"].AsUInt = 0u;
        transitionDataField["m_TransitionOffset"].AsFloat = transition.Offset;
        transitionDataField["m_TransitionDuration"].AsFloat = transition.TransitionDuration;
        transitionDataField["m_HasFixedDuration"].AsBool = transition.HasFixedDuration;
        transitionDataField["m_HasExitTime"].AsBool = transition.HasExitTime;
        transitionDataField["m_ExitTime"].AsFloat = transition.ExitTime;
        transitionDataField["m_InterruptionSource"].AsInt = 0;
        transitionDataField["m_CanTransitionToSelf"].AsBool = true;
        transitionDataField["m_OrderedInterruption"].AsBool = true;
        transitions.Children.Add(transitionField);

        var conditions = transitionDataField["m_ConditionConstantArray.Array"];
        foreach (var condition in transition.Conditions)
        {
            var parameterHash = (uint)Animator.StringToHash(condition.ParamName);
            var conditionField = ValueBuilder.DefaultValueFieldFromTemplate(conditions.TemplateField.Children[1]);
            var conditionFieldData = conditionField["data"];
            conditionFieldData["m_ConditionMode"].AsUInt = (uint)condition.ConditionMode;
            conditionFieldData["m_EventID"].AsUInt = parameterHash;
            conditionFieldData["m_EventThreshold"].AsFloat = condition.Value;
            conditionFieldData["m_ExitTime"].AsFloat = 0f;
            conditions.Children.Add(conditionField);
        }

        names.Add(transitionName);
        names.Add(transitionFullPathName);
    }

    private void AddTOS()
    {
        var tos = baseField["m_TOS.Array"];
        var existingNames = new HashSet<string>(tos.Select(f => f["second"].AsString));
        foreach (var name in names)
        {
            if (!existingNames.Add(name))
            {
                continue;
            }

            var hash = (uint)Animator.StringToHash(name);
            var tosField = ValueBuilder.DefaultValueFieldFromTemplate(tos.TemplateField.Children[1]);
            tosField["first"].AsUInt = hash;
            tosField["second"].AsString = name;
            tos.Children.Add(tosField);
        }
    }

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
}
