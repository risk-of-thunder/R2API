using System;
using System.Collections.Generic;
using MonoMod;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace RoR2 {
    [MonoModPatch("RoR2.CatalogModHelper`1")]
    class CatalogModHelper<TEntry>{

        public event Action<List<TEntry>> getAdditionalEntries;
        private readonly Action<int, TEntry> registrationDelegate;
        private readonly Func<TEntry, string> nameGetter;

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
