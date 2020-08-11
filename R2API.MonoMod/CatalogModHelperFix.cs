using System;
using System.Collections.Generic;
using MonoMod;
// ReSharper disable InconsistentNaming
#pragma warning disable 649
#pragma warning disable IDE1006 // Naming Styles

namespace RoR2 {
    [MonoModPatch("RoR2.CatalogModHelper`1")]
    class CatalogModHelper<TEntry>{

        public event Action<List<TEntry>> getAdditionalEntries;
        private readonly Action<int, TEntry> registrationDelegate;

        public void CollectAndRegisterAdditionalEntries(ref TEntry[] entries) {
            int vanillaEntriesLength = entries.Length;
            List<TEntry> moddedEntries = new List<TEntry>();
            getAdditionalEntries?.Invoke(moddedEntries);

            Array.Resize(ref entries, vanillaEntriesLength + moddedEntries.Count);

            for (int i = vanillaEntriesLength; i < moddedEntries.Count + vanillaEntriesLength; i++) {
                registrationDelegate(i, moddedEntries[i - vanillaEntriesLength]);
            }
        }
    }
}

#pragma warning restore IDE1006 // Naming Styles
