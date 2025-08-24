using BepInEx;
using HG.Coroutines;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
                if(_AddressFailedToLoad)
                {
                    AddressablesPlugin.Logger.LogWarning($"Not trying to load {this} because it's address has already failed to load beforehand. Null will be returned.");
                }
                else
                {
                    LoadFromAddress();
                }
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
    /// <inheritdoc cref="AddressReferencedAsset.BoxedAsset"/>
    /// </summary>
    public override UObject BoxedAsset => _asset;

    /// <summary>
    /// <inheritdoc cref="AddressReferencedAsset.AsyncOperationHandle"/>
    /// </summary>
    public new AsyncOperationHandle<T> AsyncOperationHandle
    {
        get
        {
            return _asyncOperationHandle;
        }
        protected set
        {
            _asyncOperationHandle = value;
            base.AsyncOperationHandle = value;
        }
    }
    private AsyncOperationHandle<T> _asyncOperationHandle;

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
            _AddressFailedToLoad = false;

            //Release the handle
            if(_asyncOperationHandle.IsValid())
            {
                Addressables.Release(_asyncOperationHandle);
            }

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
    public virtual bool CanLoadFromCatalog { get; protected set; } = false;

    private bool _AddressFailedToLoad;

    private bool IsValidForLoadingWithAddress()
    {
        return !_asset && !string.IsNullOrEmpty(_address);
    }

    [Obsolete("Call \"LoadAssetAsyncCoroutine()\" instead.")]
    protected sealed override async Task LoadAssetAsync()
    {
        if(IsValidForLoadingWithAddress())
        {
            await LoadAsync();
        }
    }

    /// <summary>
    /// Loads the asset asynchronously with a coroutine.
    /// </summary>
    /// <returns>A Coroutine, which can be awaited</returns>
    protected sealed override IEnumerator LoadAssetAsyncCoroutine()
    {
        if(IsValidForLoadingWithAddress())
        {
            var coroutine = LoadAsyncCoroutine();
            while(coroutine.MoveNext())
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Loads the asset immediatly, instead of awaiting for <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/>
    /// </summary>
    /// <returns>The loaded asset, or null if no asset was found.</returns>
    public T LoadAssetNow()
    {
        if(!AssetExists)
            Load();

        return Asset;
    }

    /// <summary>
    /// Allows you to Resolve the asset immediatly using a coroutine, instead of awaiting for <see cref="AddressReferencedAsset.OnAddressReferencedAssetsLoaded"/>.
    /// <br></br>
    /// If <see cref="CanLoadFromCatalog"/> is true, then you should at the very least await for said asset's catalog to initialize, otherwise null might return.
    /// <br></br>
    /// If you want to immediatly load the asset, you can do so by calling LoadAssetNow
    /// </summary>
    /// <param name="onLoaded">An action to execute once the asset is loaded.</param>
    /// <returns>Yield returns null until the asset is loaded, afterwards it returns </returns>
    public virtual IEnumerator LoadAssetNowCoroutine(Action<T> onLoaded)
    {
        if(!AssetExists)
        {
            var loadCoroutine = LoadAsyncCoroutine();
            while(loadCoroutine.MoveNext())
            {
                yield return null;
            }
        }

        onLoaded?.Invoke(Asset);
        yield break;
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
    /// <returns></returns>
    protected virtual IEnumerator LoadAsyncCoroutine()
    {
        var loadCoroutine = LoadFromAddressAsyncCoroutine();
        while(loadCoroutine.MoveNext())
        {
            yield return null;
        }
    }

    [Obsolete("Use \"LoadAsyncCoroutine()\" Instead")]
    protected virtual async Task LoadAsync()
    {
        await LoadFromAddressAsync();
    }

    /// <summary>
    /// Loads the Asset asynchronously via <see cref="Addressables"/>
    /// </summary>
    protected IEnumerator LoadFromAddressAsyncCoroutine()
    {
        bool? result = null;
        IEnumerator<bool?> addressValidCoroutine = IsAdressValidAsync();
        while(result != null && addressValidCoroutine.MoveNext())
        {
            result = addressValidCoroutine.Current;
        }

        result ??= false;
        if(result == false)
        {
            AddressablesPlugin.Logger.LogWarning($"{this} failed to load from it's address because the address is either invalid, or malformed.");
            _AddressFailedToLoad = true;
        }

        AsyncOperationHandle = Addressables.LoadAssetAsync<T>(_address);
        while(!AsyncOperationHandle.IsDone)
        {
            yield return null;
        }
        _asset = AsyncOperationHandle.Result;
    }

    [Obsolete("Use \"LoadFromAdressAsyncCoroutine\" Instead")]
    protected async Task LoadFromAddressAsync()
    {
        bool? result = null;
        IEnumerator<bool?> coroutine = IsAdressValidAsync();
        while(result != null)
        {
            if(coroutine.MoveNext())
            {
                result = coroutine.Current;
            }
            break;
        }

        if(result == false)
        {
            AddressablesPlugin.Logger.LogWarning($"{this} failed to load from it's address because the address is either invalid, or malformed.");
            _AddressFailedToLoad = true;
        }

        AsyncOperationHandle = Addressables.LoadAssetAsync<T>(_address);
        var task = AsyncOperationHandle.Task;
        _asset = await task;
    }

    /// <summary>
    /// Loads the Asset synchronously via <see cref="Addressables"/>
    /// </summary>
    protected void LoadFromAddress()
    {
        if(!IsAddressValid())
        {
            AddressablesPlugin.Logger.LogWarning($"{this} failed to load from it's address because the address is either invalid, or malformed.");
            _AddressFailedToLoad = true;
            return;
        }
        AsyncOperationHandle = Addressables.LoadAssetAsync<T>(_address);
        _asset = AsyncOperationHandle.WaitForCompletion();
    }

    private bool IsAddressValid()
    {
        var location = Addressables.LoadResourceLocationsAsync(_address).WaitForCompletion();

        AddressablesPlugin.Logger.LogFatal($"Location for address {Address} exists?: {location.Any()}");
        return location.Any();
    }

    private IEnumerator<bool?> IsAdressValidAsync()
    {
        var locationTask = Addressables.LoadResourceLocationsAsync(_address);
        while(!locationTask.IsDone)
        {
            yield return null;
        }

        var result = locationTask.Result;
        yield return result.Any();
    }

    /// <summary>
    /// Returns a human readable representation of this AddressReferencedAsset
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{GetType().Name}(Asset={(_asset ? _asset : "null")}.Address={Address}";
    }

    /// <summary>
    /// Calls <see cref="Addressables.Release{TObject}(AsyncOperationHandle{TObject})"/> on <see cref="AsyncOperationHandle"/>
    /// </summary>
    public override void Dispose()
    {
        if (AsyncOperationHandle.IsValid())
        {
            Addressables.Release(AsyncOperationHandle);
        }
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
        _useDirectReference = true;
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
public abstract class AddressReferencedAsset : IDisposable
{
    protected static readonly HashSet<AddressReferencedAsset> instances = new();

    /// <summary>
    /// The asset loaded, boxed inside a regular unity object.
    /// </summary>
    public abstract UObject BoxedAsset { get; }

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
    /// An exposure to the internal AsyncOperationHandle, this operation handle is only valid if the object was loaded via an address.
    /// </summary>
    public AsyncOperationHandle AsyncOperationHandle { get; protected set; }

    /// <summary>
    /// Sets hooks for the AddressReferencedSystem, any constructor from classes inheriting <see cref="AddressReferencedAsset"/> must call it.
    /// </summary>
    protected void SetHooks()
    {
        if(RoR2Application.loadFinished)
        {
            StartCoroutineOnLoad();
            return;
        }
        RoR2Application.onLoad -= StartCoroutineOnLoad;
        RoR2Application.onLoad += StartCoroutineOnLoad;
    }

    /// <summary>
    /// Unsets hooks for the AddressReferencedSystem, any deconstructor from classes inheriting <see cref="AddressReferencedAsset"/> must call it IF <see cref="instances"/>'s count is 0
    /// </summary>
    protected void UnsetHooks()
    {
        if (!RoR2Application.loadFinished)
            RoR2Application.onLoad -= StartCoroutineOnLoad;
    }

    private void StartCoroutineOnLoad()
    {
        AddressablesPlugin.Instance.StartCoroutine(LoadReferencesAsync());
    }

    private static IEnumerator LoadReferencesAsync()
    {
        ParallelCoroutine parallelCoroutine = new ParallelCoroutine();
        foreach(var instance in instances)
        {
            if(instance.BoxedAsset)
            {
                continue;
            }

            parallelCoroutine.Add(instance.LoadAssetAsyncCoroutine());
        }

        while(parallelCoroutine.MoveNext())
        {
            yield return null;
        }

        //Backwards compat for the task version.
        List<Task> tasks = new List<Task>();
        foreach(AddressReferencedAsset instance in instances)
        {
            if(instance.BoxedAsset)
            {
                continue;
            }

            tasks.Add(instance.LoadAssetAsync());
        }

        var supertask = Task.WhenAll(tasks);
        while(!supertask.IsCompleted)
        {
            yield return null;
        }

        _initialized = true;
        OnAddressReferencedAssetsLoaded?.Invoke();
    }

    [Obsolete("If you need to implement this, implement a method that just returns Task.CompeltedTask, loading is now done via LoadAssetAsyncCoroutine instead.")]
    protected abstract Task LoadAssetAsync();

    /// <summary>
    /// Implement how the asset is loaded asynchronously using a coroutine
    /// </summary>
    protected abstract IEnumerator LoadAssetAsyncCoroutine();

#pragma warning disable R2APISubmodulesAnalyzer
    /// <summary>
    /// Calls <see cref="Addressables.Release{TObject}(AsyncOperationHandle{TObject})"/> on <see cref="AsyncOperationHandle"/>
    /// </summary>
    public virtual void Dispose()
    {
        if(AsyncOperationHandle.IsValid())
        {
            Addressables.Release(AsyncOperationHandle);
        }
    }
#pragma warning restore R2APISubmodulesAnalyzer
}
