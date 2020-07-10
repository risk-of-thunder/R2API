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
        string achievementIdentifier { get; }
        /// <summary>
        /// The identifier of the unlockable granted when the achievement is completed
        /// Should be unique
        /// This is what is used when specifying an unlock condition for various things in the game
        /// </summary>
        string unlockableIdentifier { get; }
        /// <summary>
        /// The unlockableIdentifier of a prerequisite
        /// Should be used for skill unlocks for a custom character if the character has an unlock condition
        /// Multiple prereqs are not supported (as far as I can tell)
        /// </summary>
        string prerequisiteUnlockableIdentifier { get; }
        /// <summary>
        /// The language token for the name to be shown in logbook for this achievement
        /// </summary>
        string achievementNameToken { get; }
        /// <summary>
        /// The language token for the description to be shown in logbook for this achievement
        /// Also used to create the 'How to unlock' text
        /// </summary>
        string achievementDescToken { get; }
        /// <summary>
        /// The language token for the unlockable
        /// Not 100% sure where this is shown in game
        /// </summary>
        string unlockableNameToken { get; }
        /// <summary>
        /// The path that is passed to Resources.Load when the sprite is loaded for this achievement
        /// </summary>
        string spritePath { get; }
    }

    /// <summary>
    /// Interface for supplying alternative ways of loading sprites for achievements in the future
    /// </summary>
    public interface IAchievementSpriteProvider {
        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        string pathString { get; }
        /// <summary>
        /// The sprite that is being provided
        /// </summary>
        Sprite sprite { get; }
    }
    #endregion

    #region Extensions
    internal static class ModdedUnlockableDataProviderExtensions {
        internal static string GetHowToUnlock<TDataProvider>(this TDataProvider self)
            where TDataProvider : class, IModdedUnlockableDataProvider {
            var name = Language.GetString(self.achievementNameToken);
            var desc = Language.GetString(self.achievementDescToken);
            return Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", name, desc);
        }

        internal static String GetUnlocked<TDataProvider>(this TDataProvider self)
            where TDataProvider : class, IModdedUnlockableDataProvider {
            var name = Language.GetString(self.achievementNameToken);
            var desc = Language.GetString(self.achievementDescToken);
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
            this.pathString = path;
            this.sprite = UnityEngine.Resources.Load<Sprite>(path);
        }

        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public string pathString { get; }
        /// <summary>
        /// Returns the cached sprite
        /// </summary>
        public Sprite sprite { get; }
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
        public CustomSpriteProvider( String modPrefixedPath) {
            this.pathString = modPrefixedPath;
        }

        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public String pathString { get; }
        /// <summary>
        /// Loads the sprite
        /// </summary>
        public Sprite sprite { get => Resources.Load<Sprite>(this.pathString); }
    }



    //Generics good, boxing bad :)
    /// <summary>
    /// A base class that can be used to conveinently supply all the required info for a modded achievement
    /// </summary>
    /// <typeparam name="TSpriteProvider">The type of sprite provider being used for this achievement</typeparam>
    public abstract class ModdedUnlockable<TSpriteProvider> : BaseAchievement, IModdedUnlockableDataProvider
        where TSpriteProvider : IAchievementSpriteProvider {
        #region Implementation
        /// <summary>
        /// The path that will be passed to Resources.Load to get the sprite
        /// </summary>
        public String spritePath { get => this.spriteProvider.pathString; }
        #endregion

        #region Contract
        /// <summary>
        /// This should return the sprite provider for this achievement
        /// </summary>
        protected abstract TSpriteProvider spriteProvider { get; }
        /// <summary>
        /// The identifier of the achievement being added
        /// Should be unique
        /// </summary>
        public abstract String achievementIdentifier { get; }
        /// <summary>
        /// The identifier of the unlockable granted when the achievement is completed
        /// Should be unique
        /// This is what is used when specifying an unlock condition for various things in the game
        /// </summary>
        public abstract String unlockableIdentifier { get; }
        /// <summary>
        /// The unlockableIdentifier of a prerequisite
        /// Should be used for skill unlocks for a custom character if the character has an unlock condition
        /// Multiple prereqs are not supported (as far as I can tell)
        /// </summary>
        public abstract String prerequisiteUnlockableIdentifier { get; }
        /// <summary>
        /// The language token for the name to be shown in logbook for this achievement
        /// </summary>
        public abstract String achievementNameToken { get; }
        /// <summary>
        /// The language token for the description to be shown in logbook for this achievement
        /// Also used to create the 'How to unlock' text
        /// </summary>
        public abstract String achievementDescToken { get; }
        /// <summary>
        /// The language token for the unlockable
        /// Not 100% sure where this is shown in game
        /// </summary>
        public abstract String unlockableNameToken { get; }
        #endregion

        #region Virtuals
        /// <summary>
        /// Called when this achievement is granted
        /// </summary>
        public override void OnGranted() => base.OnGranted();
        /// <summary>
        /// This should be used to register to an event or apply a hook in order to detect when the achievement conditions are met
        /// When you detect that the conditions are met, call base.Grant inside the method
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
        /// TODO
        /// </summary>
        /// <returns>TODO</returns>
        public override Int32 LookUpRequiredBodyIndex() => base.LookUpRequiredBodyIndex();
        /// <summary>
        /// TODO
        /// </summary>
        public override void OnBodyRequirementBroken() => base.OnBodyRequirementBroken();
        /// <summary>
        /// TODO
        /// </summary>
        public override void OnBodyRequirementMet() => base.OnBodyRequirementMet();
        /// <summary>
        /// TODO
        /// </summary>
        public override Boolean wantsBodyCallbacks { get => base.wantsBodyCallbacks; }
        #endregion
    }
    #endregion
}
