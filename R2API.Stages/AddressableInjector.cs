using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API;

/// <summary>
/// Component that injects an AddressableAsset to a component's field
/// </summary>
[ExecuteAlways]
public class AddressableInjector : MonoBehaviour
{
    [Tooltip("The address used for injecting")]
    public string address;
    /// <summary>
    /// The Loaded Asset
    /// </summary>
    public Object Asset { get => _asset; private set => _asset = value; }
    [NonSerialized] private Object _asset;

    [Tooltip("The component that will be injected")]
    [SerializeField] private Component targetComponent;
    [Tooltip("The member info that'll be injected")]
    [SerializeField] private string targetMemberInfoName;

    private MemberInfo cachedMemberInfo;

    private void OnEnable() => Refresh();

    /// <summary>
    /// Refreshes and re-injects the asset specified in <see cref="address"/>
    /// </summary>
    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrEmpty(address))
        {
            string msg = $"Invalid address in {this}, address is null, empty, or white space";
            Log.Warning(msg);
            return;
        }

        if (!targetComponent)
        {
            string msg = $"No Target Component Set in {this}";
            Log.Warning(msg);
            return;
        }

        if (string.IsNullOrEmpty(targetMemberInfoName) || string.IsNullOrWhiteSpace(targetMemberInfoName))
        {
            string msg = $"{this}'s targetMemberInfoName is null, empty or white space";
            Log.Warning(msg);
            return;
        }

        var memberInfo = GetMemberInfo();
        if (memberInfo == null)
        {
            string msg = $"{this} failed finding the MemberInfo to target based on the name \"{targetMemberInfoName}\". Target Component: {targetComponent}";
            Log.Warning(msg); ;
            return;
        }

        var _asset = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Object>(address).WaitForCompletion();
        if (!_asset)
            return;

        if (Application.isEditor)
            Asset = Instantiate(_asset);

        Asset.hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable | HideFlags.DontSaveInBuild;

        Inject(memberInfo);
    }

    private void Inject(MemberInfo memberInfo)
    {
        switch (memberInfo)
        {
            case PropertyInfo pInfo: InjectPropertyInfo(pInfo); break;
            case FieldInfo fInfo: InjectFieldInfo(fInfo); break;
        }

        void InjectPropertyInfo(PropertyInfo propertyInfo)
        {
            try
            {
                propertyInfo.SetValue(targetComponent, Asset);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            if (Application.isEditor)
            {
                Log.Info($"injected {Asset} onto {targetComponent}'s propertyInfo, setting propertyInfo value to null to avoid broken scenes/objects");
                propertyInfo.SetValue(targetComponent, null);
                DestroyImmediate(Asset);
            }
        }

        void InjectFieldInfo(FieldInfo fieldInfo)
        {
            try
            {
                fieldInfo.SetValue(targetComponent, Asset);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            if (Application.isEditor)
            {
                Log.Info($"injected {Asset} onto {targetComponent}'s fieldInfo, setting fieldInfo value to null to avoid broken scenes/objects");
                fieldInfo.SetValue(targetComponent, null);
                DestroyImmediate(Asset);
            }
        }
    }

    private MemberInfo GetMemberInfo()
    {
        if ((cachedMemberInfo == null || $"({cachedMemberInfo.DeclaringType.Name}) {cachedMemberInfo.Name}" != targetMemberInfoName) && targetComponent)
        {
            cachedMemberInfo = targetComponent.GetType()
                .GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(m =>
                {
                    string memberTypeName = m.GetType().Name;
                    return memberTypeName == "MonoProperty" || memberTypeName == "MonoField" || memberTypeName == "FieldInfo" || memberTypeName == "PropertyInfo";
                })
                .FirstOrDefault(m => $"({m.DeclaringType.Name}) {m.Name}" == targetMemberInfoName);
        }

        return cachedMemberInfo;
    }
    private void OnDisable() => RemoveReferencesEditor();
    private void RemoveReferencesEditor()
    {
        if (!Application.isEditor)
            return;

        var memberInfo = GetMemberInfo();

        switch (memberInfo)
        {
            case PropertyInfo pInfo:
                pInfo.SetValue(targetComponent, null);
                break;
            case FieldInfo fInfo:
                fInfo.SetValue(targetComponent, null);
                break;
        }
    }
}
