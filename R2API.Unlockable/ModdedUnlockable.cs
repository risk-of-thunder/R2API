using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API {
    /// <summary>
    /// Class used to provide the metadata needed to register an achievement + unlockable
    /// </summary>
    [Obsolete(UnlockableAPI.ObsoleteMessage)]
    public abstract class ModdedUnlockable : BaseAchievement, IModdedUnlockableDataProvider {
        public abstract string AchievementIdentifier { get; }
        public abstract string UnlockableIdentifier { get; }
        public abstract string AchievementNameToken { get; }
        public abstract string PrerequisiteUnlockableIdentifier { get; }
        public abstract string UnlockableNameToken { get; }
        public abstract string AchievementDescToken { get; }
        public abstract Sprite Sprite { get; }
        public abstract Func<string> GetHowToUnlock { get; }
        public abstract Func<string> GetUnlocked { get; }

        public override void OnGranted() => base.OnGranted();

        public override void OnInstall() {
            base.OnInstall();
        }

        public override void OnUninstall() {
            base.OnUninstall();
        }

        public void Revoke() {
            if (userProfile.HasAchievement(AchievementIdentifier)) {
                userProfile.RevokeAchievement(AchievementIdentifier);
            }

            userProfile.RevokeUnlockable(UnlockableCatalog.GetUnlockableDef(UnlockableIdentifier));
        }

        public override float ProgressForAchievement() => base.ProgressForAchievement();

        public override BodyIndex LookUpRequiredBodyIndex() {
            return base.LookUpRequiredBodyIndex();
        }

        public override void OnBodyRequirementBroken() => base.OnBodyRequirementBroken();
        public override void OnBodyRequirementMet() => base.OnBodyRequirementMet();
        public override bool wantsBodyCallbacks { get => base.wantsBodyCallbacks; }
    }
}
