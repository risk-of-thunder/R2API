using System;
using System.Collections.Generic;
using System.Text;

namespace RoR2;
public class SkinDefParams
{
    public object r2api_skinSkillVariantsDef;
    public struct MeshReplacement
    {
        public object[] r2api_skillVariants;
    }
    public struct MinionSkinReplacement
    {
        public object[] r2api_skillVariants;
    }
    public struct ProjectileGhostReplacement
    {
        public object[] r2api_skillVariants;
    }
}
