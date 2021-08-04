using RoR2;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace R2API.ScriptableObjects {

    [CreateAssetMenu(fileName = "New Artifact Code", menuName = "R2API/ArtifactCodeAPI/ArtifactCode", order = 0)]
    public class ArtifactCode : ScriptableObject {

        /// <summary>
        /// List that contains your Artifact code. for information on how to fill this list, check this wiki page:
        /// <para>https://github.com/risk-of-thunder/R2Wiki/wiki/Creating-Custom-Artifacts</para>
        /// </summary>
        public List<int> ArtifactCompounds = new List<int>();

        private int[] artifactSequence;

        private SHA256 hasher;

        /// <summary>
        /// The Sha256HashAsset stored in this scriptable object.
        /// </summary>
        [HideInInspector]
        public Sha256HashAsset hashAsset;

        public void Start() {
            hasher = SHA256.Create();

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

            artifactSequence = sequence.ToArray();

            hashAsset = CreateHashAsset(CreateHash());
        }

        internal Sha256Hash CreateHash() {
            byte[] array = new byte[artifactSequence.Length];
            for (int i = 0; i < array.Length; i++) {
                array[i] = (byte)artifactSequence[i];
            }
            return Sha256Hash.FromBytes(hasher.ComputeHash(array));
        }

        internal Sha256HashAsset CreateHashAsset(Sha256Hash hash) {
            var asset = ScriptableObject.CreateInstance<Sha256HashAsset>();
            asset.value._00_07 = hash._00_07;
            asset.value._08_15 = hash._08_15;
            asset.value._16_23 = hash._16_23;
            asset.value._24_31 = hash._24_31;
            return asset;
        }
    }
}
