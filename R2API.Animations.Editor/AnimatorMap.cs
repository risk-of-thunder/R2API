using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

namespace R2API.Animations.Editor;

public class AnimatorMap : ScriptableObject
{
    public AnimatorController sourceController;
    public List<UnityEngine.Object> sourceObjects = [];
    public List<UnityEngine.Object> modifiedObjects = [];
}
