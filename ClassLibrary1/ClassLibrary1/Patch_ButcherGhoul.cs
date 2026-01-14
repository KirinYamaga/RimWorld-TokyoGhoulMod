using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace TokyoGhoulMod
{
    [HarmonyPatch(typeof(Corpse), "ButcherProducts")]
    public static class Patch_ButcherGhoul
    {
        [HarmonyPostfix]
        public static void Postfix(Corpse __instance, ref IEnumerable<Thing> __result, Pawn butcher, float efficiency)
        {
            Pawn victim = __instance.InnerPawn;

            // Проверяем, является ли жертва гулем (наличие гена RC-клеток)
            if (victim?.genes == null || victim.genes.GetFirstGeneOfType<Gene_RCCells>() == null)
                return;

            List<Thing> products = __result.ToList();
            ThingDef ghoulMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Ghoul", false);

            if (ghoulMeatDef == null) return;

            // 1. Заменяем обычную человечину на мясо гуля
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].def == ThingDefOf.Meat_Human)
                {
                    int count = products[i].stackCount;
                    Thing newMeat = ThingMaker.MakeThing(ghoulMeatDef);
                    newMeat.stackCount = count;
                    products[i] = newMeat;
                }
            }

            // 2. Логика выпадения Какухо (Шанс 10%)
            if (Rand.Value < 0.10f)
            {
                string kakuhoType = GetKakuhoTypeFromPawn(victim);
                if (!string.IsNullOrEmpty(kakuhoType))
                {
                    // Собираем имя дефа предмета, например: Kakuho_Ukaku_Item
                    string itemDefName = "Kakuho_" + kakuhoType + "_Item";
                    ThingDef kakuhoItemDef = DefDatabase<ThingDef>.GetNamed(itemDefName, false);

                    if (kakuhoItemDef != null)
                    {
                        Thing kakuho = ThingMaker.MakeThing(kakuhoItemDef);
                        products.Add(kakuho);

                        // Сообщение игроку об удачной находке
                        Messages.Message("TG_KakuhoFoundDuringButchery".Translate(victim.LabelShort, kakuho.Label),
                            new LookTargets(__instance), MessageTypeDefOf.PositiveEvent);
                    }
                }
            }

            __result = products;
        }

        private static string GetKakuhoTypeFromPawn(Pawn p)
        {
            // Ищем хедифф какухо, чтобы понять тип
            if (p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_KakuhoUkaku", false))) return "Ukaku";
            if (p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_KakuhoKoukaku", false))) return "Koukaku";
            if (p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_KakuhoRinkaku", false))) return "Rinkaku";
            if (p.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_KakuhoBikaku", false))) return "Bikaku";

            return null;
        }
    }
}