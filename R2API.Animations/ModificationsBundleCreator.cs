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
            foreach (var (layerName, states) in modification.NewStates)
            {
                var layerHash = (uint)Animator.StringToHash(layerName);
                var layer = mController["m_LayerArray.Array"].FirstOrDefault(f => f["data.m_Binding"].AsUInt == layerHash);
                if (layer is null)
                {
                    AnimationsPlugin.Logger.LogError($"Layer \"{layerName}\" not found for a controller \"{controllerName}\". Mod: {modification.Key}");
                    continue;
                }

                var stateMachineIndex = layer["data.m_StateMachineIndex"].AsUInt;
                var stateMachine = mController["m_StateMachineArray.Array"][(int)stateMachineIndex];
                var statesArray = stateMachine["data.m_StateConstantArray.Array"];

                foreach (var state in states)
                {

                    var stateHash = (uint)Animator.StringToHash(state.Name);
                    var stateFullPathName = $"{layerName}.{state.Name}";
                    var stateFullPathHash = (uint)Animator.StringToHash(stateFullPathName);
                    var tagHash = (uint)Animator.StringToHash(state.Tag);
                    var speedHash = (uint)Animator.StringToHash(state.SpeedParam);
                    var mirrorHash = (uint)Animator.StringToHash(state.MirrorParam);
                    var cycleOffsetHash = (uint)Animator.StringToHash(state.CycleOffsetParam);
                    var timeHash = (uint)Animator.StringToHash(state.TimeParam);

                    var stateField = ValueBuilder.DefaultValueFieldFromTemplate(statesArray.TemplateField.Children[1]);
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
                    statesArray.Children.Add(stateField);

                    var transitions = stateField["data.m_TransitionConstantArray.Array"];
                    foreach (var transition in state.Transitions)
                    {
                        AddTransition(layerName, state.Name, transition, transitions, statesArray);
                    }

                    var blendTreeIndex = stateField["data.m_BlendTreeConstantIndexArray.Array"];
                    var blendTreeIndexField = ValueBuilder.DefaultValueFieldFromTemplate(blendTreeIndex.TemplateField.Children[1]);
                    blendTreeIndex.Children.Add(blendTreeIndexField);
                    if (state.Clip || state.BlendTree != null)
                    {
                        blendTreeIndexField.AsInt = 0;

                        var blendTree = stateField["data.m_BlendTreeConstantArray.Array"];
                        var blendTreeField = ValueBuilder.DefaultValueFieldFromTemplate(blendTree.TemplateField.Children[1]);
                        blendTree.Children.Add(blendTreeField);

                        var nodeArray = blendTreeField["data.m_NodeArray.Array"];
                        if (state.Clip)
                        {
                            CreateBlendTreeNodeFromClip(animationClips, nodeArray, state.Clip, state.ClipBundlePath, state.CycleOffset, state.Mirror);
                        }
                        else
                        {
                            CreateBlendTreeNodeFromBlendTree(animationClips, nodeArray, state.BlendTree, state.CycleOffset, state.Mirror);
                        }
                    }
                    else
                    {
                        blendTreeIndexField.AsInt = -1;
                    }

                    names.Add(stateFullPathName);
                    names.Add(state.Name);
                    if (!string.IsNullOrWhiteSpace(state.Tag))
                    {
                        names.Add(state.Tag);
                    }
                }
            }
        }
    }

    private int CreateBlendTreeNodeFromBlendTree(AssetTypeValueField animationClips, AssetTypeValueField nodeArray, BlendTree blendTree, float cycleOffset, bool mirror)
    {
        var blendParameterHash = (uint)Animator.StringToHash(blendTree.BlendParameter);
        var blendParameterYHash = (uint)Animator.StringToHash(blendTree.BlendParameterY);

        var nodeField = ValueBuilder.DefaultValueFieldFromTemplate(nodeArray.TemplateField.Children[1]);
        nodeArray.Children.Add(nodeField);
        var index = nodeArray.Children.Count - 1;

        var nodeDataField = nodeField["data"];
        nodeDataField["m_BlendType"].AsUInt = (uint)blendTree.BlendType;

        if (blendTree.BlendType != BlendTreeType.Direct)
        {
            nodeDataField["m_BlendEventID"].AsUInt = blendParameterHash;
        }
        else
        {
            nodeDataField["m_BlendEventID"].AsUInt = 0xffffffffu;
        }

        if (blendTree.BlendType != BlendTreeType.Simple1D && blendTree.BlendType != BlendTreeType.Direct)
        {
            nodeDataField["m_BlendEventYID"].AsUInt = blendParameterYHash;
        }
        else
        {
            nodeDataField["m_BlendEventYID"].AsUInt = 0xffffffffu;
        }

        nodeDataField["m_ClipID"].AsUInt = 0xffffffffu;
        nodeDataField["m_Duration"].AsFloat = 0;
        nodeDataField["m_CycleOffset"].AsFloat = cycleOffset;
        nodeDataField["m_Mirror"].AsBool = mirror;

        var childIndicesArray = nodeDataField["m_ChildIndices.Array"];
        foreach (var child in blendTree.Children)
        {
            if (child.Clip)
            {
                var childIndexField = ValueBuilder.DefaultValueFieldFromTemplate(childIndicesArray.TemplateField.Children[1]);
                childIndexField.AsInt = CreateBlendTreeNodeFromClip(animationClips, nodeArray, child.Clip, child.ClipBundlePath, child.CycleOffset, child.Mirror, child.TimeScale);
                childIndicesArray.Children.Add(childIndexField);
            }
            else if (child.BlendTree is not null)
            {
                var childIndexField = ValueBuilder.DefaultValueFieldFromTemplate(childIndicesArray.TemplateField.Children[1]);
                childIndexField.AsInt = CreateBlendTreeNodeFromBlendTree(animationClips, nodeArray, child.BlendTree, child.CycleOffset, child.Mirror);
                childIndicesArray.Children.Add(childIndexField);
            }

        }

        switch (blendTree.BlendType)
        {
            case BlendTreeType.Simple1D:
            {
                var blend1dData = nodeDataField["m_Blend1dData.data"];
                var childThresholdArray = blend1dData["m_ChildThresholdArray.Array"];
                foreach (var child in blendTree.Children)
                {
                    var thresholdField = ValueBuilder.DefaultValueFieldFromTemplate(childThresholdArray.TemplateField.Children[1]);
                    thresholdField.AsFloat = child.Threshold;
                    childThresholdArray.Children.Add(thresholdField);
                }

                break;
            }
            case BlendTreeType.SimpleDirectional2D:
            {
                var blend2dData = nodeDataField["m_Blend2dData.data"];
                var childPositionArray = blend2dData["m_ChildPositionArray.Array"];
                foreach (var child in blendTree.Children)
                {
                    var childBlendEventField = ValueBuilder.DefaultValueFieldFromTemplate(childPositionArray.TemplateField.Children[1]);
                    childBlendEventField["x"].AsFloat = child.Position.x;
                    childBlendEventField["y"].AsFloat = child.Position.y;
                    childPositionArray.Children.Add(childBlendEventField);
                }
                break;
            }
            case BlendTreeType.FreeformDirectional2D:
            case BlendTreeType.FreeformCartesian2D:
            {
                ComputeFreeform(blendTree, nodeDataField);
                break;
            }
            case BlendTreeType.Direct:
            {
                var blendDirectData = nodeDataField["m_BlendDirectData.data"];
                var childBlendEventIDArray = blendDirectData["m_ChildBlendEventIDArray.Array"];
                foreach (var child in blendTree.Children)
                {
                    var childBlendEventField = ValueBuilder.DefaultValueFieldFromTemplate(childBlendEventIDArray.TemplateField.Children[1]);
                    childBlendEventField.AsUInt = (uint)Animator.StringToHash(child.DirectBlendParameter);
                    childBlendEventIDArray.Children.Add(childBlendEventField);
                }
                break;
            }
        }

        return index;
    }

    private void ComputeFreeform(BlendTree blendTree, AssetTypeValueField nodeDataField)
    {
        var count = blendTree.Children.Count;

        var positions = new Vector2[count];
        var magnitudes = new float[count];
        var pairVectors = new Vector2[count * count];
        var pairAvgMagInvs = new float[count * count];
        var neighbors = new bool[count * count];

        for (var i = 0; i < blendTree.Children.Count; i++)
        {
            var child = blendTree.Children[i];
            positions[i] = child.Position;

            if (blendTree.BlendType == BlendTreeType.FreeformDirectional2D)
            {
                magnitudes[i] = child.Position.magnitude;
            }
        }

        if (blendTree.BlendType == BlendTreeType.FreeformDirectional2D)
        {
            for (var i = 0; i < count; i++)
            {
                var child = blendTree.Children[i];
                for (var j = 0; j < count; j++)
                {
                    var innerChild = blendTree.Children[j];
                    var pairIndex = i + j * count;

                    var magnitude = magnitudes[i];
                    var innerMagnitude = magnitudes[j];
                    var avgMagnitude = innerMagnitude + magnitude;
                    avgMagnitude = avgMagnitude == 0 ? float.PositiveInfinity : 2 / avgMagnitude;
                    pairAvgMagInvs[pairIndex] = avgMagnitude;

                    var angle = 0f;
                    if (innerMagnitude != 0 && magnitude != 0)
                    {
                        angle = Vector2.SignedAngle(child.Position, innerChild.Position) * Mathf.Deg2Rad;
                    }
                    var mag = (innerMagnitude - magnitude) * avgMagnitude;
                    pairVectors[pairIndex] = new Vector2(angle, mag);
                }
            }
        }
        else if (blendTree.BlendType == BlendTreeType.FreeformCartesian2D)
        {
            for (var i = 0; i < count; i++)
            {
                var child = blendTree.Children[i];
                for (var j = 0; j < count; j++)
                {
                    var innerChild = blendTree.Children[j];
                    var pairIndex = i + j * count;
                    pairAvgMagInvs[pairIndex] = 1 / (innerChild.Position - child.Position).sqrMagnitude;

                    var pair = innerChild.Position - child.Position;
                    pairVectors[pairIndex] = pair;
                }
            }
        }

        ComputeNeighborsFreeform(blendTree, positions, magnitudes, pairVectors, pairAvgMagInvs, neighbors);

        var blend2dData = nodeDataField["m_Blend2dData.data"];
        var childPositionArray = blend2dData["m_ChildPositionArray.Array"];
        var childMagnitudeArray = blend2dData["m_ChildMagnitudeArray.Array"];
        var childPairVectorArray = blend2dData["m_ChildPairVectorArray.Array"];
        var childPairAvgMagInvArray = blend2dData["m_ChildPairAvgMagInvArray.Array"];
        var childNeighborArray = blend2dData["m_ChildNeighborListArray.Array"];

        for (var i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            var childBlendEventField = ValueBuilder.DefaultValueFieldFromTemplate(childPositionArray.TemplateField.Children[1]);
            childBlendEventField["x"].AsFloat = position.x;
            childBlendEventField["y"].AsFloat = position.y;
            childPositionArray.Children.Add(childBlendEventField);
           
            if (blendTree.BlendType == BlendTreeType.FreeformDirectional2D)
            {
                var magnitude = magnitudes[i];
                var childMagnitudeField = ValueBuilder.DefaultValueFieldFromTemplate(childMagnitudeArray.TemplateField.Children[1]);
                childMagnitudeField.AsFloat = magnitude;
                childMagnitudeArray.Children.Add(childMagnitudeField);
            }

            var childNeighborField = ValueBuilder.DefaultValueFieldFromTemplate(childNeighborArray.TemplateField.Children[1]);
            childNeighborArray.Children.Add(childNeighborField);
            var childNeighborNeighborArray = childNeighborField["m_NeighborArray.Array"];
            for (int j = 0; j < count; j++)
            {
                if (neighbors[i * count + j])
                {
                    var childNeighborNeighborField = ValueBuilder.DefaultValueFieldFromTemplate(childNeighborNeighborArray.TemplateField.Children[1]);
                    childNeighborNeighborField.AsInt = j;
                    childNeighborNeighborArray.Children.Add(childNeighborNeighborField);
                }
            }
        }

        for (var i = 0; i < pairVectors.Length; i++)
        {
            var magnitude = pairAvgMagInvs[i];
            var childPairAvgMagInvField = ValueBuilder.DefaultValueFieldFromTemplate(childPairAvgMagInvArray.TemplateField.Children[1]);
            childPairAvgMagInvField.AsFloat = magnitude;
            childPairAvgMagInvArray.Children.Add(childPairAvgMagInvField);

            var pair = pairVectors[i];
            var childPairVectorField = ValueBuilder.DefaultValueFieldFromTemplate(childPairVectorArray.TemplateField.Children[1]);
            childPairVectorField["x"].AsFloat = pair.x;
            childPairVectorField["y"].AsFloat = pair.y;
            childPairVectorArray.Children.Add(childPairVectorField);
        }
    }

    private void ComputeNeighborsFreeform(BlendTree blendTree, Vector2[] positions, float[] magnitudes, Vector2[] pairVectors, float[] pairAvgMagInvs, bool[] neighbors)
    {
        var count = blendTree.Children.Count;
        var cropArray = new int[count];
        var workspaceBlendVectors = new Vector2[count];

        var minX = 10000.0f;
        var maxX = -10000.0f;
        var minY = 10000.0f;
        var maxY = -10000.0f;

        foreach (var position in positions)
        {
            minX = Mathf.Min(minX, position.x);
            maxX = Mathf.Max(maxX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxY = Mathf.Max(maxY, position.y);
        }

        var xRange = (maxX - minX) * 0.5f;
        var yRange = (maxY - minY) * 0.5f;
        minX -= xRange;
        maxX += xRange;
        minY -= yRange;
        maxY += yRange;

        for (var i = 0; i <= 100; i++)
        {
            var x = i * 0.01f;
            for (var j = 0; j <= 100; j++)
            {
                var y = j * 0.01f;
                if (blendTree.BlendType == BlendTreeType.FreeformDirectional2D)
                {
                    GetWeightsFreeformDirectional(
                        positions,
                        magnitudes,
                        pairVectors,
                        pairAvgMagInvs,
                        cropArray,
                        workspaceBlendVectors,
                        minX * (1 - x) + maxX * x,
                        minY * (1 - y) + maxY * y);
                }
                else if (blendTree.BlendType == BlendTreeType.FreeformCartesian2D)
                {
                    GetWeightsFreeformCartesian(
                        positions,
                        pairVectors,
                        pairAvgMagInvs,
                        cropArray,
                        workspaceBlendVectors,
                        minX * (1 - x) + maxX * x,
                        minY * (1 - y) + maxY * y);
                }
                for (var c = 0; c < count; c++)
                {
                    if (cropArray[c] >= 0)
                    {
                        neighbors[c * count + cropArray[c]] = true;
                    }
                }
            }
        }
    }

    private void GetWeightsFreeformCartesian(
        Vector2[] positions,
        Vector2[] pairVectors,
        float[] pairAvgMagInvs,
        int[] cropArray,
        Vector2[] workspaceBlendVectors,
        float blendValueX,
        float blendValueY)
    {
        var count = positions.Length;
        var blendPosition = new Vector2(blendValueX, blendValueY);
        for (var i = 0; i < count; i++)
        {
            workspaceBlendVectors[i] = blendPosition - positions[i];
        }

        for (var i = 0; i < count; i++)
        {
            cropArray[i] = -1;
            var vecIO = workspaceBlendVectors[i];
            var value = 1f;
            for (var j = 0; j < count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var pairIndex = i + j * count;
                var vecIJ = pairVectors[pairIndex];
                var newValue = 1 - Vector2.Dot(vecIJ, vecIO) * pairAvgMagInvs[pairIndex];
                if (newValue <= 0)
                {
                    cropArray[i] = -1;
                    break;
                }

                if (newValue < value)
                {
                    cropArray[i] = j;
                    value = newValue;
                }
            }
        }
    }

	private float GetWeightFreeformDirectional(Vector2[] positions, Vector2[] pairVectors, float[] pairAvgMagInvs, Vector2[] workspaceBlendVectors, int i, int j, Vector2 blendPosition)
	{
		var pairIndex = i + j * positions.Length;
		var vecIJ = pairVectors[pairIndex];
		var vecIO = workspaceBlendVectors[i];
		vecIO.y *= pairAvgMagInvs[pairIndex];

        if (positions[i] == Vector2.zero)
        {
            vecIJ.x = workspaceBlendVectors[j].x;
        }
        else if (positions[j] == Vector2.zero)
        {
            vecIJ.x = workspaceBlendVectors[i].x;
        }
        else if (vecIJ.x == 0 || blendPosition == Vector2.zero)
        {
            vecIO.x = vecIJ.x;
        }
		
		return 1 - Vector2.Dot(vecIJ, vecIO) / vecIJ.sqrMagnitude;
	}

    private void GetWeightsFreeformDirectional(
        Vector2[] positions,
        float[] magnitudes,
        Vector2[] pairVectors,
        float[] pairAvgMagInvs,
        int[] cropArray,
        Vector2[] workspaceBlendVectors,
        float blendValueX,
        float blendValueY)
    {
        var count = positions.Length;
        var blendPosition = new Vector2(blendValueX, blendValueY);
        var magO = blendPosition.magnitude;

        if (blendPosition == Vector2.zero)
        {
            for (var i = 0; i < count; i++)
            {
                workspaceBlendVectors[i] = new Vector2(0, magO - magnitudes[i]);
            }
        }
        else
        {
            for (var i = 0; i < count; i++)
            {
                var position = positions[i];
                var angle = 0f;
                if (position != Vector2.zero)
                {
                    angle = Vector2.SignedAngle(position, blendPosition) * Mathf.Deg2Rad;
                }
                workspaceBlendVectors[i] = new Vector2(angle, magO - magnitudes[i]);
            }
        }

        for (var i = 0; i < count; i++)
        {
            // Fade out over 180 degrees away from example
            var value = 1 - Mathf.Abs(workspaceBlendVectors[i].x) * (1 / Mathf.PI);
            cropArray[i] = -1;
            for (int j = 0; j < count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var newValue = GetWeightFreeformDirectional(positions, pairVectors, pairAvgMagInvs, workspaceBlendVectors, i, j, blendPosition);
                if (newValue <= 0)
                {
                    cropArray[i] = -1;
                    break;
                }

                if (newValue < value)
                {
                    cropArray[i] = j;
                    value = newValue;
                }
            }
        }
    }

    private int CreateBlendTreeNodeFromClip(AssetTypeValueField animationClips, AssetTypeValueField nodeArray, AnimationClip clip, string clipBundlePath, float cycleOffset, bool mirror, float timeScale = 1)
    {
        var clipPathID = NativeHelpers.GetAssetPathID(clip);
        var clipBundleFile = manager.LoadBundleFile(clipBundlePath);
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

        var nodeField = ValueBuilder.DefaultValueFieldFromTemplate(nodeArray.TemplateField.Children[1]);
        var nodeDataField = nodeField["data"];
        nodeDataField["m_BlendEventID"].AsUInt = 0xffffffffu;
        nodeDataField["m_BlendEventYID"].AsUInt = 0xffffffffu;
        nodeDataField["m_ClipID"].AsInt = clipID;
        nodeDataField["m_Duration"].AsFloat = timeScale == 0 ? 100 : 1 / timeScale;
        nodeDataField["m_CycleOffset"].AsFloat = cycleOffset;
        nodeDataField["m_Mirror"].AsBool = mirror;
        nodeArray.Children.Add(nodeField);
        return nodeArray.Children.Count - 1;
    }

    private void AddTransitions()
    {
        var mController = baseField["m_Controller"];
        var controllerName = baseField["m_Name"].AsString;

        foreach (var modification in modifications)
        {
            foreach (var ((layerName, stateName), transitions) in modification.NewTransitions)
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

                var transitionsArray = stateField["data.m_TransitionConstantArray.Array"];
                foreach (var transition in transitions)
                {
                    AddTransition(layerName, stateName, transition, transitionsArray, states);
                }
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
        transitionDataField["m_InterruptionSource"].AsInt = (int)transition.InterruptionSource;
        transitionDataField["m_CanTransitionToSelf"].AsBool = transition.CanTransitionToSelf;
        transitionDataField["m_OrderedInterruption"].AsBool = transition.OrderedInterruption;
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
