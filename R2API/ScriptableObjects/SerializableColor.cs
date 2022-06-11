using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace R2API.ScriptableObjects {
    [CreateAssetMenu(fileName = "new Color", menuName = "R2API/Color")]
    public class SerializableColor : ScriptableObject {
        public Color32 color32;
        public ColorCatalog.ColorIndex ColorIndex { get; internal set; } = (ColorCatalog.ColorIndex)(-1);

    }
}
