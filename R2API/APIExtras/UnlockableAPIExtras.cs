using System;
using UnityEngine;
using RoR2;
using RoR2.Achievements;

namespace R2API {
    #region Interfaces
    /// <summary>
    /// Interface used to provide the metadata needed to register an achievement
    /// </summary>
    public interface IModdedUnlockableDataProvider {

        /// <summary>
        /// The identifier of the achievement being added
        /// Should be unique
        /// </summary>
        string AchievementIdentifier { get; }
        /// <summary>
        /// The identifier of the unlockable granted when the achievement is completed
        /// Should be unique
        /// This is what is used when specifying an unlock condition for various things in the game
        /// </summary>
        string UnlockableIdentifier { get; }
        /// <summary>
        /// The unlockableIdentifier of a prerequisite
        /// Should be used for skill unlocks for a custom character if the character has an unlock condition
        /// Multiple prereqs are not supported (as far as I can tell)
        /// </summary>
        string PrerequisiteUnlockableIdentifier { get; }
        /// <summary>
        /// The language token for the name to be shown in logbook for this achievement
        /// </summary>
        string AchievementNameToken { get; }
        /// <summary>
        /// The language token for the description to be shown in logbook for this achievement
        /// Also used to create the 'How to unlock' text
        /// </summary>
        string AchievementDescToken { get; }
        /// <summary>
        /// The language token for the unlockable
        /// Not 100% sure where this is shown in game
        /// </summary>
        string UnlockableNameToken { get; }
        /// <summary>
        /// The path that is passed to Resources.Load when the sprite is loaded for this achievement
        /// </summary>
        string SpritePath { get; }
    }

    /// <summary>
    /// Interface for supplying alternative ways of loading sprites for achievements in the future
    /// </summary>
    public interface IAchievementSpriteProvider {
        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        string PathString { get; }
        /// <summary>
        /// The sprite that is being provided
        /// </summary>
        Sprite Sprite { get; }
    }
    #endregion

    #region Extensions
    internal static class ModdedUnlockableDataProviderExtensions {
        internal static string GetHowToUnlock<TDataProvider>(this TDataProvider self)
            where TDataProvider : class, IModdedUnlockableDataProvider {
            var name = Language.GetString(self.AchievementNameToken);
            var desc = Language.GetString(self.AchievementDescToken);
            return Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", name, desc);
        }

        internal static string GetUnlocked<TDataProvider>(this TDataProvider self)
            where TDataProvider : class, IModdedUnlockableDataProvider {
            var name = Language.GetString(self.AchievementNameToken);
            var desc = Language.GetString(self.AchievementDescToken);
            return Language.GetStringFormatted("UNLOCKED_FORMAT", name, desc);
        }
    }
    #endregion


    #region Implementations
    /// <summary>
    /// An implementation of IAchievementSpriteProvider for using vanilla achievement icons
    /// </summary>
    public readonly struct VanillaSpriteProvider : IAchievementSpriteProvider {
        /// <summary>
        /// Creates a VanillaSpriteProvider from the path to its target sprite.
        /// The sprite is loaded and cached immediately upon creation
        /// </summary>
        /// <param name="path">A path suitable for Resources.Load that will return the desired sprite</param>
        public VanillaSpriteProvider(String path) {
            this.PathString = path;
            this.Sprite = UnityEngine.Resources.Load<Sprite>(path);
        }

        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public string PathString { get; }
        /// <summary>
        /// Returns the cached sprite
        /// </summary>
        public Sprite Sprite { get; }
    }

    /// <summary>
    /// An implementation of IAchievementSpriteProvider for use with AssetBundleResourcesProvider and ResourcesAPI
    /// </summary>
    public readonly struct CustomSpriteProvider : IAchievementSpriteProvider {
        //There is room for some extra constructor signatures here, but I haven't used ResourcesAPI in a long time so its best if someone else writes them.
        /// <summary>
        /// Creates a CustomSpriteProvider from the path used for a ResourcesProvider from ResourcesAPI
        /// </summary>
        /// <param name="modPrefixedPath">The path used to retreive the sprite from the provider</param>
        public CustomSpriteProvider( string modPrefixedPath) {
            this.PathString = modPrefixedPath;
        }

        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public string PathString { get; }
        /// <summary>
        /// Loads the sprite
        /// </summary>
        public Sprite Sprite { get => Resources.Load<Sprite>(this.PathString); }
    }



    //Generics good, boxing bad :)
    /// <summary>
    /// A base class that can be used to conveinently supply all the required info for a modded achievement
    /// </summary>
    /// <typeparam name="TSpriteProvider">The type of sprite provider being used for this achievement</typeparam>
    public abstract class ModdedUnlockableAndAchievement<TSpriteProvider> : BaseAchievement, IModdedUnlockableDataProvider
        where TSpriteProvider : IAchievementSpriteProvider {
        #region Implementation
        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public string SpritePath { get => this.SpriteProvider.PathString; }

        /// <summary>
        /// Removes this achievement from the current profile.
        /// </summary>
        public void Revoke() {
            if (base.userProfile.HasAchievement(this.AchievementIdentifier)) {
                base.userProfile.RevokeAchievement(this.AchievementIdentifier);
            }

            base.userProfile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(this.UnlockableIdentifier));
        }
        #endregion

        #region Contract
        /// <summary>
        /// This should return the sprite provider for this achievement
        /// </summary>
        protected abstract TSpriteProvider SpriteProvider { get; }
        /// <summary>
        /// The identifier of the achievement being added
        /// Should be unique
        /// </summary>
        public abstract string AchievementIdentifier { get; }
        /// <summary>
        /// The identifier of the unlockable granted when the achievement is completed
        /// Should be unique
        /// This is what is used when specifying an unlock condition for various things in the game
        /// </summary>
        public abstract string UnlockableIdentifier { get; }
        /// <summary>
        /// The unlockableIdentifier of a prerequisite
        /// Should be used for skill unlocks for a custom character if the character has an unlock condition
        /// Multiple prereqs are not supported (as far as I can tell)
        /// </summary>
        public abstract string PrerequisiteUnlockableIdentifier { get; }
        /// <summary>
        /// The language token for the name to be shown in logbook for this achievement
        /// </summary>
        public abstract string AchievementNameToken { get; }
        /// <summary>
        /// The language token for the description to be shown in logbook for this achievement
        /// Also used to create the 'How to unlock' text
        /// </summary>
        public abstract string AchievementDescToken { get; }
        /// <summary>
        /// The language token for the unlockable
        /// Not 100% sure where this is shown in game
        /// </summary>
        public abstract string UnlockableNameToken { get; }
        #endregion

        #region Virtuals
        /// <summary>
        /// Called when this achievement is granted
        /// </summary>
        public override void OnGranted() => base.OnGranted();
        /// <summary>
        /// This should be used to register to an event or apply a hook in order to detect when the achievement conditions are met
        /// When you detect that the conditions are met, call `base.Grant` inside the method
        /// </summary>
        public override void OnInstall() => base.OnInstall();
        /// <summary>
        /// This should unregister whatever was registered in OnInstall
        /// </summary>
        public override void OnUninstall() => base.OnUninstall();
        /// <summary>
        /// TODO
        /// </summary>
        /// <returns>TODO</returns>
        public override Single ProgressForAchievement() => base.ProgressForAchievement();
        /// <summary>
        /// Used to specify if this achievement is limited to a certain character
        /// </summary>
        /// <returns>The index of the character that is needed to unlock this achievement</returns>
        public override Int32 LookUpRequiredBodyIndex() => base.LookUpRequiredBodyIndex();
        /// <summary>
        /// Called when the body changes to a body that does not meet the requirements for this achievement
        /// </summary>
        public override void OnBodyRequirementBroken() => base.OnBodyRequirementBroken();
        /// <summary>
        /// Called when the body changes to a body that meets the requirements for this acheivement
        /// </summary>
        public override void OnBodyRequirementMet() => base.OnBodyRequirementMet();
        /// <summary>
        /// This actually does nothing in vanilla, it is here in case that changes in future updates.
        /// </summary>
        public override Boolean wantsBodyCallbacks { get => base.wantsBodyCallbacks; }   
        // This cannot be capitalized because it needs to match the base class defined in ror2.
        #endregion
    }
    #endregion
}
