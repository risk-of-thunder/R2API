using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoR2;
using MonoMod.Utils;
using Mono.Cecil.Cil;
using Mono.Cecil;
using System.Reflection;
using MonoMod.Cil;

namespace R2API
{
	public class PickupSelection
	{
		public List<PickupIndex> Pickups { get; set; }
		public float DropChance { get; set; } = 1.0f;
	}

	public static class DefaultItemDrops
	{
		public static void AddDefaults()
		{
			AddDefaultShrineDrops();
			AddChestDefaultDrops();
			AddLunarChestDefaultDrops();
			AddEquipmentChestDefaultDrops();
			AddBossDefaultDrops();
		}

		public static void AddDefaultShrineDrops()
		{
			var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
			var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
			var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);
			var eq = ItemDropAPI.GetDefaultEquipmentDropList();

			var shrineSelections = new List<PickupSelection>
			{
				new List<ItemIndex> { ItemIndex.None }.ToSelection(ItemDropAPI.DefaultShrineFailureWeight),
				t1.ToSelection(ItemDropAPI.DefaultShrineTier1Weight),
				t2.ToSelection(ItemDropAPI.DefaultShrineTier2Weight),
				t3.ToSelection(ItemDropAPI.DefaultShrineTier3Weight),
				eq.ToSelection(ItemDropAPI.DefaultShrineEquipmentWeight)
			};

			ItemDropAPI.AddDropInformation(ItemDropLocation.Shrine, shrineSelections);
		}

		public static void AddChestDefaultDrops()
		{
			var t1 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier1);
			var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);
			var t3 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier3);

			var chestSelections = new List<PickupSelection>
			{
				t1.ToSelection(ItemDropAPI.DefaultChestTier1DropChance),
				t2.ToSelection(ItemDropAPI.DefaultChestTier2DropChance),
				t3.ToSelection(ItemDropAPI.DefaultChestTier3DropChance),
			};

			ItemDropAPI.AddDropInformation(ItemDropLocation.SmallChest, chestSelections);
			ItemDropAPI.AddDropInformation(ItemDropLocation.MediumChest, t2.ToSelection(0.8f), t3.ToSelection(0.2f));
			ItemDropAPI.AddDropInformation(ItemDropLocation.LargeChest, t3.ToSelection());
		}

		public static void AddEquipmentChestDefaultDrops()
		{
			var eq = ItemDropAPI.GetDefaultEquipmentDropList();

			ItemDropAPI.AddDropInformation(ItemDropLocation.EquipmentChest, eq.ToSelection());
		}

		public static void AddLunarChestDefaultDrops()
		{
			var lun = ItemDropAPI.GetDefaultLunarDropList();
			ItemDropAPI.AddDropInformation(ItemDropLocation.LunarChest, lun.ToSelection());
		}

		public static void AddBossDefaultDrops()
		{
			ItemDropAPI.IncludeSpecialBossDrops = true;

			var t2 = ItemDropAPI.GetDefaultDropList(ItemTier.Tier2);

			ItemDropAPI.AddDropInformation(ItemDropLocation.Boss, t2.ToSelection());
		}
	}

	public enum ItemDropLocation
	{
		//Mobs,
		Boss,
		EquipmentChest,
		LunarChest,
		SmallChest,
		MediumChest,
		LargeChest,
		Shrine,
		SmallChestSelector,
		MediumChestSelector,
		LargeChestSelector
	}

	public static class ItemDropAPI
	{
		public static void InitHooks()
		{
			IL.RoR2.BossGroup.OnCharacterDeathCallback += (IL) =>
			{
				ILCursor cursor = new ILCursor(IL);

				cursor.GotoNext(x => x.MatchCall(typeof(PickupIndex).GetMethod("get_itemIndex")));
				var itemIndex_index = (int)cursor.Next.Operand;

				cursor.GotoNext(x => x.MatchStloc(itemIndex_index));

				cursor.Emit(OpCodes.Ldarg_0);
				cursor.Emit(OpCodes.Ldfld, typeof(BossGroup).GetField("rng", BindingFlags.NonPublic));
				cursor.Emit(OpCodes.Callvirt, typeof(Xoroshiro128Plus).GetMethod("get_nextNormalizedFloat"));
				cursor.Emit(OpCodes.Ldc_I4_0);
				cursor.Emit(OpCodes.Call, typeof(ItemDropLocation).GetMethod("GetSelection", BindingFlags.Static));
				cursor.EmitDelegate(() => { Debug.Log("[R2API] YAY"); });
			};
		}

		public static float ChestSpawnRate = 1.0f;
		public static bool IncludeSpecialBossDrops = true;

		public static float DefaultChestTier1DropChance = 0.8f;
		public static float DefaultChestTier2DropChance = 0.2f;
		public static float DefaultChestTier3DropChance = 0.01f;

		public static float DefaultShrineEquipmentWeight = 2f;
		public static float DefaultShrineFailureWeight = 10.1f;
		public static float DefaultShrineTier1Weight = 8f;
		public static float DefaultShrineTier2Weight = 2f;
		public static float DefaultShrineTier3Weight = 0.2f;

		public static float DefaultTier1SelectorDropChance = 0.8f;
		public static float DefaultTier2SelectorDropChance = 0.2f;
		public static float DefaultTier3SelectorDropChance = 0.01f;

		public static bool DefaultDrops { get; set; } = true;

		public static List<ItemIndex> None { get; set; } = new List<ItemIndex> { ItemIndex.None };

		public static Dictionary<ItemDropLocation, List<PickupSelection>> Selection { get; set; } = new Dictionary<ItemDropLocation, List<PickupSelection>>();

		public static void AddDropInformation(ItemDropLocation dropLocation, params PickupSelection[] pickupSelections)
		{
			Debug.Log($"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

			Selection[dropLocation] = pickupSelections.ToList();
		}

		public static void AddDropInformation(ItemDropLocation dropLocation, List<PickupSelection> pickupSelections)
		{
			Debug.Log($"Adding drop information for {dropLocation.ToString()}: {pickupSelections.Sum(x => x.Pickups.Count)} items");

			Selection[dropLocation] = pickupSelections;
		}

		public static PickupIndex GetSelection(ItemDropLocation dropLocation, float normalizedIndex)
		{
			if (!Selection.ContainsKey(dropLocation))
				return new PickupIndex(ItemIndex.None);

			var selections = Selection[dropLocation];

			var weightedSelection = new WeightedSelection<PickupIndex>();
			foreach (var selection in selections.Where(x => x != null))
				foreach (var pickup in selection.Pickups)
					weightedSelection.AddChoice(pickup, selection.DropChance / selection.Pickups.Count);

			return weightedSelection.Evaluate(normalizedIndex);
		}

		public static List<ItemIndex> GetDefaultDropList(ItemTier itemTier)
		{
			var list = new List<ItemIndex>();

			for (ItemIndex itemIndex = ItemIndex.Syringe; itemIndex < ItemIndex.Count; itemIndex++)
			{
				if (Run.instance.availableItems.HasItem(itemIndex))
				{
					ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);

					if (itemDef.tier == itemTier)
					{
						list.Add(itemIndex);
					}
				}
			}

			return list;
		}

		public static List<EquipmentIndex> GetDefaultLunarDropList()
		{
			var list = new List<EquipmentIndex>();

			for (EquipmentIndex equipmentIndex = EquipmentIndex.CommandMissile; equipmentIndex < EquipmentIndex.Count; equipmentIndex++)
			{
				if (Run.instance.availableEquipment.HasEquipment(equipmentIndex))
				{
					EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
					if (equipmentDef.canDrop)
					{
						if (equipmentDef.isLunar)
						{
							list.Add(equipmentIndex);
						}
					}
				}
			}

			return list;
		}

		public static List<EquipmentIndex> GetDefaultEquipmentDropList()
		{
			var list = new List<EquipmentIndex>();

			for (EquipmentIndex equipmentIndex = EquipmentIndex.CommandMissile; equipmentIndex < EquipmentIndex.Count; equipmentIndex++)
			{
				if (Run.instance.availableEquipment.HasEquipment(equipmentIndex))
				{
					EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
					if (equipmentDef.canDrop)
					{
						if (!equipmentDef.isLunar)
						{
							list.Add(equipmentIndex);
						}
					}
				}
			}

			return list;
		}

		public static PickupSelection ToSelection(this List<ItemIndex> indices, float dropChance = 1.0f)
		{
			if (indices == null)
			{
				return null;
			}

			return new PickupSelection
			{
				DropChance = dropChance,
				Pickups = indices.Select(x => new PickupIndex(x)).ToList()
			};
		}

		public static PickupSelection ToSelection(this List<EquipmentIndex> indices, float dropChance = 1.0f)
		{
			if (indices == null)
			{
				return null;
			}

			return new PickupSelection
			{
				DropChance = dropChance,
				Pickups = indices.Select(x => new PickupIndex(x)).ToList()
			};
		}
	}
}
