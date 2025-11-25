using System;
using System.Collections.Generic;
using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using R2API.Animations.Models.Interfaces;
using R2API.Models;
using UnityEngine;

namespace R2API;
internal partial class ModificationsBundleCreator
{
    /// <summary>
    /// Creates new BlendTree from Motion
    /// </summary>
    /// <param name="animationClips"></param>
    /// <param name="motion"></param>
    /// <param name="blendTreeIndexArray"></param>
    /// <param name="blendTreeConstantArray"></param>
    private void CreateBlendTreeFromMotion(AssetTypeValueField animationClips, IMotion motion, AssetTypeValueField blendTreeIndexArray, AssetTypeValueField blendTreeConstantArray)
    {
        var blendTreeIndexField = ValueBuilder.DefaultValueFieldFromTemplate(blendTreeIndexArray.TemplateField.Children[1]);
        blendTreeIndexArray.Children.Add(blendTreeIndexField);
        if (motion is not null && (motion.Clip || motion.BlendTree != null))
        {
            blendTreeIndexField.AsInt = blendTreeConstantArray.Children.Count;

            var blendTreeField = ValueBuilder.DefaultValueFieldFromTemplate(blendTreeConstantArray.TemplateField.Children[1]);
            blendTreeConstantArray.Children.Add(blendTreeField);

            var nodeArray = blendTreeField["data"]["m_NodeArray"]["Array"];
            if (motion.Clip)
            {
                CreateBlendTreeNodeFromClip(animationClips, nodeArray, motion.Clip);
            }
            else
            {
                CreateBlendTreeNodeFromBlendTree(animationClips, nodeArray, motion.BlendTree);
            }
        }
        else
        {
            blendTreeIndexField.AsInt = -1;
        }
    }

    /// <summary>
    /// Creates new BlendTreeNode from AnimationClip
    /// </summary>
    /// <param name="animationClips"></param>
    /// <param name="nodeArray"></param>
    /// <param name="clip"></param>
    /// <param name="cycleOffset"></param>
    /// <param name="mirror"></param>
    /// <param name="timeScale"></param>
    /// <returns></returns>
    private int CreateBlendTreeNodeFromClip(AssetTypeValueField animationClips, AssetTypeValueField nodeArray, AnimationClip clip, float cycleOffset = 0, bool mirror = false, float timeScale = 1)
    {
        TryAddDependency(clip, out var fileID, out var clipPathID);

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
        nodeDataField["m_Duration"].AsFloat = timeScale == 0 ? 100 : (1 / timeScale);
        nodeDataField["m_CycleOffset"].AsFloat = cycleOffset;
        nodeDataField["m_Mirror"].AsBool = mirror;
        nodeArray.Children.Add(nodeField);
        return nodeArray.Children.Count - 1;
    }

    /// <summary>
    /// Creates new BlendTreeNode from BlendTree
    /// </summary>
    /// <param name="animationClips"></param>
    /// <param name="nodeArray"></param>
    /// <param name="blendTree"></param>
    /// <param name="cycleOffset"></param>
    /// <param name="mirror"></param>
    /// <returns></returns>
    private int CreateBlendTreeNodeFromBlendTree(AssetTypeValueField animationClips, AssetTypeValueField nodeArray, IBlendTree blendTree, float cycleOffset = 0, bool mirror = false)
    {
        var blendParameterHash = GetOrAddName(blendTree.BlendParameter);
        var blendParameterYHash = GetOrAddName(blendTree.BlendParameterY);

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

        var childIndicesArray = nodeDataField["m_ChildIndices"]["Array"];
        foreach (var child in blendTree.Children)
        {
            if (child.Clip)
            {
                var childIndexField = ValueBuilder.DefaultValueFieldFromTemplate(childIndicesArray.TemplateField.Children[1]);
                childIndexField.AsInt = CreateBlendTreeNodeFromClip(animationClips, nodeArray, child.Clip, child.CycleOffset, child.Mirror, child.TimeScale);
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
                var blend1dData = nodeDataField["m_Blend1dData"]["data"];
                var childThresholdArray = blend1dData["m_ChildThresholdArray"]["Array"];
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
                var blend2dData = nodeDataField["m_Blend2dData"]["data"];
                var childPositionArray = blend2dData["m_ChildPositionArray"]["Array"];
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
                var blendDirectData = nodeDataField["m_BlendDirectData"]["data"];
                var childBlendEventIDArray = blendDirectData["m_ChildBlendEventIDArray"]["Array"];
                foreach (var child in blendTree.Children)
                {
                    var childBlendEventField = ValueBuilder.DefaultValueFieldFromTemplate(childBlendEventIDArray.TemplateField.Children[1]);
                    childBlendEventField.AsUInt = GetOrAddName(child.DirectBlendParameter);
                    childBlendEventIDArray.Children.Add(childBlendEventField);
                }
                break;
            }
        }

        return index;
    }

    private void ComputeFreeform(IBlendTree blendTree, AssetTypeValueField nodeDataField)
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

        var blend2dData = nodeDataField["m_Blend2dData"]["data"];
        var childPositionArray = blend2dData["m_ChildPositionArray"]["Array"];
        var childMagnitudeArray = blend2dData["m_ChildMagnitudeArray"]["Array"];
        var childPairVectorArray = blend2dData["m_ChildPairVectorArray"]["Array"];
        var childPairAvgMagInvArray = blend2dData["m_ChildPairAvgMagInvArray"]["Array"];
        var childNeighborArray = blend2dData["m_ChildNeighborListArray"]["Array"];

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
            var childNeighborNeighborArray = childNeighborField["m_NeighborArray"]["Array"];
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

    private void ComputeNeighborsFreeform(IBlendTree blendTree, Vector2[] positions, float[] magnitudes, Vector2[] pairVectors, float[] pairAvgMagInvs, bool[] neighbors)
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
}

