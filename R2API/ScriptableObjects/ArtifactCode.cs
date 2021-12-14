using RoR2;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace R2API {

    /// <summary>
    /// Scriptable object to ease the creation of Artifact codes.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtifactCode", menuName = "R2API/ArtifactCodeAPI/ArtifactCode", order = 0)]
    public class ArtifactCode : ScriptableObject {

        private const string constant = "For a list of accepted vanilla compound values, check ArtifactCodeAPI.CompoundValues";
        [Header(constant)]
        /// <summary>
        /// Compound values that represent the top 3 compounds. For a list of accepted vanilla compound values, check ArtifactCodeAPI.CompoundValues
        /// </summary>
        [Tooltip($"Compound values that represent the top 3 compounds.\n{constant}")]
        public Vector3Int topRow = new Vector3Int();
        /// <summary>
        /// Compound values that represent the middle 3 compounds. For a list of accepted vanilla compound values, check ArtifactCodeAPI.CompoundValues
        /// </summary>
        [Tooltip($"Compound values that represent the middle 3 compounds.\n{constant}")]
        public Vector3Int middleRow = new Vector3Int();
        /// <summary>
        /// Compound values that represent the bottom 3 compounds. For a list of accepted vanila compound values, check ArtifactCodeAPI.CompoundValues
        /// </summary>
        [Tooltip($"Compounds values that represent the bottom 3 compounds.\n{constant}")]
        public Vector3Int bottomRow = new Vector3Int();

        [Obsolete("The artifact compounds list is obsolete, please use the topRow, middleRow and bottomRow Vector3Int.")]
        [HideInInspector]
        public List<int> ArtifactCompounds = new List<int>();

        private int[] artifactSequence;

        private SHA256 hasher;

        /// <summary>
        /// The Sha256HashAsset stored in this Scriptable Object.
        /// </summary>
        [HideInInspector]
        public Sha256HashAsset hashAsset;

        /// <summary>
        /// Creates the Sha256HashAsset
        /// </summary>
        internal void Start() {
            hasher = SHA256.Create();

            if (ArtifactCompounds.Count > 0) {
                R2API.Logger.LogWarning($"Artifact Code of name {name} is using the deprecated ArtifactCompounds list.");
                artifactSequence = CreateSequenceFromList();
                hashAsset = CreateHashAsset(CreateHash());
            }
            else {
                artifactSequence = CreateSequenceFromVectors();
                hashAsset = CreateHashAsset(CreateHash());
            }
        }

        private int[] CreateSequenceFromList() {

            List<int> sequence = new List<int>();

            sequence.Add(ArtifactCompounds[2]);
            sequence.Add(ArtifactCompounds[5]);
            sequence.Add(ArtifactCompounds[8]);
            sequence.Add(ArtifactCompounds[1]);
            sequence.Add(ArtifactCompounds[7]);
            sequence.Add(ArtifactCompounds[4]);
            sequence.Add(ArtifactCompounds[0]);
            sequence.Add(ArtifactCompounds[3]);
            sequence.Add(ArtifactCompounds[6]);

            return sequence.ToArray();
        }

        private int[] CreateSequenceFromVectors() {
            List<int> sequence = new List<int>();
            sequence.Add(topRow.z);
            sequence.Add(middleRow.z);
            sequence.Add(bottomRow.z);
            sequence.Add(topRow.y);
            sequence.Add(middleRow.y);
            sequence.Add(bottomRow.y);
            sequence.Add(topRow.x);
            sequence.Add(middleRow.x);
            sequence.Add(bottomRow.x);

            return sequence.ToArray();
        }

        private Sha256Hash CreateHash() {
            byte[] array = new byte[artifactSequence.Length];
            for (int i = 0; i < array.Length; i++) {
                array[i] = (byte)artifactSequence[i];
            }
            return Sha256Hash.FromBytes(hasher.ComputeHash(array));
        }

        private Sha256HashAsset CreateHashAsset(Sha256Hash hash) {
            var asset = ScriptableObject.CreateInstance<Sha256HashAsset>();
            asset.value._00_07 = hash._00_07;
            asset.value._08_15 = hash._08_15;
            asset.value._16_23 = hash._16_23;
            asset.value._24_31 = hash._24_31;
            return asset;
        }
    }
}
