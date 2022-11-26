using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// ReSharper disable UnusedMember.Global

namespace R2API;

// ReSharper disable once InconsistentNaming
public static class OrbAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".orb";
    public const string PluginName = R2API.PluginName + ".Orb";
    public const string PluginVersion = "0.0.1";

    /// <summary>
    /// Return true if the submodule is loaded.
    /// </summary>
    [Obsolete(R2APISubmoduleDependency.propertyObsolete)]
    public static bool Loaded => true;

    private static bool _orbsAlreadyAdded = false;

    public static ObservableCollection<Type?>? OrbDefinitions = new ObservableCollection<Type?>();

    /// <summary>
    /// Adds an Orb to the orb catalog.
    /// This must be called during plugin Awake() or OnEnable().
    /// The type must be a subclass of RoR2.Orbs.Orb
    /// </summary>
    /// <param name="t">The type of the orb being added</param>
    /// <returns>True if orb will be added</returns>
    public static bool AddOrb(Type? t)
    {
        OrbAPI.SetHooks();
        if (_orbsAlreadyAdded)
        {
            R2API.Logger.LogError($"Tried to add Orb type: {nameof(t)} after orb catalog was generated");
            return false;
        }

        if (t == null || !t.IsSubclassOf(typeof(RoR2.Orbs.Orb)))
        {
            R2API.Logger.LogError($"Type: {nameof(t)} is null or not a subclass of RoR2.Orbs.Orb");
            return false;
        }

        OrbDefinitions.Add(t);

        return true;
    }

    /// <summary>
    /// Adds an Orb to the orb catalog.
    /// This must be called during pplpugin Awake() or OnEnable()
    /// The type of <typeparamref name="TOrb"/> must be a subclass of RoR2.Orbs.Orb
    /// </summary>
    /// <typeparam name="TOrb">The Type of orb being added</typeparam>
    /// <returns>True if the orb will be added</returns>
    public static bool AddOrb<TOrb>() where TOrb : RoR2.Orbs.Orb
    {
        OrbAPI.SetHooks();
        Type orbType = typeof(TOrb);
        if (_orbsAlreadyAdded)
        {
            R2API.Logger.LogError($"Tried to add Orb type: {orbType.Name} after orb catalog was generated");
            return false;
        }

        if (!orbType.IsSubclassOf(typeof(RoR2.Orbs.Orb)))
        {
            R2API.Logger.LogError($"Type: {orbType.Name} is not a subclass of RoR2.Orbs.Orb");
            return false;
        }

        OrbDefinitions.Add(orbType);
        return true;
    }

    private static bool _hooksEnabled = false;

    internal static void SetHooks()
    {
        if (_hooksEnabled)
        {
            return;
        }

        On.RoR2.Orbs.OrbCatalog.GenerateCatalog += AddOrbs;

        _hooksEnabled = true;
    }

    internal static void UnsetHooks()
    {
        On.RoR2.Orbs.OrbCatalog.GenerateCatalog -= AddOrbs;

        _hooksEnabled = false;
    }

    private static void AddOrbs(On.RoR2.Orbs.OrbCatalog.orig_GenerateCatalog orig)
    {
        _orbsAlreadyAdded = true;
        orig();

        var orbCat = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Type[]>("indexToType");
        var typeToIndex = typeof(RoR2.Orbs.OrbCatalog).GetFieldValue<Dictionary<Type, int>>("typeToIndex");

        int origLength = orbCat.Length;
        int extraLength = OrbDefinitions.Count;

        Array.Resize(ref orbCat, origLength + extraLength);

        for (int i = 0; i < extraLength; i++)
        {
            var temp = i + origLength;
            orbCat[temp] = OrbDefinitions[i];
            typeToIndex.Add(OrbDefinitions[i], temp);
        }

        typeof(RoR2.Orbs.OrbCatalog).SetFieldValue("indexToType", orbCat);
        typeof(RoR2.Orbs.OrbCatalog).SetFieldValue("typeToIndex", typeToIndex);
    }
}
