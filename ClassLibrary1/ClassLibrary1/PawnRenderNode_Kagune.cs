using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;

namespace TokyoGhoulMod
{
    public class PawnRenderNodeProperties_Kagune : PawnRenderNodeProperties
    {
        public string kaguneTag;
        public float baseScale = 1f;

        public PawnRenderNodeProperties_Kagune()
        {
            nodeClass = typeof(PawnRenderNode_Kagune);
            workerClass = typeof(PawnRenderNodeWorker_Kagune);
        }
    }

    public class PawnRenderNodeWorker_Kagune : PawnRenderNodeWorker
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms)) return false;
            Pawn pawn = node.tree?.pawn;
            if (pawn?.health?.hediffSet == null) return false;

            var kaguneProps = node.Props as PawnRenderNodeProperties_Kagune;
            if (kaguneProps == null || string.IsNullOrEmpty(kaguneProps.kaguneTag)) return false;

            string requiredHediffName = "Hediff_" + kaguneProps.kaguneTag;
            return pawn.health.hediffSet.hediffs.Any(x => x.def.defName == requiredHediffName);
        }

        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 result = base.ScaleFor(node, parms);
            if (node.Props is PawnRenderNodeProperties_Kagune props) result *= props.baseScale;
            return result;
        }
    }

    public class PawnRenderNode_Kagune : PawnRenderNode
    {
        public PawnRenderNode_Kagune(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        public override Color ColorFor(Pawn pawn)
        {
            if (pawn.genes != null)
            {
                // ИСПРАВЛЕНО: Проверяем g.Active, чтобы учитывать систему доминирования генов
                Gene colorGene = pawn.genes.GenesListForReading.FirstOrDefault(g =>
                    g.Active &&
                    g.def.exclusionTags != null &&
                    g.def.exclusionTags.Contains("KaguneColor"));

                if (colorGene != null && colorGene.def.renderNodeProperties != null)
                {
                    var colorProp = colorGene.def.renderNodeProperties.FirstOrDefault();
                    if (colorProp != null && colorProp.color.HasValue)
                    {
                        return colorProp.color.Value;
                    }
                }
            }
            return new Color(0.8f, 0.1f, 0.1f); // Дефолтный красный
        }
    }
}