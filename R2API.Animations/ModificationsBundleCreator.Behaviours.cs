using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using R2API.Models;
using UnityEngine;

namespace R2API;

internal partial class ModificationsBundleCreator
{
    /// <summary>
    /// Rebuilding controller behaviours and adding new ones if were found
    /// </summary>
    private void AddBehaviours()
    {
        var controller = baseField["m_Controller"];
        var layerArray = controller["m_LayerArray"]["Array"];
        var stateMachineArray = controller["m_StateMachineArray"]["Array"];
        GatherLayerBehaviourNodes(layerArray, stateMachineArray, out var layerBehaviourNodes);

        var stateMachineBehavioursArray = baseField["m_StateMachineBehaviours"]["Array"];
        var stateMachineBehaviourVectorDescription = baseField["m_StateMachineBehaviourVectorDescription"];
        var stateMachineBehaviourRangesArray = stateMachineBehaviourVectorDescription["m_StateMachineBehaviourRanges"]["Array"];
        var stateMachineBehaviourIndicesArray = stateMachineBehaviourVectorDescription["m_StateMachineBehaviourIndices"]["Array"];

        var behaviours = new List<(int fileID, long pathID)>();
        var addedBehaviours = new Dictionary<StateMachineBehaviour, uint>();
        foreach (var behaviour in stateMachineBehavioursArray)
        {
            behaviours.Add((behaviour["m_FileID"].AsInt, behaviour["m_PathID"].AsLong));
        }

        var unknownStates = new List<(uint stateID, int layerIndex, List<uint> behaviours)>();
        foreach (var range in stateMachineBehaviourRangesArray)
        {
            var first = range["first"];
            var layerIndex = first["m_LayerIndex"].AsUInt;
            var stateID = first["m_StateID"].AsUInt;

            var second = range["second"];
            var startIndex = (int)second["m_StartIndex"].AsUInt;
            var count = second["m_Count"].AsUInt;

            var behavioursNodes = layerBehaviourNodes[(int)layerIndex];
            if (behavioursNodes.TryGetValue(stateID, out var node))
            {
                for (var i = 0; i < count; i++)
                {
                    node.Behaviours.Add(stateMachineBehaviourIndicesArray[i + startIndex].AsUInt);
                }
            }
            else
            {
                var behaviourIndices = new List<uint>();
                for (var i = 0; i < count; i++)
                {
                    behaviourIndices.Add(stateMachineBehaviourIndicesArray[i + startIndex].AsUInt);
                }
                //If a state machine has a behaviour but doesn't have any states
                //we can't figure out it's name which is not that important,
                //but we still need to keep track of it, since we recreate
                //behaviours arrays from scratch
                unknownStates.Add((stateID, (int)layerIndex, behaviourIndices));
            }
        }

        //Each existing state includes behaviours from parent state machines,
        //removing them here since we track state machine behaviours separately.
        foreach (var layerBehaviour in layerBehaviourNodes)
        {
            foreach (var node in layerBehaviour.Values.Where(v => v.IsState))
            {
                var parent = node.Parent;
                while (parent is not null)
                {
                    foreach (var parentBehaviour in parent.Behaviours)
                    {
                        node.Behaviours.Remove(parentBehaviour);
                    }

                    parent = parent.Parent;
                }
            }
        }

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var layer in modification.Layers)
            {
                var layerID = GetOrAddName(layer.Name);
                var layerIndex = layerArray.IndexOf(l => l["data"]["m_Binding"].AsUInt == layerID);
                var behavioursNodes = layerBehaviourNodes[layerIndex];
                if (layer.StateMachine)
                {
                    FillExistingStateMachineBehaviours("", layer.StateMachine, behaviours, behavioursNodes, addedBehaviours);
                }
                else
                {
                    var layerField = layerArray[layerIndex];
                    FillSyncedStateMachineBehaviours(layer.NewSyncedBehaviours, stateMachineArray, behavioursNodes, layerField, behaviours, addedBehaviours);
                }
            }
            foreach (var layer in modification.NewLayers)
            {
                var layerID = GetOrAddName(layer.Name);
                var layerIndex = layerArray.IndexOf(l => l["data"]["m_Binding"].AsUInt == layerID);
                var behavioursNodes = layerBehaviourNodes[layerIndex];
                if (layer.StateMachine)
                {
                    FillStateMachineBehaviours("", layer.StateMachine, behaviours, behavioursNodes, addedBehaviours);
                }
                else
                {
                    var layerField = layerArray[layerIndex];
                    FillSyncedStateMachineBehaviours(layer.SyncedBehaviours, stateMachineArray, behavioursNodes, layerField, behaviours, addedBehaviours);
                }
            }
        }
        currentModification = null;

        stateMachineBehavioursArray.Children.Clear();
        foreach (var (fileID, pathID) in behaviours)
        {
            var behaviourField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehavioursArray.TemplateField.Children[1]);
            stateMachineBehavioursArray.Children.Add(behaviourField);
            behaviourField["m_FileID"].AsInt = fileID;
            behaviourField["m_PathID"].AsLong = pathID;
        }

        stateMachineBehaviourIndicesArray.Children.Clear();
        stateMachineBehaviourRangesArray.Children.Clear();
        for (var i = 0; i < layerBehaviourNodes.Count; i++)
        {
            var layerBehaviours = layerBehaviourNodes[i];
            foreach (var node in layerBehaviours.Values)
            {
                var startIndex = stateMachineBehaviourIndicesArray.Children.Count;
                var count = 0;
                foreach (var behaviourIndex in node.Behaviours)
                {
                    count++;

                    var behaviourIndexField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehaviourIndicesArray.TemplateField.Children[1]);
                    stateMachineBehaviourIndicesArray.Children.Add(behaviourIndexField);
                    behaviourIndexField.AsUInt = behaviourIndex;
                }

                if (node.IsState)
                {
                    var parent = node.Parent;
                    while (parent is not null)
                    {
                        for (var j = 0; j < parent.Behaviours.Count; j++)
                        {
                            var behaviourIndex = parent.Behaviours[j];
                            count++;

                            var behaviourIndexField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehaviourIndicesArray.TemplateField.Children[1]);
                            stateMachineBehaviourIndicesArray.Children.Insert(startIndex + j, behaviourIndexField);
                            behaviourIndexField.AsUInt = behaviourIndex;
                        }

                        parent = parent.Parent;
                    }
                }

                if (count > 0)
                {
                    var behaviourRangeField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehaviourRangesArray.TemplateField.Children[1]);
                    stateMachineBehaviourRangesArray.Children.Add(behaviourRangeField);
                    var first = behaviourRangeField["first"];
                    first["m_StateID"].AsUInt = node.FullPathID;
                    first["m_LayerIndex"].AsInt = i;
                    var second = behaviourRangeField["second"];
                    second["m_StartIndex"].AsUInt = (uint)startIndex;
                    second["m_Count"].AsUInt = (uint)count;
                }
            }
        }

        foreach (var state in unknownStates)
        {
            var startIndex = stateMachineBehaviourIndicesArray.Children.Count;
            var count = 0;
            foreach (var behaviourIndex in state.behaviours)
            {
                count++;

                var behaviourIndexField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehaviourIndicesArray.TemplateField.Children[1]);
                stateMachineBehaviourIndicesArray.Children.Add(behaviourIndexField);
                behaviourIndexField.AsUInt = behaviourIndex;
            }

            if (count > 0)
            {
                var behaviourRangeField = ValueBuilder.DefaultValueFieldFromTemplate(stateMachineBehaviourRangesArray.TemplateField.Children[1]);
                stateMachineBehaviourRangesArray.Children.Add(behaviourRangeField);
                var first = behaviourRangeField["first"];
                first["m_StateID"].AsUInt = state.stateID;
                first["m_LayerIndex"].AsInt = state.layerIndex;
                var second = behaviourRangeField["second"];
                second["m_StartIndex"].AsUInt = (uint)startIndex;
                second["m_Count"].AsUInt = (uint)count;
            }
        }

        //Apparently the order in this array (and only in this) matters for correct behaviours execution
        stateMachineBehaviourRangesArray.Children = stateMachineBehaviourRangesArray.Children
            .OrderBy(f => f["first"]["m_StateID"].AsUInt)
            .ThenBy(f => f["first"]["m_LayerIndex"].AsInt)
            .ToList();
    }

    /// <summary>
    /// Collects BehaviourNodes from StateMachines and States
    /// </summary>
    /// <param name="layerArray"></param>
    /// <param name="stateMachineArray"></param>
    /// <param name="layerBehaviourNodes"></param>
    private void GatherLayerBehaviourNodes(AssetTypeValueField layerArray, AssetTypeValueField stateMachineArray, out List<Dictionary<uint, BehavioursNode>> layerBehaviourNodes)
    {
        layerBehaviourNodes = new List<Dictionary<uint, BehavioursNode>>();
        var stateMachineBehavioursNodes = new List<Dictionary<uint, BehavioursNode>>();
        foreach (var stateMachineField in stateMachineArray)
        {
            var behavioursNodes = new Dictionary<uint, BehavioursNode>();
            stateMachineBehavioursNodes.Add(behavioursNodes);

            var dataField = stateMachineField["data"];
            var statesArray = dataField["m_StateConstantArray"]["Array"];
            foreach (var statesField in statesArray)
            {
                var fullPathID = statesField["data"]["m_FullPathID"].AsUInt;
                var fullPath = hashToName[fullPathID];
                var node = new BehavioursNode
                {
                    FullPathID = fullPathID,
                    FullPath = fullPath,
                    IsState = true,
                    Parent = GetOrCreateParent(fullPath, behavioursNodes),
                };
                behavioursNodes[fullPathID] = node;

                BehavioursNode GetOrCreateParent(string fullPath, Dictionary<uint, BehavioursNode> behavioursNodes)
                {
                    var lastSeparatorIndex = fullPath.LastIndexOf('.');
                    if (lastSeparatorIndex == -1)
                    {
                        return null;
                    }

                    var parentFullPath = fullPath[..lastSeparatorIndex];
                    var parentFullPathID = GetOrAddName(parentFullPath);
                    if (!behavioursNodes.TryGetValue(parentFullPathID, out var parent))
                    {
                        parent = new BehavioursNode
                        {
                            FullPathID = parentFullPathID,
                            FullPath = parentFullPath,
                            IsState = false,
                            Parent = GetOrCreateParent(parentFullPath, behavioursNodes)
                        };

                        behavioursNodes[parentFullPathID] = parent;
                    }

                    return parent;
                }
            }
        }

        foreach (var modification in modifications)
        {
            currentModification = modification;
            foreach (var layer in modification.Layers)
            {
                if (layer.StateMachine)
                {
                    var layerID = GetOrAddName(layer.Name);
                    var layerField = layerArray.FirstOrDefault(l => l["data"]["m_Binding"].AsUInt == layerID);
                    var stateMachineIndex = layerField["data"]["m_StateMachineIndex"].AsUInt;
                    var behavioursNodes = stateMachineBehavioursNodes[(int)stateMachineIndex];
                    FillBehaviourNodesFromExistingStateMachine(layer.StateMachine, behavioursNodes, "");
                }
            }
            foreach (var layer in modification.NewLayers)
            {
                if (layer.StateMachine)
                {
                    var layerID = GetOrAddName(layer.Name);
                    var layerField = layerArray.FirstOrDefault(l => l["data"]["m_Binding"].AsUInt == layerID);
                    var stateMachineIndex = layerField["data"]["m_StateMachineIndex"].AsUInt;
                    var behavioursNodes = stateMachineBehavioursNodes[(int)stateMachineIndex];
                    FillBehaviourNodesFromStateMachine(layer.StateMachine, behavioursNodes, "");
                }
            }
        }
        currentModification = null;

        foreach (var layer in layerArray)
        {
            var stateMachineStatesBehaviours = new Dictionary<uint, List<uint>>();
            var stateMachineIndex = layer["data"]["m_StateMachineIndex"].AsUInt;
            var syncIndex = layer["data"]["m_StateMachineSynchronizedLayerIndex"].AsUInt;
            var stateMachineNodes = stateMachineBehavioursNodes[(int)stateMachineIndex];
            if (syncIndex > 0)
            {
                //Synced layers use original layer behaviours for state machines,
                //so we keep these nodes while making new ones for states
                stateMachineNodes = stateMachineNodes
                    .ToDictionary(
                        n => n.Key,
                        n => !n.Value.IsState ? n.Value : new BehavioursNode
                        {
                            FullPath = n.Value.FullPath,
                            FullPathID = n.Value.FullPathID,
                            Parent = n.Value.Parent,
                            IsState = true,
                        });
            }

            layerBehaviourNodes.Add(stateMachineNodes);
        }
    }

    /// <summary>
    /// Collects BehaviourNodes from modifications if they were not added already
    /// </summary>
    /// <param name="stateMachine"></param>
    /// <param name="behavioursNodes"></param>
    /// <param name="stateMachinePath"></param>
    /// <param name="parent"></param>
    private void FillBehaviourNodesFromExistingStateMachine(ExistingStateMachine stateMachine, Dictionary<uint, BehavioursNode> behavioursNodes, string stateMachinePath, BehavioursNode parent = null)
    {
        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";
        var stateMachineID = GetOrAddName(newStateMachinePath);
        if (!behavioursNodes.TryGetValue(stateMachineID, out var stateMachineNode))
        {
            behavioursNodes[stateMachineID] = stateMachineNode = new BehavioursNode
            {
                FullPath = newStateMachinePath,
                FullPathID = stateMachineID,
                Parent = parent,
                IsState = false,
            };
        }
        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            FillBehaviourNodesFromExistingStateMachine(subStateMachine, behavioursNodes, newStateMachinePath, stateMachineNode);
        }
        foreach (var subStateMachine in stateMachine.NewSubStateMachines)
        {
            FillBehaviourNodesFromStateMachine(subStateMachine, behavioursNodes, newStateMachinePath, stateMachineNode);
        }
    }

    /// <summary>
    /// Collects BehaviourNodes from modifications if they were not added already
    /// </summary>
    /// <param name="stateMachine"></param>
    /// <param name="behavioursNodes"></param>
    /// <param name="stateMachinePath"></param>
    /// <param name="parent"></param>
    private void FillBehaviourNodesFromStateMachine(StateMachine stateMachine, Dictionary<uint, BehavioursNode> behavioursNodes, string stateMachinePath, BehavioursNode parent = null)
    {
        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";
        var stateMachineID = GetOrAddName(newStateMachinePath);
        if (!behavioursNodes.TryGetValue(stateMachineID, out var stateMachineNode))
        {
            behavioursNodes[stateMachineID] = stateMachineNode = new BehavioursNode
            {
                FullPath = newStateMachinePath,
                FullPathID = stateMachineID,
                Parent = parent,
                IsState = false,
            };
        }
        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            FillBehaviourNodesFromStateMachine(subStateMachine, behavioursNodes, newStateMachinePath, stateMachineNode);
        }
    }

    /// <summary>
    /// Fills BehaviourNodes in a Synced Layer with new Behaviours
    /// </summary>
    /// <param name="syncedBehaviours"></param>
    /// <param name="stateMachineArray"></param>
    /// <param name="behavioursNodes"></param>
    /// <param name="layerField"></param>
    /// <param name="behaviours"></param>
    /// <param name="addedBehaviours"></param>
    private void FillSyncedStateMachineBehaviours(List<SyncedBehaviour> syncedBehaviours, AssetTypeValueField stateMachineArray, Dictionary<uint, BehavioursNode> behavioursNodes, AssetTypeValueField layerField, List<(int, long)> behaviours, Dictionary<StateMachineBehaviour, uint> addedBehaviours)
    {
        var stateMachineIndex = layerField["data"]["m_StateMachineIndex"].AsUInt;
        var stateMachineField = stateMachineArray[(int)stateMachineIndex];
        var defaultStateIndex = (int)stateMachineField["data"]["m_DefaultState"].AsUInt;
        var statesArray = stateMachineField["data"]["m_StateConstantArray"]["Array"];
        var stateField = statesArray[defaultStateIndex];
        var stateID = stateField["data"]["m_FullPathID"].AsUInt;
        var stateMachineNode = behavioursNodes[stateID];
        while (stateMachineNode.Parent is not null)
        {
            stateMachineNode = stateMachineNode.Parent;
        }

        foreach (var syncedBehaviour in syncedBehaviours)
        {
            var stateMachinePath = string.IsNullOrEmpty(syncedBehaviour.StateMachinePath) ? stateMachineNode.FullPath : syncedBehaviour.StateMachinePath;
            var stateFullPathID = GetOrAddName($"{stateMachinePath}.{syncedBehaviour.StateName}");
            var stateNode = behavioursNodes[stateFullPathID];
            foreach (var behaviour in syncedBehaviour.Behaviours)
            {
                if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
                {
                    behaviourIndex = (uint)behaviours.Count;
                    TryAddDependency(behaviour, out var fileID, out var pathID);
                    behaviours.Add((fileID, pathID));
                }
                stateNode.Behaviours.Add(behaviourIndex);
            }
        }
    }

    /// <summary>
    /// Fills BehaviourNodes in a new StateMachine with Behaviours
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="stateMachine"></param>
    /// <param name="behaviours"></param>
    /// <param name="behavioursNodes"></param>
    /// <param name="addedBehaviours"></param>
    private void FillStateMachineBehaviours(string stateMachinePath, StateMachine stateMachine, List<(int, long)> behaviours, Dictionary<uint, BehavioursNode> behavioursNodes, Dictionary<StateMachineBehaviour, uint> addedBehaviours)
    {
        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";
        var stateMachineID = GetOrAddName(newStateMachinePath);
        var stateMachineNode = behavioursNodes[stateMachineID];

        foreach (var behaviour in stateMachine.Behaviours)
        {
            if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
            {
                behaviourIndex = (uint)behaviours.Count;
                if (!TryAddDependency(behaviour, out var fileID, out var pathID))
                {
                    LogError($"Behaviour for a StateMachine \"{stateMachineNode.FullPath}\". Make sure it was loaded from an AssetBundle.");
                    continue;
                }
                behaviours.Add((fileID, pathID));
            }
            stateMachineNode.Behaviours.Add(behaviourIndex);
        }

        foreach (var state in stateMachine.States)
        {
            var stateID = GetOrAddName($"{newStateMachinePath}.{state.Name}");
            var stateNode = behavioursNodes[stateID];
            foreach (var behaviour in state.Behaviours)
            {
                if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
                {
                    behaviourIndex = (uint)behaviours.Count;
                    if (!TryAddDependency(behaviour, out var fileID, out var pathID))
                    {
                        LogError($"Behaviour for a State \"{stateNode.FullPath}\". Make sure it was loaded from an AssetBundle.");
                        continue;
                    }
                    behaviours.Add((fileID, pathID));
                }
                stateNode.Behaviours.Add(behaviourIndex);
            }
        }

        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            FillStateMachineBehaviours(newStateMachinePath, subStateMachine, behaviours, behavioursNodes, addedBehaviours);
        }
    }

    /// <summary>
    /// Fills BehaviourNodes in an existing StateMachine with new Behaviours
    /// </summary>
    /// <param name="stateMachinePath"></param>
    /// <param name="stateMachine"></param>
    /// <param name="behaviours"></param>
    /// <param name="behavioursNodes"></param>
    /// <param name="addedBehaviours"></param>
    private void FillExistingStateMachineBehaviours(string stateMachinePath, ExistingStateMachine stateMachine, List<(int, long)> behaviours, Dictionary<uint, BehavioursNode> behavioursNodes, Dictionary<StateMachineBehaviour, uint> addedBehaviours)
    {
        var newStateMachinePath = string.IsNullOrEmpty(stateMachinePath) ? stateMachine.Name : $"{stateMachinePath}.{stateMachine.Name}";
        var stateMachineID = GetOrAddName(newStateMachinePath);
        var stateMachineNode = behavioursNodes[stateMachineID];

        foreach (var behaviour in stateMachine.NewBehaviours)
        {
            if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
            {
                behaviourIndex = (uint)behaviours.Count;
                if (!TryAddDependency(behaviour, out var fileID, out var pathID))
                {
                    LogError($"Behaviour for a StateMachine \"{stateMachineNode.FullPath}\". Make sure it was loaded from an AssetBundle.");
                    continue;
                }
                behaviours.Add((fileID, pathID));
            }
            stateMachineNode.Behaviours.Add(behaviourIndex);
        }

        foreach (var state in stateMachine.States)
        {
            var stateID = GetOrAddName($"{newStateMachinePath}.{state.Name}");
            var stateNode = behavioursNodes[stateID];
            foreach (var behaviour in state.NewBehaviours)
            {
                if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
                {
                    behaviourIndex = (uint)behaviours.Count;
                    if (!TryAddDependency(behaviour, out var fileID, out var pathID))
                    {
                        LogError($"Behaviour for a State \"{stateNode.FullPath}\". Make sure it was loaded from an AssetBundle.");
                        continue;
                    }
                    behaviours.Add((fileID, pathID));
                }
                stateNode.Behaviours.Add(behaviourIndex);
            }
        }

        foreach (var state in stateMachine.NewStates)
        {
            var stateID = GetOrAddName($"{newStateMachinePath}.{state.Name}");
            var stateNode = behavioursNodes[stateID];
            foreach (var behaviour in state.Behaviours)
            {
                if (!addedBehaviours.TryGetValue(behaviour, out var behaviourIndex))
                {
                    behaviourIndex = (uint)behaviours.Count;
                    if (!TryAddDependency(behaviour, out var fileID, out var pathID))
                    {
                        LogError($"Behaviour for a State \"{stateNode.FullPath}\". Make sure it was loaded from an AssetBundle.");
                        continue;
                    }
                    behaviours.Add((fileID, pathID));
                }
                stateNode.Behaviours.Add(behaviourIndex);
            }
        }

        foreach (var subStateMachine in stateMachine.SubStateMachines)
        {
            FillExistingStateMachineBehaviours(newStateMachinePath, subStateMachine, behaviours, behavioursNodes, addedBehaviours);
        }

        foreach (var subStateMachine in stateMachine.NewSubStateMachines)
        {
            FillStateMachineBehaviours(newStateMachinePath, subStateMachine, behaviours, behavioursNodes, addedBehaviours);
        }
    }

    private class BehavioursNode
    {
        public uint FullPathID;
        public string FullPath;
        public bool IsState;
        public BehavioursNode Parent;
        public List<uint> Behaviours = [];
    }
}
