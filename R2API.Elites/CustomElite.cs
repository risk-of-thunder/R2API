using System.Collections.Generic;
using RoR2;
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
    public EliteDef EliteDef;

    /// <summary>
    /// The TextureRamp for this elite, can be omitted
    /// </summary>
    public Texture2D? EliteRamp;

    /// <summary>
    /// Elite tier(s) that the eliteDef will be on.
    /// </summary>
    public IEnumerable<CombatDirector.EliteTierDef> EliteTierDefs;

    /// <summary>
    /// 
    /// </summary>
    public CustomElite(string name, EquipmentDef equipmentDef, Color32 color, string modifierToken, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs)
        : this(name, equipmentDef, color, modifierToken, eliteTierDefs, eliteRamp: null) { }

    /// <summary>
    /// Use <see cref="EliteAPI.Add(CustomElite?)"/> to add your <see cref="EliteDef"/> to the <see cref="EliteCatalog"/> during startup,
    /// along with adding it to all the given <see cref="EliteTierDefs"/>.
    /// <para>
    /// It's recommended that you use <see cref="EliteAPI.GetEliteTierEnumerable(VanillaEliteTier)"/> or <see cref="EliteAPI.GetHonorEliteTierEnumerable(VanillaEliteTier)"/>
    /// if you intend on adding this elite to any vanilla tiers to ensure correct tier placement, with custom tiers added as needed.
    /// </para>
    /// </summary>
    /// <param name="name">Internal <see cref="Object.name"/> for the <see cref="EliteDef"/></param>
    /// <param name="equipmentDef">Elite affix equipment</param>
    /// <param name="color">Elite base color</param>
    /// <param name="modifierToken">Token for the text before a characters name</param>
    /// <param name="eliteTierDefs">List of all tiers that can spawn this elite.</param>
    /// <param name="eliteRamp">Color ramp to be used in <see cref="EliteRamp"/>. Can be null.</param>
    public CustomElite(string name, EquipmentDef equipmentDef, Color32 color, string modifierToken, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs, Texture2D? eliteRamp)
    {
        EliteDef = ScriptableObject.CreateInstance<EliteDef>();
        EliteDef.name = name;
        EliteDef.eliteEquipmentDef = equipmentDef;
        EliteDef.color = color;
        EliteDef.modifierToken = modifierToken;
        EliteTierDefs = eliteTierDefs ?? [];
        EliteRamp = eliteRamp;
    }

    /// <inheritdoc cref="CustomElite(EliteDef, IEnumerable{CombatDirector.EliteTierDef}?, Texture2D)"/>
    public CustomElite(EliteDef eliteDef, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs) : this(eliteDef, eliteTierDefs, eliteRamp: null) { }
    /// <summary>
    /// <para>You can omit giving a value to <see cref="EliteDef.eliteIndex"/>, as it'll be filled in automatically by the game.</para>
    /// <inheritdoc cref="CustomElite(string, EquipmentDef, Color32, string, IEnumerable{CombatDirector.EliteTierDef}, Texture2D?)"/>
    /// </summary>
    /// <param name="eliteDef"></param>
    /// <param name="eliteTierDefs">List of <see cref="CombatDirector.EliteTierDef"/> the <see cref="EliteDef"/> can spawn in.</param>
    /// <param name="eliteRamp">Color ramp to be used in <see cref="EliteRamp"/>. Can be null.</param>
    public CustomElite(EliteDef eliteDef, IEnumerable<CombatDirector.EliteTierDef> eliteTierDefs, Texture2D? eliteRamp)
    {
        EliteDef = eliteDef;
        EliteTierDefs = eliteTierDefs ?? [];
        EliteRamp = eliteRamp;
    }
}
