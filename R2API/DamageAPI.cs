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

        private const byte valuesPerBlock = 8;
        private const byte blocksPerSection = 18;
        private const byte valuesPerSection = valuesPerBlock * blocksPerSection;
        private const byte sectionsCount = 8;

        private static readonly ConditionalWeakTable<DamageInfo, Section> sectionPerInstance = new();

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

            var section = Section.FromNetworkReader(reader);
            if (section != null) {
                sectionPerInstance.Add(damageInfo, section);
            }

            return damageInfo;
        }

        private static void WriteDamageInfo(On.RoR2.NetworkExtensions.orig_Write_NetworkWriter_DamageInfo orig, UnityEngine.Networking.NetworkWriter writer, RoR2.DamageInfo damageInfo) {
            orig(writer, damageInfo);

            if (ModdedDamageTypeCount == 0) {
                return;
            }

            if (!sectionPerInstance.TryGetValue(damageInfo, out var section)) {
                writer.Write((byte)0);
                return;
            }

            section.Write(writer);
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

            if (!sectionPerInstance.TryGetValue(damageInfo, out var section)) {
                sectionPerInstance.Add(damageInfo, section = new Section());
            }

            section.Add(moddedDamageType);
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

            if (!sectionPerInstance.TryGetValue(damageInfo, out var section)) {
                return false;
            }

            return section.Has(moddedDamageType);
        }
        #endregion

        #region Private classes
        internal class Section {
            public readonly Dictionary<byte, Block> blocks = new();

            public static Section FromNetworkReader(NetworkReader reader) {
                var sectionByte = reader.ReadByte();
                if (sectionByte == 0) {
                    return null;
                }
                var section = new Section();

                for (var i = 0; i < 8; i++) {
                    if ((sectionByte & 1 << i) != 0) {
                        section.blocks[(byte)i] = Block.FromNetworkReader(reader); 
                    } 
                }

                return section;
            }

            public void Write(NetworkWriter writer) {
                int section = 0;
                foreach (var row in blocks) {
                    section |= 1 << row.Key;
                }
                writer.Write((byte)section);

                for (var i = 0; i < sectionsCount; i++) {
                    if (blocks.TryGetValue((byte)i, out var block)) {
                        block.Write(writer);
                    }
                }
            }

            public void Add(ModdedDamageType moddedDamageType) {
                var blockIndex = (byte)((int)moddedDamageType / valuesPerSection);
                if (!blocks.TryGetValue(blockIndex, out var block)) {
                    blocks[blockIndex] = block = new Block();
                }
                block.Add((byte)((int)moddedDamageType % valuesPerSection));
            }

            public bool Has(ModdedDamageType moddedDamageType) {
                var blockIndex = (int)moddedDamageType / valuesPerSection;
                if (!blocks.TryGetValue((byte)blockIndex, out var block)) {
                    return false;
                }
                return block.Has((byte)((int)moddedDamageType % valuesPerSection));
            }
        }

        internal class Block {
            public readonly Dictionary<byte, byte> values = new();

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

            public static Block FromNetworkReader(NetworkReader reader) {
                var block = new Block();
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
                        block.values[(byte)(i - bitesSkipped)] = reader.ReadByte();
                    } else {
                    }
                }

                return block;
            }

            public void Write(NetworkWriter writer) {
                var bitesSkipped = 0;
                var fullBlockMask = new FullBlockMask();
                var orderedValues = new List<byte>();
                for (var i = 0; i < 32; i++) {
                    fullBlockMask.integer <<= 1;
                    if ((blockValuesMask & highestBitInInt >> i) == 0) {
                        bitesSkipped++;
                        continue;
                    }
                    if (values.TryGetValue((byte)(i - bitesSkipped), out var value)) {
                        fullBlockMask.integer |= 1;
                        orderedValues.Add(value);
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

            public bool Has(byte insideBlockIndex) {
                var valueIndex = (byte)(insideBlockIndex / valuesPerBlock);
                if (!values.TryGetValue(valueIndex, out var value)) {
                    return false;
                }
                return (value & (highestBitInByte >> insideBlockIndex % valuesPerBlock)) != 0;
            }

            public void Add(byte insideBlockIndex) {
                var valueIndex = (byte)(insideBlockIndex / valuesPerBlock);
                if (!values.TryGetValue(valueIndex, out var value)) {
                    value = 0;
                }
                values[valueIndex] = (byte)(value | highestBitInByte >> insideBlockIndex % valuesPerBlock);
            }

            public static (uint mask, uint xor) GetMask(int i) {
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
