using RoR2;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using static R2API.ArtifactCodeAPI;

namespace R2API {
    [CreateAssetMenu(fileName = "New Artifact Code", menuName = "R2API/ArtifactCodeAPI/ArtifactCode", order = 0)]
    public class ArtifactCodeScriptableObject : ScriptableObject {

        /// <summary>
        /// List that contains your Artifact code. for information on how to fill this list, check this wiki page:
        /// <para>https://github.com/risk-of-thunder/R2Wiki/wiki/Creating-Custom-Artifacts</para>
        /// </summary>
        public List<ArtifactCompound> ArtifactCompounds = new List<ArtifactCompound>();

        private int[] artifactSequence;

        private SHA256 hasher;

        /// <summary>
        /// The Sha256HashAsset stored in this scriptable object.
        /// </summary>
        [HideInInspector]
        public Sha256HashAsset hashAsset;

        private void Awake() {
            hasher = SHA256.Create();

            List<int> sequence = new List<int>();

            foreach (ArtifactCompound compound in ArtifactCompounds) {
                switch (compound) {
                    case ArtifactCompound.None:
                        sequence.Add(11);
                        break;
                    case ArtifactCompound.Square:
                        sequence.Add(7);
                        break;
                    case ArtifactCompound.Circle:
                        sequence.Add(1);
                        break;
                    case ArtifactCompound.Triangle:
                        sequence.Add(3);
                        break;
                    case ArtifactCompound.Diamond:
                        sequence.Add(5);
                        break;
                }
            }

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
