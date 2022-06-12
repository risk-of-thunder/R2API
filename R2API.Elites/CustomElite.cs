using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// Class that defines a custom Elite type for use in the game
/// All Elites consist of an <see cref="RoR2.EliteDef"/>, an <see cref="EquipmentDef"/>
/// and a <see cref="BuffDef"/>.
/// An Elite can only spawn if its in one of the available <see cref="CombatDirector.eliteTiers"/> (vanilla or custom).
/// Please check the constructors docs for more information.
/// </summary>
public class CustomElite
{

    /// <summary>
    /// Elite definition
    /// </summary>
    public EliteDef? EliteDef;

    /// <summary>
    /// The TextureRamp for this elite, can be omitted
    /// </summary>
    public Texture2D? EliteRamp;

    /// <summary>
    /// Elite tier(s) that the eliteDef will be on.
    /// </summary>
    public IEnumerable<CombatDirector.EliteTierDef> EliteTierDefs;

    /// <summary>
    /// You can omit giving a value to <see cref="EliteDef.eliteIndex"/>, as it'll be filled in automatically by the game.
    /// For your custom elite to spawn, you need to provide an enumerable of <see cref="CombatDirector.EliteTierDef"/> as second parameter.
    /// The API will then add your <see cref="EliteDef"/> in them.
    /// You can also make a totally new tier, by either
    /// directly modifying the array through <see cref="EliteAPI.GetCombatDirectorEliteTiers"/> and <see cref="EliteAPI.OverrideCombatDirectorEliteTiers"/>
    /// or by using <see cref="EliteAPI.AddCustomEliteTier(CombatDirector.EliteTierDef?)"/>
    /// </summary>
    public CustomElite(string? name, EquipmentDef equipmentDef, Color32 color, string? modifierToken, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs)
    {
        EliteDef = ScriptableObject.CreateInstance<EliteDef>();
        EliteDef.name = name;
        EliteDef.eliteEquipmentDef = equipmentDef;
        EliteDef.color = color;
        EliteDef.modifierToken = modifierToken;
        EliteTierDefs = eliteTierDefs;
    }

    /// <inheritdoc cref="CustomElite(string?, EquipmentDef, Color32, string?, IEnumerable{CombatDirector.EliteTierDef})"/>
    public CustomElite(string? name, EquipmentDef equipmentDef, Color32 color, string? modifierToken, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs, Texture2D eliteRamp)
    {
        EliteDef = ScriptableObject.CreateInstance<EliteDef>();
        EliteDef.name = name;
        EliteDef.eliteEquipmentDef = equipmentDef;
        EliteDef.color = color;
        EliteDef.modifierToken = modifierToken;
        EliteTierDefs = eliteTierDefs;
        EliteRamp = eliteRamp;
    }

    /// <inheritdoc cref="CustomElite(string?, EquipmentDef, Color32, string?, IEnumerable{CombatDirector.EliteTierDef})"/>
    public CustomElite(EliteDef? eliteDef, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs)
    {
        EliteDef = eliteDef;
        EliteTierDefs = eliteTierDefs;
    }
    /// <inheritdoc cref="CustomElite(string?, EquipmentDef, Color32, string?, IEnumerable{CombatDirector.EliteTierDef})"/>
    public CustomElite(EliteDef? eliteDef, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs, Texture2D eliteRamp)
    {
        EliteDef = eliteDef;
        EliteTierDefs = eliteTierDefs;
        EliteRamp = eliteRamp;
    }
}
