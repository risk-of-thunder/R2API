using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace R2API.ScriptableObjects {
    [CreateAssetMenu(fileName = "new ColorCatalogEntry", menuName = "R2API/ColorCatalogEntry")]
    public class SerializableColorCatalogEntry : ScriptableObject {
        public Color32 color32;
        public ColorCatalog.ColorIndex ColorIndex { get; internal set; } = (ColorCatalog.ColorIndex)(-1);

    }
}
