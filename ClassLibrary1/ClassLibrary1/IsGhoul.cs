using Verse;
using RimWorld;

namespace TokyoGhoulMod
{
    public static class PawnExtensions
    {
        public static bool IsGhoul(this Pawn pawn)
        {
            if (pawn == null || pawn.genes == null)
                return false;

            bool result = pawn.genes.Xenotype == GhoulDefOf.TG_Ghoul;
            return result;
        }
    }
}