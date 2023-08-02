using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UObject = UnityEngine.Object;

namespace R2API.AddressReferencedAssets;

/// <summary>
/// A <see cref="AddressReferencedAsset"/> is a class that's used for referencing assets ingame.
/// <br></br>
/// <br>The asset referenced can either be a direct reference, or a reference via an address</br>
/// <br>Some <see cref="AddressReferencedAsset{T}"/> can load their assets via catalog, such as <see cref="AddressReferencedItemDef"/></br>
/// <br>An <see cref="AddressReferencedAsset{T}"/> has implicit operators for casting to it's <typeparamref name="T"/> Type, and for casting into a boolean. It also has implicit operators for encapsulating a <see cref="string"/> and <typeparamref name="T"/> inside an <see cref="AddressReferencedAsset{T}"/></br>
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public class AddressReferencedAsset<T> : AddressReferencedAsset where T : UObject
{
    /// <summary>
    /// The Asset of type <typeparamref name="T"/> contained by this <see cref="AddressReferencedAsset"/>
    /// <para><b>Get behaviour</b></para>
    /// <br>If the asset has not been loaded or is null, the asset is loaded synchronously via addressables.</br>
    /// <br>In the scenario where <see cref="CanLoadFromCatalog"/> is set to true, the asset itself is null, and <see cref="AddressReferencedAsset.Initialized"/> is false, a warning message is displayed, this is because the AddressReferencedAsset instance can use an asset name directly by loading it via the game's catalogues.</br>
    /// <para><b>Set Behaviour</b></para>
    /// <br>Setting this property sets the address to null if the provided value is not null.</br>
    /// </summary>
    public T Asset
    {
        get
        {
            /*
             * For future maintainers, some AddressReferencedAssets like AddressReferencedItem can load directly from the catalog.
             * As such, if the Asset is trying to be fetched, it doesnt use a direct reference, the system it not initalized, and it can load from catalog, we should warn the user they should wait until the system loads the asset. We will still try to load the asset synchronously regardless in case it loads from an address.
             * 
             * In other cases, we'll just load the asset immediatly.
             */
            if(!_asset && !Initialized && CanLoadFromCatalog)
            {
                string typeName = GetType().Name;
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrame(1).GetMethod();
                AddressablesPlugin.Logger.LogWarning($"Assembly {Assembly.GetCallingAssembly()} is trying to access an {typeName} before AddressReferencedAssets have initialized! This can cause issues as {typeName} can load the asset from Catalogs within the game." +
                    $"\n Consider using AddressReferencedAssets.OnAddressReferencedAssetsLoaded for running code that depends on AddressableAssets! (Method: {method.DeclaringType.FullName}.{method.Name}()");
                Load();
            }
            else if(IsValidForLoadingWithAddress())
            {
                LoadFromAddress();
            }

            return _asset;
        }
        set
        {
            _asset = value;
            _address = _asset ? string.Empty : _address;
            _useDirectReference = _asset;
        }
    }

    /// <summary>
    /// Determines wether <see cref="Asset"/>'s backing field has a value.
    /// </summary>
    public bool AssetExists
    {
        get
        {
            return _asset;
        }
    }
    [SerializeField] private T _asset;

    /// <summary>
    /// The string to use for loading the <see cref="Asset"/>
    /// <br>Setting this value sets the current <see cref="Asset"/> to null and <see cref="UseDirectReference"/> to false. if the game has already finished loading, the new Asset is loaded.</br>
    /// </summary>
    public string Address
    {
        get => _address;
        set
        {
            _address = value;
            _asset = null;
            _useDirectReference = false;
            if(RoR2Application.loadFinished)
            {
                Load();
            }
        }
    }
    [SerializeField] private string _address;

    /// <summary>
    /// Whether the AddressReferencedAsset is considered Invalid.
    /// <para>For an AddressReferencedAsset to be invalid it must have the following characteristics:</para>
    /// <br>A: <see cref="Asset"/>'s backing field value is null</br>
    /// <br>B: <see cref="Address"/>'s backing field value is null, empty or whitespace</br>
    /// </summary>
    public bool IsInvalid
    {
        get
        {
            return !_asset && (string.IsNullOrEmpty(_address) || string.IsNullOrWhiteSpace(_address));
        }
    }

    /// <summary>
    /// Wether this AddressReferencedAsset is using a DirectReference (<see cref="Asset"/> is not null) or an Address Reference (<see cref="Asset"/> is null)
    /// <br>Mainly used for Editor related scripts</br>
    /// </summary>
    public bool UseDirectReference => _useDirectReference;
    [SerializeField,HideInInspector] private bool _useDirectReference;

    /// <summary>
    /// Wether this AddressReferencedAsset can load an Asset using the game's catalogues.
    /// <br>If this is true, you're encouraged to wait for AddressReferencedAsset to initialize fully using <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/></br>
    /// </summary>
    public virtual bool CanLoadFromCatalog { get; } = false;

    private bool IsValidForLoadingWithAddress()
    {
        return !_asset && !string.IsNullOrEmpty(_address);
    }
    /// <summary>
    /// Loads the asset asynchronously if <see cref="Asset"/> is not null and <see cref="Address"/> is not null or empty.
    /// <br>This is automatically called by the AddressReferencedAsset system and should not be called manually.</br>
    /// </summary>
    protected sealed override async Task LoadAssetAsync()
    {
        if(IsValidForLoadingWithAddress())
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// Implement how the Asset of type <typeparamref name="T"/> is loaded synchronously when <see cref="Asset"/> is null
    /// </summary>
    protected virtual void Load()
    {
        LoadFromAddress();
    }

    /// <summary>
    /// Implement how the Asset of type <typeparamref name="T"/> is loaded asynchronously when <see cref="Asset"/> is null
    /// </summary>
    protected virtual async Task LoadAsync()
    {
        await LoadFromAddressAsync();
    }

    /// <summary>
    /// Loads the Asset asynchronously via <see cref="Addressables"/>
    /// </summary>
    protected async Task LoadFromAddressAsync()
    {
        var task = Addressables.LoadAssetAsync<T>(_address).Task;
        _asset = await task;
    }

    /// <summary>
    /// Loads the Asset synchronously via <see cref="Addressables"/>
    /// </summary>
    protected void LoadFromAddress()
    {
        _asset = Addressables.LoadAssetAsync<T>(_address).WaitForCompletion();
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedAsset{T}"/> to a boolean value
    /// <br>Allows you to keep using the unity Syntax for checking if an object exists.</br>
    /// </summary>
    public static implicit operator bool(AddressReferencedAsset<T> addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for casting <see cref="AddressReferencedAsset{T}"/> to it's currently loaded <see cref="Asset"/> value
    /// </summary>
    public static implicit operator T(AddressReferencedAsset<T> addressReferencedAsset)
    {
        return addressReferencedAsset?.Asset;
    }

    /// <summary>
    /// Operator for encapsulating a <see cref="string"/> inside an <see cref="AddressReferencedAsset{T}"/>
    /// </summary>
    public static implicit operator AddressReferencedAsset<T>(string address)
    {
        return new AddressReferencedAsset<T>(address);
    }

    /// <summary>
    /// Operator for encapsulating an asset of type <typeparamref name="T"/> inside an <see cref="AddressReferencedAsset{T}"/>
    /// </summary>
    public static implicit operator AddressReferencedAsset<T>(T asset)
    {
        return new AddressReferencedAsset<T>(asset);
    }

    /// <summary>
    /// Constructor for <see cref="AddressReferencedAsset{T}"/>
    /// <br><see cref="Asset"/> will be set to <paramref name="asset"/> and <see cref="UseDirectReference"/> to true</br>
    /// </summary>
    public AddressReferencedAsset(T asset)
    {
        SetHooks();
        _asset = asset;
        _useDirectReference = false;
        instances.Add(this);
    }

    /// <summary>
    /// Constructor for <see cref="AddressReferencedAsset{T}"/>
    /// <br><see cref="Address"/> will be set to <paramref name="address"/> and <see cref="UseDirectReference"/> to false</br>
    /// </summary>
    public AddressReferencedAsset(string address)
    {
        SetHooks();
        this._address = address;
        _useDirectReference = false;
        instances.Add(this);
    }

    /// <summary>
    /// Parameterless Constructor for <see cref="AddressReferencedAsset{T}"/>
    /// </summary>
    public AddressReferencedAsset()
    {
        SetHooks();
        instances.Add(this);
    }

    /// <summary>
    /// <see cref="AddressReferencedAsset{T}"/> Deconstructor
    /// </summary>
    ~AddressReferencedAsset()
    {
        instances.Remove(this);
        if (instances.Count == 0)
            UnsetHooks();
    }
}

/// <summary>
/// A <see cref="AddressReferencedAsset"/> is a class that's used for referencing assets ingame.
/// <br>You're strongly adviced to use <see cref="AddressReferencedAsset{T}"/> instead.</br> 
/// </summary>
public abstract class AddressReferencedAsset
{
    protected static readonly HashSet<AddressReferencedAsset> instances = new();

    /// <summary>
    /// Wether or not the <see cref="AddressReferencedAsset"/> system has initialized.
    /// <br>Particularly useful for interacting with <see cref="AddressReferencedAsset{T}"/> who's <see cref="AddressReferencedAsset{T}.CanLoadFromCatalog"/> is true, such as <see cref="AddressReferencedItemDef"/></br>
    /// </summary>
    public static bool Initialized { get => _initialized; }
    private static bool _initialized;

    /// <summary>
    /// An event that gets invoked when all the AddressReferencedAssets have been loaded.
    /// </summary>

    public static event Action OnAddressReferencedAssetsLoaded;

    /// <summary>
    /// Sets hooks for the AddressReferencedSystem, any constructor from classes inheriting <see cref="AddressReferencedAsset"/> must call it.
    /// </summary>
    protected void SetHooks()
    {
        if(RoR2Application.loadFinished)
        {
            LoadReferencesAsync();
            return;
        }
        RoR2Application.onLoad -= LoadReferencesAsync;
        RoR2Application.onLoad += LoadReferencesAsync;
    }

    /// <summary>
    /// Unsets hooks for the AddressReferencedSystem, any deconstructor from classes inheriting <see cref="AddressReferencedAsset"/> must call it IF <see cref="instances"/>'s count is 0
    /// </summary>
    protected void UnsetHooks()
    {
        if (!RoR2Application.loadFinished)
            RoR2Application.onLoad -= LoadReferencesAsync;
    }

    private static async void LoadReferencesAsync()
    {
        foreach(AddressReferencedAsset instance in instances)
        {
            try
            {
                await instance.LoadAssetAsync();
            }
            catch(Exception e)
            {
                AddressablesPlugin.Logger.LogError(e);
            }
        }
        _initialized = true;
        OnAddressReferencedAssetsLoaded?.Invoke();
    }
    protected abstract Task LoadAssetAsync();
}
