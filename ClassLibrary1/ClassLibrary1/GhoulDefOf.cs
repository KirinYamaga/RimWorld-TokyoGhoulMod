using RimWorld;
using Verse;

namespace TokyoGhoulMod
{
    [DefOf]
    public static class GhoulDefOf
    {
        public static HediffDef TG_RinkakuKagune;

        public static XenotypeDef TG_Ghoul;

        static GhoulDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(GhoulDefOf));
        }
    }
}