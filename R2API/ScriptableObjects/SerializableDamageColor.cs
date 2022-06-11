using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.ScriptableObjects {
    [CreateAssetMenu(fileName = "new DamageColor", menuName = "R2API/DamageColor")]
    public class SerializableDamageColor : ScriptableObject {
        public Color color;
        public DamageColorIndex DamageColorIndex { get; internal set; }
    }
}
