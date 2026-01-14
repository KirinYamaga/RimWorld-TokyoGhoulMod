using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

namespace TokyoGhoulMod
{
    [HarmonyPatch(typeof(Thing), "Ingested")]
    public static class Patch_Ingested
    {
        [HarmonyPrefix]
        public static bool Prefix(Thing __instance, Pawn ingester, ref float __result)
        {
            if (ingester?.genes == null) return true;
            if (ingester.genes.GetFirstGeneOfType<Gene_RCCells>() != null)
            {
                __result = 0f; // Гули живут на RC-клетках
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(Thing __instance, Pawn ingester, ref float __result)
        {
            if (ingester == null || __instance == null) return;

            Gene_RCCells gene = ingester.genes?.GetFirstGeneOfType<Gene_RCCells>();
            float nutrition = __instance.GetStatValue(StatDefOf.Nutrition);

            // Человек ест гуля
            if (gene == null)
            {
                if (IsGhoulMatter(__instance)) ApplyRCSyndrome(ingester);
                return;
            }

            // Гуль ест
            if (IsGhoulMatter(__instance))
            {
                gene.ConsumeGhoulMatter(nutrition);
                HediffDef highDef = DefDatabase<HediffDef>.GetNamed("TG_GhoulCannibalHigh", false);
                if (highDef != null) ingester.health.AddHediff(highDef);
            }
            else if (IsHumanlikeMeat(__instance))
            {
                gene.Value += nutrition * 0.5f;
            }
            else if (IsNormalFood(__instance))
            {
                ApplyGhoulishRejection(ingester, nutrition);
            }
        }

        private static bool IsGhoulMatter(Thing food)
        {
            if (food.def.defName == "Meat_Ghoul") return true;
            if (food is Corpse corpse && corpse.InnerPawn != null)
                return corpse.InnerPawn.genes?.GetFirstGeneOfType<Gene_RCCells>() != null;

            CompIngredients ingredients = food.TryGetComp<CompIngredients>();
            return ingredients != null && ingredients.ingredients.Any(d => d.defName == "Meat_Ghoul");
        }

        private static bool IsHumanlikeMeat(Thing food)
        {
            if (food.def == ThingDefOf.Meat_Human) return true;
            if (food is Corpse corpse && corpse.InnerPawn != null)
                return corpse.InnerPawn.RaceProps.Humanlike;

            CompIngredients ingredients = food.TryGetComp<CompIngredients>();
            return ingredients != null && ingredients.ingredients.Contains(ThingDefOf.Meat_Human);
        }

        private static bool IsNormalFood(Thing food)
        {
            if (food?.def?.ingestible == null) return false;
            return !IsGhoulMatter(food) && !IsHumanlikeMeat(food);
        }

        private static void ApplyRCSyndrome(Pawn pawn)
        {
            HediffDef syndromeDef = DefDatabase<HediffDef>.GetNamed("TG_RCSyndrome", false);
            if (syndromeDef != null)
            {
                Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(syndromeDef);
                if (existing != null) existing.Severity += 0.20f;
                else pawn.health.AddHediff(syndromeDef).Severity = 0.20f;
            }
            Messages.Message("TG_HumanAteGhoulMeat".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
        }

        private static void ApplyGhoulishRejection(Pawn pawn, float nutrition)
        {
            pawn.health.AddHediff(HediffDefOf.FoodPoisoning);
            if (pawn.needs.food != null) pawn.needs.food.CurLevel -= nutrition;
            Messages.Message("TokyoGhoul_GhoulsCantEatNormalFood".Translate(), pawn, MessageTypeDefOf.NegativeEvent);
        }
    }
}