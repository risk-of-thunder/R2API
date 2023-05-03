using RoR2;
using UnityEngine;

namespace R2API.ScriptableObjects;

[CreateAssetMenu(fileName = "new SerializableDifficultyDef", menuName = "R2API/SerializableDifficultyDef")]
public class SerializableDifficultyDef : ScriptableObject
{
    [Tooltip("Scaling value of the difficulty, Drizzle is 1, Rainstorm is 2, Monsoon is 3")]
    public float scalingValue;
    public string descriptionToken;
    public string nameToken;
    [Tooltip("Unique identifier for this Difficulty")]
    public string serverTag;
    [Tooltip("If true, beating the game on this difficulty will unlock the survivor's Mastery skin")]
    public bool countsAsHardMode;
    [Tooltip("If set to true, the difficulty index will be a possitive number, this causes the difficulty to have all the Eclipse modifiers (From 1 to 8)")]
    public bool preferPositiveIndex = false;
    [Tooltip("If set to true, the Difficulty will not be selectable on the lobby, and will be hidden.")]
    public bool hideFromDifficultySelection = false;
    public Color difficultyColor;
    public Sprite iconSprite;

    public DifficultyDef DifficultyDef { get; private set; }
    public DifficultyIndex DifficultyIndex { get; internal set; }

    internal void CreateDifficultyDef()
    {
        DifficultyDef = new DifficultyDef(scalingValue, nameToken, string.Empty, descriptionToken, difficultyColor, serverTag, countsAsHardMode);
        DifficultyDef.foundIconSprite = true;
        DifficultyDef.iconSprite = iconSprite;
    }
}
