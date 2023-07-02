using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.AutoVersionGen;
using R2API.Utils;
using RoR2;
using UnityEngine;

namespace R2API;

#pragma warning disable CS0436 // Type conflicts with imported type
[AutoVersion]
#pragma warning restore CS0436 // Type conflicts with imported type
public static partial class StagesAPI
{
    public const string PluginGUID = R2API.PluginGUID + ".stages";
    public const string PluginName = R2API.PluginName + ".Stages";

#pragma warning disable CS0618 // Type or member is obsolete
    [Obsolete(R2APISubmoduleDependency.PropertyObsolete)]
#pragma warning restore CS0618 // Type or member is obsolete
    public static bool Loaded => true;
}
