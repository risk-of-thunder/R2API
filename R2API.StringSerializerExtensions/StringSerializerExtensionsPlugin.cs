using BepInEx;
using BepInEx.Logging;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace R2API;

[BepInPlugin(StringSerializerExtensions.PluginGUID, StringSerializerExtensions.PluginName, StringSerializerExtensions.PluginVersion)]
internal sealed class StringSerializerExtensionsPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger { get; set; }

    private void Awake()
    {
        Logger = base.Logger;
    }

#if DEBUG
    EntityStateConfiguration _modifiedESC;
#endif
    private void OnEnable()
    {
        StringSerializerExtensions.ApplyHooks();
        //Switch the MinePod's teleport state to use direct reference on the entity state game object fields to test the ESGO Direct Reference Serialization/Deserialization
        //This part tests our custom "SetValue" hook, ingame we can test our custom "GetValue" hook
#if DEBUG
        Addressables.LoadAssetAsync<EntityStateConfiguration>("7ff7eab3cfbcb3a40993b9c555509a4b").Completed += (handle) =>
        {
            Logger.LogMessage("Modifying MinePod.Teleport to use direct reference ESGO");
            _modifiedESC = handle.Result;
            if (!_modifiedESC)
                return;


            List<string> addressesToRemoveFromPreload = new List<string>();

            Type stateType = typeof(EntityStates.BasicTeleport);

            var collection = _modifiedESC.serializedFieldsCollection;

            FieldInfo arrivalEffectPrefabFieldInfo = stateType.GetField(nameof(EntityStates.BasicTeleport.arrivalEffectPrefab), (BindingFlags)(-1));
            ref var arrivalEffectPrefabSerializedField = ref collection.GetOrCreateField(nameof(EntityStates.BasicTeleport.arrivalEffectPrefab));
            ref var arrivalEffectPrefabSerializedValue = ref arrivalEffectPrefabSerializedField.fieldValue;
            addressesToRemoveFromPreload.Add(arrivalEffectPrefabSerializedValue.stringValue);
            Logger.LogMessage($"Testing custom SetValue hook");
            arrivalEffectPrefabSerializedValue.SetValue(arrivalEffectPrefabFieldInfo, Addressables.LoadAssetAsync<GameObject>(arrivalEffectPrefabSerializedValue.stringValue).WaitForCompletion());

            FieldInfo teleportEffectPrefabFieldInfo = stateType.GetField(nameof(EntityStates.BasicTeleport.teleportEffectPrefab), (BindingFlags)(-1));
            ref var teleportEffectPrefabSerializedField = ref collection.GetOrCreateField(nameof(EntityStates.BasicTeleport.teleportEffectPrefab));
            ref var teleportEffectPrefabSerializedValue = ref teleportEffectPrefabSerializedField.fieldValue;
            addressesToRemoveFromPreload.Add(teleportEffectPrefabSerializedValue.stringValue);
            Logger.LogMessage($"Testing custom SetValue hook");
            teleportEffectPrefabSerializedValue.SetValue(teleportEffectPrefabFieldInfo, Addressables.LoadAssetAsync<GameObject>(teleportEffectPrefabSerializedValue.stringValue).WaitForCompletion());

            for (int i = _modifiedESC.preloadGUIDReferences.Length - 1; i >= 0; i--)
            {
                if (addressesToRemoveFromPreload.Contains(_modifiedESC.preloadGUIDReferences[i]))
                {
                    HG.ArrayUtils.ArrayRemoveAtAndResize(ref _modifiedESC.preloadGUIDReferences, i);
                }
            }
        };
#endif
    }

    private void OnDisable()
    {
        StringSerializerExtensions.UndoHooks();
    }

    private void OnDestroy()
    {
        StringSerializerExtensions.FreeHooks();
    }
}
