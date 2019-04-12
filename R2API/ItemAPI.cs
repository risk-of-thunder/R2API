using System.Collections.ObjectModel;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using RoR2;

namespace R2API
{
	public static class ItemAPI
	{
		public static ObservableCollection<ItemDef> ItemDefinitions { get; private set; }
		internal static void InitHooks()
		{
			var detour = new NativeDetour(typeof(CharacterBody).GetMethod("RecalculateStats", System.Reflection.BindingFlags.Public),
				typeof(ItemAPI).GetMethod(nameof(RecalcStats),System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static));
		}

		public static void RecalcStats(CharacterBody characterBody)
		{
			characterBody.SetFieldValue("experience",TeamManager.instance.GetTeamExperience(characterBody.teamComponent.teamIndex));
			characterBody.SetFieldValue("level",TeamManager.instance.GetTeamLevel(characterBody.teamComponent.teamIndex));

			//Character Stats
			int MaxHealth = 0;
			int HealthRegen = 0;
			bool IsElite = false;
			int MaxShield = 0;
			float MovementSpeed = 0;
			float Acceleration = 0;
			float JumpPower = 0;
			float MaxJumpHeight = 0;
			float MaxJumpCount = 0;
			float AttackSpeed = 0;
			float Crit = 0;
			float Armor = 0;

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
			


			characterBody.statsDirty = false;
		}
	}

	public class StatBuff
	{
		
	}
	public enum Operands { Addition, Multiplication, Division, Substraction }
}
