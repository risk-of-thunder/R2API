// Nullable context not needed for deprecated APIs
#pragma warning disable CS8605 // Unboxing a possibly null value.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    public static class PlayerAPI {
        public static List<Action<PlayerStats>> CustomEffects { get; private set; }

        internal static void InitHooks() {
            On.RoR2.CharacterBody.RecalculateStats += (orig, self) => RecalcStats(self);
        }

        public static void RecalcStats(CharacterBody characterBody) {
            characterBody.SetFieldValue("experience",
                TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
            characterBody.SetFieldValue("level",
                TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex));

            /* Calculate Vanilla items effects
            *
            * TODO
            */

            PlayerStats playerStats = null; //TODO: initialize this from characterBody

            foreach (var effectAction in CustomEffects) {
                effectAction(playerStats);
            }

            //characterBody.statsDirty = false;
        }
    }

    public class PlayerStats {
        //Character Stats
        public int maxHealth = 0;
        public int healthRegen = 0;
        public bool isElite = false;
        public int maxShield = 0;
        public float movementSpeed = 0;
        public float acceleration = 0;
        public float jumpPower = 0;
        public float maxJumpHeight = 0;
        public float maxJumpCount = 0;
        public float attackSpeed = 0;
        public float damage = 0;
        public float Crit = 0;
        public float Armor = 0;
        public float critHeal = 0;

        //Primary Skill
        public float PrimaryCooldownScale = 0;
        public float PrimaryStock = 0;

        //Secondary Skill
        public float SecondaryCooldownScale = 0;
        public float SecondaryStock = 0;

        //Utility Skill
        public float UtilityCooldownScale = 0;
        public float UtilityStock = 0;

        //Special Skill
        public float SpecialCooldownScale = 0;
        public float SpecialStock = 0;
    }
}

#pragma warning restore CS8605 // Unboxing a possibly null value.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning restore CS8601 // Possible null reference assignment.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
