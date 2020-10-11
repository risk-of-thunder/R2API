using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2.UI;
using System;
using R2API.Utils;

namespace R2API {
    // ReSharper disable once InconsistentNaming
    [R2APISubmodule]
    public static class InventoryAPI {

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;


        public static event Action<ItemIcon>? OnItemIconAdded;
        public static event Action<EquipmentIcon>? OnEquipmentIconAdded;

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons += OnItemIconAddedHook;
            IL.RoR2.UI.ScoreboardStrip.SetMaster += OnEquipmentIconAddedHook;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons -= OnItemIconAddedHook;
            IL.RoR2.UI.ScoreboardStrip.SetMaster -= OnEquipmentIconAddedHook;
        }

        private static void OnItemIconAddedHook(ILContext il) {
            var cursor = new ILCursor(il).Goto(0);
            cursor.GotoNext(
                x => x.MatchStloc(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<ItemInventoryDisplay>("itemIcons")
            );
            cursor.Emit(OpCodes.Dup);
            cursor.EmitDelegate<Action<ItemIcon>>(i => OnItemIconAdded?.Invoke(i));
        }

        private static void OnEquipmentIconAddedHook(ILContext il) {
            var cursor = new ILCursor(il).Goto(0);
            var setSubscribedInventory = typeof(ItemInventoryDisplay).GetMethodCached("SetSubscribedInventory");
            cursor.GotoNext(x => x.MatchCallvirt(setSubscribedInventory));
            cursor.Index += 1;

            cursor.Emit(OpCodes.Ldarg_0);

            cursor.EmitDelegate<Action<ScoreboardStrip>>(eq => {
                if (eq.equipmentIcon != null) {
                    OnEquipmentIconAdded?.Invoke(eq.equipmentIcon);
                }
            });
        }
    }
}
