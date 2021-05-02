using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace R2API {
    /// <summary>
    /// API for handling deployables added by mods
    /// </summary>
    [R2APISubmodule]
    public static class DamageAPI {
        public enum ModdedDamageType { };

        private const byte flagsPerValue = 8;
        private const byte valuesPerBlock = 18;
        private const byte valuesPerSection = flagsPerValue * valuesPerBlock;
        private const byte sectionsCount = 8;
        private const byte blockPartsCount = 4;

        private static readonly ConditionalWeakTable<DamageInfo, DamageTypeHolder> damageTypeHolders = new();

        /// <summary>
        /// Return true if the submodule is loaded.
        /// </summary>
        public static bool Loaded {
            get => _loaded;
            internal set => _loaded = value;
        }
        private static bool _loaded;

        /// <summary>
        /// Reserved ModdedDamageTypes count
        /// </summary>
        public static int ModdedDamageTypeCount { get; private set; }


        #region Hooks
        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void SetHooks() {
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo += WriteDamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo += ReadDamageInfo;
        }

        [R2APISubmoduleInit(Stage = InitStage.UnsetHooks)]
        internal static void UnsetHooks() {
            On.RoR2.NetworkExtensions.Write_NetworkWriter_DamageInfo -= WriteDamageInfo;
            On.RoR2.NetworkExtensions.ReadDamageInfo -= ReadDamageInfo;
        }

        private static RoR2.DamageInfo ReadDamageInfo(On.RoR2.NetworkExtensions.orig_ReadDamageInfo orig, UnityEngine.Networking.NetworkReader reader) {
            var damageInfo = orig(reader);

            if (ModdedDamageTypeCount == 0) {
                return damageInfo;
            }

            var holder = DamageTypeHolder.FromNetworkReader(reader);
            if (holder != null) {
                damageTypeHolders.Add(damageInfo, holder);
            }

            return damageInfo;
        }

        private static void WriteDamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, UnityEngine.Networking.NetworkWriter writer, RoR2.DamageInfo damageInfo) {
            orig(writer, damageInfo);

            if (ModdedDamageTypeCount == 0) {
                return;
            }

            if (!damageTypeHolders.TryGetValue(damageInfo, out var holder)) {
                writer.Write((byte)0);
                return;
            }

            holder.Write(writer);
        }
        #endregion

        #region Public
        /// <summary>
        /// Reserve ModdedDamageType to use it with
        /// <see cref="DamageAPI.AddModdedDamageType(DamageInfo, ModdedDamageType)"/> and
        /// <see cref="DamageAPI.HasModdedDamageType(DamageInfo, ModdedDamageType)"/>
        /// </summary>
        /// <returns></returns>
        public static ModdedDamageType ReserveDamageType() {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if (ModdedDamageTypeCount >= sectionsCount * valuesPerSection) {
                //I doubt this ever gonna happen, but just in case.
                throw new IndexOutOfRangeException($"Reached the limit of {sectionsCount * valuesPerSection} ModdedDamageTypes. Please contact R2API developers to increase the limit");
            }

            return (ModdedDamageType)ModdedDamageTypeCount++;
        }

        /// <summary>
        /// Adding ModdedDamageType to DamageInfo. You can add more than one damage type to one DamageInfo
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="moddedDamageType"></param>
        public static void AddModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if ((int)moddedDamageType >= ModdedDamageTypeCount) {
                throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types ({ModdedDamageTypeCount})");
            }

            if (!damageTypeHolders.TryGetValue(damageInfo, out var holder)) {
                damageTypeHolders.Add(damageInfo, holder = new DamageTypeHolder());
            }

            holder.Add(moddedDamageType);
        }

        /// <summary>
        /// Check if DamageInfo has ModdedDamageType. One DamageInfo can have more than one damage type.
        /// </summary>
        /// <param name="damageInfo"></param>
        /// <param name="moddedDamageType"></param>
        /// <returns></returns>
        public static bool HasModdedDamageType(this DamageInfo damageInfo, ModdedDamageType moddedDamageType) {
            if (!Loaded) {
                throw new InvalidOperationException($"{nameof(DamageAPI)} is not loaded. Please use [{nameof(R2APISubmoduleDependency)}(nameof({nameof(DamageAPI)})]");
            }

            if ((int)moddedDamageType >= ModdedDamageTypeCount) {
                throw new ArgumentOutOfRangeException($"Parameter '{nameof(moddedDamageType)}' with value {moddedDamageType} is out of range of registered types ({ModdedDamageTypeCount})");
            }

            if (!damageTypeHolders.TryGetValue(damageInfo, out var holder)) {
                return false;
            }

            return holder.Has(moddedDamageType);
        }
        #endregion

        #region Private classes
        private class DamageTypeHolder {
            private const uint blockValuesMask = 0b_00111111_00011111_00001111_00000111;
            private const uint fullBlockHeader = 0b_00000000_01000000_01100000_01110000;

            private const uint block1HeaderMask = 0b_01000000;
            private const uint block2HeaderMask = 0b_01100000;
            private const uint block3HeaderMask = 0b_01110000;
            private const uint block4HeaderMask = 0b_01111000;

            private const uint block1HeaderXor = 0b_00000000;
            private const uint block2HeaderXor = 0b_01000000;
            private const uint block3HeaderXor = 0b_01100000;
            private const uint block4HeaderXor = 0b_01110000;

            private const uint highestBitInInt = 0b_10000000_00000000_00000000_00000000;
            private const uint highestBitInByte = 0b_10000000;

            private byte[] values;

            public void Add(ModdedDamageType moddedDamageType) {
                var valueIndex = (int)moddedDamageType / flagsPerValue;
                var flagIndex = (int)moddedDamageType % flagsPerValue;

                ResizeIfNeeded(valueIndex);

                values[valueIndex] = (byte)(values[valueIndex] | highestBitInByte >> flagIndex);
            }

            public bool Has(ModdedDamageType moddedDamageType) {
                var valueIndex = (int)moddedDamageType / flagsPerValue;
                var flagIndex = (int)moddedDamageType % flagsPerValue;

                if (values == null || valueIndex >= values.Length) {
                    return false;
                }

                return (values[valueIndex] & (highestBitInByte >> flagIndex)) != 0;
            }

            public static DamageTypeHolder FromNetworkReader(NetworkReader reader) {
                var sectionByte = reader.ReadByte();
                if (sectionByte == 0) {
                    return null;
                }
                var holder = new DamageTypeHolder();

                for (var i = 0; i < 8; i++) {
                    if ((sectionByte & 1 << i) != 0) {
                        holder.ReadBlock(reader, i);
                    }
                }

                return holder;
            }

            private void ReadBlock(NetworkReader reader, int blockIndex) {
                var fullBlockMask = new FullBlockMask();

                var maskIndex = 0;
                while (true) {
                    var blockBytes = reader.ReadByte();
                    uint mask, xor;
                    do {
                        (mask, xor) = GetMask(maskIndex++);
                    }
                    while ((blockBytes & mask ^ xor) != 0);

                    fullBlockMask[maskIndex - 1] = blockBytes;

                    if ((blockBytes & highestBitInByte) != 0) {
                        break;
                    }
                }

                var bitesSkipped = 0;
                for (var i = 0; i < 32; i++) {
                    if ((blockValuesMask & highestBitInInt >> i) == 0) {
                        bitesSkipped++;
                        continue;
                    }
                    if ((fullBlockMask.integer & highestBitInInt >> i) != 0) {
                        var valueIndex = (blockIndex * valuesPerBlock) + i - bitesSkipped;
                        ResizeIfNeeded(valueIndex);
                        values[valueIndex] = reader.ReadByte();
                    }
                }
            }

            private void ResizeIfNeeded(int valueIndex) {
                if (values == null) {
                    values = new byte[valueIndex + 1];
                }
                if (valueIndex >= values.Length) {
                    Array.Resize(ref values, valueIndex + 1);
                }
            }


            public void Write(NetworkWriter writer) {
                int section = 0;
                for (var i = 0; i < sectionsCount; i++) {
                    if (!IsBlockEmpty(i)) {
                        section |= 1 << i;
                    }
                }
                writer.Write((byte)section);

                for (var i = 0; i < sectionsCount; i++) {
                    if (!IsBlockEmpty(i)) {
                        WriteBlock(writer, i);
                    }
                }
            }

            private void WriteBlock(NetworkWriter writer, int blockIndex) {
                var bitesSkipped = 0;
                var fullBlockMask = new FullBlockMask();
                var orderedValues = new List<byte>();
                for (var i = 0; i < 32; i++) {
                    fullBlockMask.integer <<= 1;
                    if ((blockValuesMask & highestBitInInt >> i) == 0) {
                        bitesSkipped++;
                        continue;
                    }
                    var valueIndex = blockIndex * valuesPerBlock + (i - bitesSkipped);
                    if (valueIndex >= values.Length) {
                        continue;
                    }
                    if (values[valueIndex] != 0) {
                        fullBlockMask.integer |= 1;
                        orderedValues.Add(values[valueIndex]);
                    }
                }
                var lastIndex = 0;
                for (var i = 0; i < 4; i++) {
                    if (fullBlockMask[i] != 0) {
                        lastIndex = i;
                    }
                }

                fullBlockMask.integer |= fullBlockHeader;
                fullBlockMask[lastIndex] = (byte)(fullBlockMask[lastIndex] | highestBitInByte);

                for (var i = 0; i <= lastIndex; i++) {
                    var (headerMask, _) = GetMask(i);
                    if ((fullBlockMask[i] & (~headerMask)) != 0) {
                        writer.Write(fullBlockMask[i]);
                    }
                }
                foreach (var value in orderedValues) {
                    writer.Write(value);
                }
            }

            private bool IsBlockEmpty(int blockIndex) {
                if (values == null || values.Length == 0 || values.Length / valuesPerBlock < blockIndex) {
                    return true;
                }

                for (var i = blockIndex * valuesPerBlock; i < Math.Min((blockIndex + 1) * valuesPerBlock, values.Length); i++) {
                    if (values[i] != 0) {
                        return false;
                    }
                }

                return true;
            }

            private static (uint mask, uint xor) GetMask(int i) {
                return i switch {
                    0 => (block1HeaderMask, block1HeaderXor),
                    1 => (block2HeaderMask, block2HeaderXor),
                    2 => (block3HeaderMask, block3HeaderXor),
                    3 => (block4HeaderMask, block4HeaderXor),
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FullBlockMask {
            [FieldOffset(3)]
            public byte byte0;
            [FieldOffset(2)]
            public byte byte1;
            [FieldOffset(1)]
            public byte byte2;
            [FieldOffset(0)]
            public byte byte3;

            [FieldOffset(0)]
            public uint integer;

            public byte this[int i] {
                get {
                    return i switch {
                        0 => byte0,
                        1 => byte1,
                        2 => byte2,
                        3 => byte3,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set {
                    switch (i) {
                        case 0:
                            byte0 = value;
                            break;
                        case 1:
                            byte1 = value;
                            break;
                        case 2:
                            byte2 = value;
                            break;
                        case 3:
                            byte3 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }
        #endregion
    }
}
