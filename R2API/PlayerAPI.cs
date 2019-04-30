using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;

namespace R2API
{
	public static class PlayerAPI
	{
		public static List<Action<PlayerStats>> CustomEffects { get; private set; }

		public static void InitHooks()
		{
			On.RoR2.CharacterBody.RecalculateStats += (orig, self) => RecalcStats(self);
		}

		public static void RecalcStats(CharacterBody characterBody)
		{
			characterBody.SetFieldValue("experience",
				TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
			characterBody.SetFieldValue("level",
				TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex));

			/* Calculate Vanilla items effects
			* 
			* TODO
			*/

			PlayerStats playerStats = null; //TODO: initialize this from characterBody

			foreach (var effectAction in PlayerAPI.CustomEffects)
			{
				effectAction(playerStats);
			}

			characterBody.statsDirty = false;
		}
	}

	public class PlayerStats
	{
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