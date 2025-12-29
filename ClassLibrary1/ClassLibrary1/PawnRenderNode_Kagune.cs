using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace TokyoGhoulMod
{
    // 1. Свойства: Добавляем поле kaguneTag для использования в XML
    public class PawnRenderNodeProperties_Kagune : PawnRenderNodeProperties
    {
        public string kaguneTag; // Ukaku, Koukaku, Rinkaku, Bikaku

        public PawnRenderNodeProperties_Kagune()
        {
            nodeClass = typeof(PawnRenderNode_Kagune);
            workerClass = typeof(PawnRenderNodeWorker_Kagune);
        }
    }

    // 2. Worker: Логика отрисовки
    public class PawnRenderNodeWorker_Kagune : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;

            Pawn pawn = node.tree?.pawn;
            if (pawn?.health?.hediffSet == null) return false;

            // ИСПРАВЛЕНИЕ: Используем Props с большой буквы
            var kaguneProps = node.Props as PawnRenderNodeProperties_Kagune;

            if (kaguneProps == null || string.IsNullOrEmpty(kaguneProps.kaguneTag))
                return false;

            // Ищем хедифф: Hediff_ + тег (например, Hediff_Ukaku)
            string requiredHediffName = "Hediff_" + kaguneProps.kaguneTag;

            // Проверяем наличие хедиффа в списке
            return pawn.health.hediffSet.hediffs.Any(x => x.def.defName == requiredHediffName);
        }
    }

    // 3. Узел
    public class PawnRenderNode_Kagune : PawnRenderNode
    {
        public PawnRenderNode_Kagune(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }
    }
}