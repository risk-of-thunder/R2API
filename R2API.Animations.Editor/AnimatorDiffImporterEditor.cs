using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace R2API.Animations.Editor;
[CustomEditor(typeof(AnimatorDiffImporter))]
public class AnimatorDiffImporterEditor : ScriptedImporterEditor
{
    public override Type extraDataType => typeof(AnimatorDiffImporterExtra);
    private SerializedProperty modifiedControllerProperty;

    public override void InitializeExtraDataInstance(UnityEngine.Object extraData, int targetIndex)
    {
        var extra = extraData as AnimatorDiffImporterExtra;
        var target = targets[targetIndex] as AnimatorDiffImporter;
        if (!string.IsNullOrEmpty(target.modifiedControllerGuid))
        {
            extra.modifiedController = AssetDatabase.LoadMainAssetAtGUID(new GUID(target.modifiedControllerGuid)) as AnimatorController;
        }
        else
        {
            extra.modifiedController = null;
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        modifiedControllerProperty = extraDataSerializedObject.FindProperty(nameof(AnimatorDiffImporterExtra.modifiedController));
    }

    public override bool CanOpenMultipleObjects()
    {
        return false;
    }

    public override void OnInspectorGUI()
    {
        extraDataSerializedObject.Update();

        EditorGUILayout.PropertyField(modifiedControllerProperty);

        extraDataSerializedObject.ApplyModifiedPropertiesWithoutUndo();
        ApplyRevertGUI();
    }

    public override void Apply()
    {
        var guidProperty = serializedObject.FindProperty(nameof(AnimatorDiffImporter.modifiedControllerGuid));
        var extra = extraDataTarget as AnimatorDiffImporterExtra;
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(extra.modifiedController, out var guid, out long localId))
        {
            guidProperty.stringValue = guid;
        }
        else
        {
            guidProperty.stringValue = null;
        }

        base.Apply();
    }
}

public class AnimatorDiffImporterExtra : ScriptableObject
{
    public AnimatorController modifiedController;
}
