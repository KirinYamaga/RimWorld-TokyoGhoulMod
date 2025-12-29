using Verse;
using RimWorld;
using UnityEngine;

namespace TokyoGhoulMod
{
    // 1. Worker управляет логикой "когда рисовать"
    public class PawnRenderNodeWorker_Kagune : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            // Сначала выполняем базовые проверки (жив ли, не скелет ли и т.д.)
            if (!base.CanDrawNow(node, parms))
                return false;

            // В 1.6 пешка достается через node.tree.pawn
            Pawn pawn = node.tree?.pawn;

            if (pawn?.health?.hediffSet == null)
                return false;

            // Возвращаем true, если у пешки есть хедифф активного кагуне
            return pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("KaguneRinkaku", false));
        }
    }

    // 2. Сам узел (Node)
    public class PawnRenderNode_Kagune : PawnRenderNode
    {
        public PawnRenderNode_Kagune(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }
    }

    // 3. Свойства (Properties) для связи с XML
    public class PawnRenderNodeProperties_Kagune : PawnRenderNodeProperties
    {
        public PawnRenderNodeProperties_Kagune()
        {
            nodeClass = typeof(PawnRenderNode_Kagune);
            workerClass = typeof(PawnRenderNodeWorker_Kagune);
        }
    }
}