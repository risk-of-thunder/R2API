using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.ScriptableObjects;

[CreateAssetMenu(fileName = "New ColorCatalogEntry", menuName = "R2API/Colors/ColorCatalogEntry")]
public class SerializableColorCatalogEntry : ScriptableObject
{
    public Color32 color32;
    public ColorCatalog.ColorIndex ColorIndex { get; internal set; } = (ColorCatalog.ColorIndex)(-1);

}
