using Verse;
using RimWorld;
using UnityEngine;

namespace TokyoGhoulMod
{
    // Свойства
    public class PawnRenderNodeProperties_Single : PawnRenderNodeProperties
    {
        public float baseScale = 1f; // Поле для XML

        public PawnRenderNodeProperties_Single()
        {
            nodeClass = typeof(PawnRenderNode_Single);
            workerClass = typeof(PawnRenderNodeWorker_Single);
        }
    }

    // Воркер
    public class PawnRenderNodeWorker_Single : PawnRenderNodeWorker_Eye
    {
        public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
        {
            // Возвращаем стандартный масштаб глаз (воркер глаза сам делает их маленькими)
            return base.ScaleFor(node, parms);
        }
    }

    // Узел
    public class PawnRenderNode_Single : PawnRenderNode
    {
        public PawnRenderNode_Single(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
            : base(pawn, props, tree) { }

        public override Graphic GraphicFor(Pawn pawn)
        {
            string text = TexPathFor(pawn);
            if (text.NullOrEmpty()) return null;

            float scale = 1f;
            // ПРОВЕРКА: Пытаемся достать масштаб из свойств
            if (props is PawnRenderNodeProperties_Single customProps)
            {
                scale = customProps.baseScale;
            }

            // Устанавливаем размер графики (drawSize) на основе нашего масштаба
            // Именно это значение физически уменьшит текстуру внутри игры
            return GraphicDatabase.Get<Graphic_Single>(text, ShaderFor(pawn), new Vector2(scale, scale), ColorFor(pawn));
        }
    }
}