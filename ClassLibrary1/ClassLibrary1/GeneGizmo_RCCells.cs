using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace TokyoGhoulMod
{
    public class GeneGizmo_RCCells : GeneGizmo_Resource
    {
        // Конструктор, принимающий 4 параметра
        public GeneGizmo_RCCells(Gene_Resource gene, List<IGeneResourceDrain> drainDefs, Color barColor, Color barHighlightColor)
            : base(gene, drainDefs, barColor, barHighlightColor)
        {
        }

        // Обязательные поля для слайдеров в 1.6
        protected override bool DraggingBar { get; set; }

        protected override string GetTooltip()
        {
            return "Запас RC-клеток. Необходим для активации кагуне и регенерации.";
        }
    }
}