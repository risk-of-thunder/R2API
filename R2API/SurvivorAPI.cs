using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using RoR2;
using UnityEngine;

namespace R2API
{
	public static class SurvivorAPI
	{
		/// <summary>
		/// The complete list of survivors, including vanilla and modded survivors.
		/// </summary>
		public static ObservableCollection<SurvivorDef> SurvivorDefinitions { get; private set; }

		/// <summary>
		/// This event gets triggered when the Survivor Catalog is ready to receive additions/changes/removals.
		/// </summary>
		public static event EventHandler SurvivorCatalogReady;
		/// <summary>
		/// Returns true when the Vanilla Survivor Catalog has been built
		/// </summary>
		private static bool HasBeenInit = false;

		internal static void InitHooks()
		{
			var detour = new NativeDetour(typeof(SurvivorCatalog).GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Static),
				typeof(SurvivorAPI).GetMethod(nameof(Init), BindingFlags.Public | BindingFlags.Static));

			detour.Apply();

			System.Diagnostics.Trace.TraceInformation("Applied hook");

			On.RoR2.SurvivorCatalog.GetSurvivorDef += (orig, survivorIndex) =>
			{
				//orig is the original method and SurvivorIndex is the variable that is given to the original GetSurvivorDef
				if (survivorIndex < 0 || (int)survivorIndex > SurvivorDefinitions.Count)
				{
					return null;
				}

				return SurvivorDefinitions[(int)survivorIndex];
				//by never doing orig(), the original method is never executed whenever it's called, effectively being replaced
			};
		}

		public static void Init()
		{
			SurvivorDefinitions = new ObservableCollection<SurvivorDef>(new List<SurvivorDef>
			{
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("CommandoBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/CommandoDisplay"),
					descriptionToken = "COMMANDO_DESCRIPTION",
					primaryColor = new Color(0.929411769f, 0.5882353f, 0.07058824f),
					survivorIndex = SurvivorIndex.Commando
				},
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("EngiBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/EngiDisplay"),
					descriptionToken = "ENGI_DESCRIPTION",
					primaryColor = new Color(0.372549027f, 0.8862745f, 0.5254902f),
					unlockableName = "Characters.Engineer",
					survivorIndex = SurvivorIndex.Engineer
				},
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("HuntressBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/HuntressDisplay"),
					primaryColor = new Color(0.8352941f, 0.235294119f, 0.235294119f),
					descriptionToken = "HUNTRESS_DESCRIPTION",
					unlockableName = "Characters.Huntress",
					survivorIndex = SurvivorIndex.Huntress
				},
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("MageBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/MageDisplay"),
					descriptionToken = "MAGE_DESCRIPTION",
					primaryColor = new Color(0.968627453f, 0.75686276f, 0.992156863f),
					unlockableName = "Characters.Mage",
					survivorIndex = SurvivorIndex.Mage
				},
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("MercBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/MercDisplay"),
					descriptionToken = "MERC_DESCRIPTION",
					primaryColor = new Color(0.423529416f, 0.819607854f, 0.917647064f),
					unlockableName = "Characters.Mercenary",
					survivorIndex = SurvivorIndex.Merc
				},
				new SurvivorDef
				{
					bodyPrefab = BodyCatalog.FindBodyPrefab("ToolbotBody"),
					displayPrefab = Resources.Load<GameObject>("Prefabs/CharacterDisplays/ToolbotDisplay"),
					descriptionToken = "TOOLBOT_DESCRIPTION",
					primaryColor = new Color(0.827451f, 0.768627465f, 0.3137255f),
					unlockableName = "Characters.Toolbot",
					survivorIndex = SurvivorIndex.Toolbot
				}
			});

			SurvivorDefinitions.CollectionChanged += (sender, args) => { ReconstructSurvivors(); };

			SurvivorCatalogReady?.Invoke(null, null);

			HasBeenInit = true;

			ReconstructSurvivors();
		}

		private static FieldInfo survivorDefs = typeof(SurvivorCatalog).GetField("survivorDefs", BindingFlags.Static | BindingFlags.NonPublic);
		private static FieldInfo allSurvivorDefs = typeof(SurvivorCatalog).GetField("_allSurvivorDefs", BindingFlags.Static | BindingFlags.NonPublic);

		public static void ReconstructSurvivors()
		{
			if (!HasBeenInit)
				return;

			SurvivorCatalog.survivorMaxCount = Mathf.Max(SurvivorDefinitions.Count, 10);

			for (int i = 0; i < SurvivorDefinitions.Count; i++)
			{
				SurvivorDefinitions[i].survivorIndex = (SurvivorIndex)i;
			}

			SurvivorCatalog.idealSurvivorOrder = SurvivorDefinitions.Select(x => x.survivorIndex).ToArray();

			survivorDefs.SetValue(null, SurvivorDefinitions.ToArray());
			allSurvivorDefs.SetValue(null, SurvivorDefinitions.ToArray());

			ViewablesCatalog.Node node = new ViewablesCatalog.Node("Survivors", true, null);

			var existingNode = ViewablesCatalog.FindNode("Survivors");

			//this essentially deletes an existing node if it exists
			existingNode?.SetParent(new ViewablesCatalog.Node("dummy", true, null));

			for (var i = 0; i < SurvivorDefinitions.Count; i++)
			{
				var survivor = SurvivorDefinitions[i];

				ViewablesCatalog.Node survivorEntryNode = new ViewablesCatalog.Node(survivor.displayNameToken, false, node);
				survivorEntryNode.shouldShowUnviewed = userProfile => !userProfile.HasViewedViewable(survivorEntryNode.fullName) && userProfile.HasSurvivorUnlocked(survivor.survivorIndex) && !string.IsNullOrEmpty(survivor.unlockableName);
			}

			ViewablesCatalog.AddNodeToRoot(node);
		}
	}
}