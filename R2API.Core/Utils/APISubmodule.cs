using BepInEx.Logging;
using JetBrains.Annotations;
using Mono.Cecil;
using System;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace R2API.Utils;

/// <summary>
/// Attribute to have at the top of your BaseUnityPlugin class if you want to load a specific R2API Submodule.
/// Parameter(s) are the nameof the submodules.
/// e.g: [R2APISubmoduleDependency("SurvivorAPI", "ItemAPI")]
/// </summary>
[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
#pragma warning disable CS0618 // Type or member is obsolete
[Obsolete(AttributeObsolete, false)]
#pragma warning restore CS0618 // Type or member is obsolete
public class R2APISubmoduleDependency : Attribute
{
    public const string AttributeObsolete = "All submodules are automatically loaded and this attribute is now unused.";
    public const string PropertyObsolete = "All submodules are automatically loaded and this property is now unused";

    public string?[]? SubmoduleNames { get; }

    public R2APISubmoduleDependency(params string[] submoduleName)
    {
        SubmoduleNames = submoduleName;
    }
}
