using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TokyoGhoulMod
{
    [HarmonyPatch(typeof(Thing), "Ingested")]
    public static class Patch_Ingested
    {
        // В Префиксе мы только подменяем число питательности на 0
        [HarmonyPrefix]
        public static bool Prefix(Thing __instance, Pawn ingester, ref float __result)
        {
            if (ingester?.genes == null) return true;

            Gene_RCCells gene = ingester.genes.GetFirstGeneOfType<Gene_RCCells>();
            if (gene == null) return true;

            // Если это обычная еда (не человечина)
            if (!IsHumanlikeMeat(__instance) && IsNormalFood(__instance))
            {
                // Принудительно ставим результат в 0, чтобы шкала голода не росла
                __result = 0f;
            }

            return true;
        }

        // В Постфиксе мы начисляем RC-клетки ИЛИ вызываем рвоту
        [HarmonyPostfix]
        public static void Postfix(Thing __instance, Pawn ingester, float __result)
        {
            if (ingester?.genes == null) return;

            Gene_RCCells gene = ingester.genes.GetFirstGeneOfType<Gene_RCCells>();
            if (gene == null) return;

            if (IsHumanlikeMeat(__instance))
            {
                // Начисляем RC-клетки (используем константу, так как __result теперь может быть 0)
                // Рекомендую использовать питание самого предмета: __instance.def.ingestible.CachedNutrition
                float nutritionValue = __instance.def.ingestible?.CachedNutrition ?? 0.1f;
                gene.Value += nutritionValue * 0.5f;
            }
            else if (IsNormalFood(__instance))
            {
                // Вызываем рвоту БЕЗ прерывания джоба (игра сама завершит его после Ingested)
                ApplyGhoulishRejection(ingester);
            }
        }

        private static bool IsHumanlikeMeat(Thing food)
        {
            // Проверка на уничтоженный объект (безопасность)
            if (food == null) return false;

            if (food.def == ThingDefOf.Meat_Human) return true;
            if (food is Corpse corpse && corpse.InnerPawn.RaceProps.Humanlike) return true;

            CompIngredients ingredients = food.TryGetComp<CompIngredients>();
            if (ingredients != null)
            {
                return ingredients.ingredients.Contains(ThingDefOf.Meat_Human);
            }
            return false;
        }

        private static bool IsNormalFood(Thing food)
        {
            if (food?.def?.ingestible == null) return false;
            FoodTypeFlags flags = food.def.ingestible.foodType;

            return flags.HasFlag(FoodTypeFlags.VegetableOrFruit) ||
                   flags.HasFlag(FoodTypeFlags.Meat) ||
                   flags.HasFlag(FoodTypeFlags.AnimalProduct) ||
                   flags.HasFlag(FoodTypeFlags.Meal) ||
                   flags.HasFlag(FoodTypeFlags.Processed) ||
                   flags.HasFlag(FoodTypeFlags.Liquor) ||
                   flags.HasFlag(FoodTypeFlags.Fungus);
        }

        private static void ApplyGhoulishRejection(Pawn pawn)
        {
            // Просто добавляем работу рвоты в очередь или запускаем её.
            // На этом этапе предмет уже "съеден" и удален, ошибки не будет.
            Job vomitJob = JobMaker.MakeJob(JobDefOf.Vomit);
            pawn.jobs.StartJob(vomitJob, JobCondition.InterruptForced, null, true);

            pawn.health.AddHediff(HediffDefOf.FoodPoisoning, null, null);
            Messages.Message("Гули не могут переваривать обычную пищу!", pawn, MessageTypeDefOf.NegativeEvent);
        }
    }
}