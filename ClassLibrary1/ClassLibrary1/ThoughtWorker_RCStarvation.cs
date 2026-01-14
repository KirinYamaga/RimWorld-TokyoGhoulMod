using RimWorld;
using Verse;

namespace TokyoGhoulMod
{
    public class ThoughtWorker_RCStarvation : ThoughtWorker
    {
        protected override ThoughtState CurrentStateInternal(Pawn p)
        {
            if (p.genes == null) return ThoughtState.Inactive;

            Gene_RCCells gene = p.genes.GetFirstGeneOfType<Gene_RCCells>();
            if (gene == null) return ThoughtState.Inactive;

            // Привязываем стадии мысли к уровню RC-клеток
            if (gene.Value < 0.05f) return ThoughtState.ActiveAtStage(2); // Критический
            if (gene.Value < 0.15f) return ThoughtState.ActiveAtStage(1); // Сильный
            if (gene.Value < 0.30f) return ThoughtState.ActiveAtStage(0); // Начальный

            return ThoughtState.Inactive;
        }
    }
}