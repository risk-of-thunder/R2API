using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2;
public class SkinDef
{
    public Delegate r2api_onSkinApplied;
    public class RuntimeSkin
    {
        public SkinDef r2api_skinDef;
    }
    public struct MeshReplacementTemplate
    {
        public object[] r2api_skillVariants;
    }
    public struct LightReplacementTemplate
    {
        public object[] r2api_skillVariants;
    }
    public struct GhostReplacementTemplate
    {
        public object[] r2api_skillVariants;
    }
    public struct MinionSkinTemplate
    {
        public object[] r2api_skillVariants;
    }
}
