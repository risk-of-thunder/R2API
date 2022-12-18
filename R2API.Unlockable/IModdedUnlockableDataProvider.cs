using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API;

/// <summary>
/// Interface used to provide the metadata needed to register an achievement + unlockable
/// </summary>
[Obsolete(UnlockableAPI.ObsoleteMessage)]
public interface IModdedUnlockableDataProvider
{

    /// <summary>
    /// The identifier of the achievement being added.
    /// Should be unique
    /// </summary>
    string AchievementIdentifier { get; }

    /// <summary>
    /// The identifier of the unlockable granted when the achievement is completed.
    /// Should be unique.
    /// This is what is used when specifying an unlock condition for various things in the game
    /// </summary>
    string UnlockableIdentifier { get; }

    /// <summary>
    /// The unlockableIdentifier of a prerequisite.
    /// Should be used for skill unlocks for a custom character if the character has an unlock condition.
    /// Multiple prereqs are not supported (as far as I can tell)
    /// </summary>
    string AchievementNameToken { get; }

    /// <summary>
    /// The language token for the name to be shown in logbook for this achievement.
    /// </summary>
    string PrerequisiteUnlockableIdentifier { get; }

    /// <summary>
    /// The language token for the unlockable.
    /// Not 100% sure where this is shown in game.
    /// </summary>
    string UnlockableNameToken { get; }

    /// <summary>
    /// The language token for the description to be shown in logbook for this achievement.
    /// Also used to create the 'How to unlock' text.
    /// </summary>
    string AchievementDescToken { get; }

    /// <summary>
    /// Sprite that is used for this achievement.
    /// </summary>
    Sprite Sprite { get; }

    /// <summary>
    /// Delegate that return a string that will be shown to the user on how to unlock the achievement.
    /// </summary>
    Func<string> GetHowToUnlock { get; }

    /// <summary>
    /// Delegate that return a string that will be shown to the user when the achievement is unlocked.
    /// </summary>
    Func<string> GetUnlocked { get; }
}
