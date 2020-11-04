using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;

namespace R2API.ItemDrop {
    [Flags]
    public enum EquipmentDropType {
        DefaultValue = 0,
        Normal = 1,
        Boss = 2,
        Lunar = 4,
        Elite = 8,
        NoDrop = 16
    }

    public static class EquipmentDropTypeUtil {
        public static IEnumerable<EquipmentDropType> GetEquipmentTypesFromIndex(EquipmentIndex equipmentIndex) {
            var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);

            var equipmentDropType = EquipmentDropType.DefaultValue;

            if (equipmentDef.canDrop) {
                if (equipmentDef.isBoss) {
                    equipmentDropType |= EquipmentDropType.Boss;
                }

                if (equipmentDef.isLunar) {
                    equipmentDropType |= EquipmentDropType.Lunar;
                }

                if (equipmentDef.IsElite()) {
                    equipmentDropType |= EquipmentDropType.Elite;
                }

                return equipmentDropType.GetFlags<EquipmentDropType>();
            }
            else {
                return new[] {EquipmentDropType.NoDrop};
            }
        }

        public static IEnumerable<T> GetFlags<T>(this Enum flags)
            where T : Enum {
            return
                from Enum value in Enum.GetValues(flags.GetType())
                where (int)(object)value != 0 && flags.HasFlag(value)
                select (T)value;
        }

        public static bool IsElite(this EquipmentDef equipmentDef) {
            return EliteCatalog.GetEquipmentEliteIndex(equipmentDef.equipmentIndex) != EliteIndex.None;
        }
    }
}
