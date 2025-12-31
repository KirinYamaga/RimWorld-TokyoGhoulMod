using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace TokyoGhoulMod
{
    public class Recipe_InstallKakuho : Recipe_InstallImplant
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            if (pawn.genes == null) return;

            GeneDef ghoulBio = DefDatabase<GeneDef>.GetNamed("Gene_RCCells", false);
            if (ghoulBio != null && !pawn.genes.HasActiveGene(ghoulBio))
            {
                pawn.genes.AddGene(ghoulBio, true);
            }

            // Ищем тип по названию рецепта (например InstallKakuhoUkaku)
            string type = "";
            if (recipe.defName.Contains("Ukaku")) type = "Ukaku";
            else if (recipe.defName.Contains("Koukaku")) type = "Koukaku";
            else if (recipe.defName.Contains("Rinkaku")) type = "Rinkaku";
            else if (recipe.defName.Contains("Bikaku")) type = "Bikaku";

            if (!string.IsNullOrEmpty(type))
            {
                GeneDef kaguneGene = DefDatabase<GeneDef>.GetNamed("Gene_" + type, false);
                if (kaguneGene != null && !pawn.genes.HasActiveGene(kaguneGene))
                {
                    pawn.genes.AddGene(kaguneGene, true);
                }
            }
        }
    }

    public class Recipe_RemoveKakuho : RecipeWorker
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            return pawn.health.hediffSet.hediffs
                .Where(h => h.def.defName.Contains("Kakuho"))
                .Select(h => h.Part)
                .Distinct();
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            Hediff kakuho = pawn.health.hediffSet.hediffs.FirstOrDefault(h => h.Part == part && h.def.defName.Contains("Kakuho"));

            if (kakuho != null)
            {
                string type = "";
                if (kakuho.def.defName.Contains("Ukaku")) type = "Ukaku";
                else if (kakuho.def.defName.Contains("Koukaku")) type = "Koukaku";
                else if (kakuho.def.defName.Contains("Rinkaku")) type = "Rinkaku";
                else if (kakuho.def.defName.Contains("Bikaku")) type = "Bikaku";

                // 1. УДАЛЯЕМ ГЕН ТИПА
                if (!string.IsNullOrEmpty(type))
                {
                    GeneDef geneDef = DefDatabase<GeneDef>.GetNamed("Gene_" + type, false);
                    if (geneDef != null)
                    {
                        Gene targetGene = pawn.genes?.GetGene(geneDef);
                        if (targetGene != null)
                        {
                            pawn.genes.RemoveGene(targetGene);
                        }
                    }
                }

                // 2. УДАЛЯЕМ БИОЛОГИЮ ГУЛЯ (Gene_RCCells)
                Gene bioGene = pawn.genes?.GetGene(DefDatabase<GeneDef>.GetNamed("Gene_RCCells", false));
                if (bioGene != null) pawn.genes.RemoveGene(bioGene);

                // 3. ВЫПАДЕНИЕ ПРЕДМЕТА
                string itemName = kakuho.def.defName + "_Item";
                if (itemName.Contains("Koukaku")) itemName = "Kakuho_Koukaku_Item";
                ThingDef itemDef = DefDatabase<ThingDef>.GetNamed(itemName, false) ?? DefDatabase<ThingDef>.GetNamed("Kakuho_Rinkaku_Item");

                if (pawn.Map != null)
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(itemDef), pawn.Position, pawn.Map, ThingPlaceMode.Near);

                pawn.health.RemoveHediff(kakuho);
                Messages.Message("TG_GhoulCured".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}