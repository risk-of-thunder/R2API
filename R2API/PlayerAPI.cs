using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace R2API
{
	public static class PlayerAPI
	{
		public static List<PlayerStatChange> CustomEffects { get; private set; }

		public static void InitHooks()
		{
			var detour = new NativeDetour(typeof(CharacterBody).GetMethod("RecalculateStats", System.Reflection.BindingFlags.Public),
				typeof(ItemAPI).GetMethod(nameof(RecalcStats), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static));
		}
		public static void RecalcStats(CharacterBody characterBody)
		{
			characterBody.SetFieldValue("experience", TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
			characterBody.SetFieldValue("level", TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex));

			//Character Stats
			int maxHealth = 0;
			int healthRegen = 0;
			bool isElite = false;
			int maxShield = 0;
			float movementSpeed = 0;
			float acceleration = 0;
			float jumpPower = 0;
			float maxJumpHeight = 0;
			float maxJumpCount = 0;
			float attackSpeed = 0;
			float damage = 0;
			float Crit = 0;
			float Armor = 0;
			float critHeal = 0; 

			//Primary Skill
			float PrimaryCooldownScale = 0;
			float PrimaryStock = 0;
			//Secondary Skill
			float SecondaryCooldownScale = 0;
			float SecondaryStock = 0;
			//Utility Skill
			float UtilityCooldownScale = 0;
			float UtilityStock = 0;
			//Special Skill
			float SpecialCooldownScale = 0;
			float SpecialStock = 0;

			/* Calculate Vanilla items effects
			* 
			* TODO
			*/

			foreach (var x in PlayerAPI.CustomEffects)
			{
				characterBody.SetFieldValue(x.Target, x.Function(characterBody));
			}

			characterBody.statsDirty = false;
		}
	}
	public class PlayerStatChange
	{
		/// <summary>
		/// The <b>case sensitive</b> name of the field you want to modify in the Character Body
		/// </summary>
		/// <example>
		/// Target = "damamge"
		/// </example>
		public string Target { get; set; }
		/// <summary>
		/// The type of operand that will be applied to the target.	
		/// </summary>
		public Operands operand { get; set; }
		/// <summary>
		/// This method is executed by the API to obtain the value. This is where you will have to code the logic of how your custom effect
		/// changes the player's stats.
		/// </summary>
		public Func<CharacterBody, int> Function { get; private set; }
	}
	public enum Operands { Addition, Multiplication, Division, Substraction }
}
