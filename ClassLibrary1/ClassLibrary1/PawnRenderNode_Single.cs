using Verse;
using RimWorld;
using UnityEngine;

namespace TokyoGhoulMod
{
    public class PawnRenderNodeProperties_Single : PawnRenderNodeProperties
    {
        public float baseScale = 1f;
        public PawnRenderNodeProperties_Single()
        {
            nodeClass = typeof(PawnRenderNode_Single);
            workerClass = typeof(PawnRenderNodeWorker_Single);
        }
    }

    public class PawnRenderNodeWorker_Single : PawnRenderNodeWorker_Eye
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            Vector3 result = base.ScaleFor(node, parms);
            if (node.Props is PawnRenderNodeProperties_Single props)
            {
                // Для глаз 1.0 — это уже много. Множим базу на наш baseScale
                result.x *= props.baseScale;
                result.z *= props.baseScale;
            }
            return result;
        }
    }

    public class PawnRenderNode_Single : PawnRenderNode
    {
        public PawnRenderNode_Single(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        public override Graphic GraphicFor(Pawn pawn)
        {
            string text = TexPathFor(pawn);
            if (text.NullOrEmpty()) return null;

            // Используем drawSize (1,1), масштаб будет контролироваться воркером выше
            return GraphicDatabase.Get<Graphic_Single>(text, ShaderFor(pawn), Vector2.one, ColorFor(pawn));
        }
    }
}