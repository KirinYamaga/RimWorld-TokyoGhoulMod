using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TokyoGhoulMod
{
    public class Gene_RCCells : Gene_Resource
    {
        private const float DaysToStarve = 15f;
        private const float PassiveDrainPerDay = 1f / DaysToStarve;

        // Баланс расхода: Укаку потребляет в 25 раз больше нормы, остальные в 15 раз
        private const float KaguneDrainMultiplier = 15f;
        private const float UkakuDrainMultiplier = 25f;

        public override float InitialResourceMax => 1f;
        protected override Color BarColor => new Color(0.75f, 0f, 0f);
        protected override Color BarHighlightColor => new Color(1f, 0.2f, 0.2f);
        public override float MinLevelForAlert => 0.15f;
        public override string ResourceLabel => "RC-клетки";

        private static readonly List<float> Thresholds = new List<float> { 0.2f, 0.5f, 0.8f };

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!(gizmo is GeneGizmo_Resource)) yield return gizmo;
            }

            if (pawn.IsColonistPlayerControlled)
            {
                // Используем конструктор, который мы поправили в GeneGizmo_RCCells
                yield return new GeneGizmo_RCCells(this, null, BarColor, BarHighlightColor);
            }
        }

        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(100))
            {
                // Проверяем типы кагуне
                bool isUkakuActive = pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_Ukaku"));
                bool isAnyActive = IsKaguneActive();

                ManageKakugan(isAnyActive);

                // Расчет множителя расхода RC-клеток
                float currentMultiplier = PassiveDrainPerDay;
                if (isAnyActive)
                {
                    // Укаку сжигает клетки значительно быстрее (стеклянная пушка)
                    currentMultiplier *= isUkakuActive ? UkakuDrainMultiplier : KaguneDrainMultiplier;
                }

                Value -= (currentMultiplier / 60000f) * 100f;

                UpdateStatusEffects(isAnyActive);
            }

            // ЛОГИКА ГОЛОДА: Если кагуне не активно, замедляем голод на 95%
            // Это позволяет гулю игнорировать штрафы метаболизма от других генов
            if (pawn.needs.food != null && !IsKaguneActive())
            {
                // Возвращаем пешке 95% от того, что она только что потеряла
                pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick * 0.95f;
            }
        }

        // Универсальная проверка активного состояния любого кагуне
        private bool IsKaguneActive()
        {
            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName == "Hediff_Ukaku" ||
                h.def.defName == "Hediff_Koukaku" ||
                h.def.defName == "Hediff_Rinkaku" ||
                h.def.defName == "Hediff_Bikaku");
        }

        private void UpdateStatusEffects(bool isKaguneActive)
        {
            if (pawn == null || !pawn.Spawned) return;

            // Срыв в берсерк при критическом голоде (еда на нуле, но есть RC-клетки)
            if (pawn.needs.food != null && pawn.needs.food.CurLevel <= 0.01f && Value > 0.1f)
            {
                if (!pawn.InMentalState && !IsDeathresting(pawn) && Rand.Value < 0.1f)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Голод гуля", true);
                }
            }

            // Если RC-клетки кончились, принудительно втягиваем ЛЮБОЕ кагуне
            if (Value <= 0 && isKaguneActive)
            {
                string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
                var toRemove = pawn.health.hediffSet.hediffs
                    .Where(h => tags.Any(tag => h.def.defName == "Hediff_" + tag))
                    .ToList();

                foreach (var h in toRemove) pawn.health.RemoveHediff(h);

                Messages.Message("TokyoGhoul_KaguneRetractedCells".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        private void ManageKakugan(bool isKaguneActive)
        {
            HediffDef kakuganDef = DefDatabase<HediffDef>.GetNamed("Kakugan", false);
            if (kakuganDef == null) return;

            bool currentlyHas = pawn.health.hediffSet.HasHediff(kakuganDef);
            bool shouldHave = isKaguneActive || pawn.InMentalState;

            if (shouldHave && !currentlyHas)
            {
                var eyes = pawn.RaceProps.body.AllParts.Where(p => p.def == BodyPartDefOf.Eye);
                foreach (var part in eyes) pawn.health.AddHediff(kakuganDef, part);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            else if (!shouldHave && currentlyHas)
            {
                pawn.health.hediffSet.hediffs.RemoveAll(x => x.def == kakuganDef);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        private bool IsDeathresting(Pawn p) => ModsConfig.BiotechActive && p.Deathresting;

        public override void PostAdd()
        {
            base.PostAdd();

            // 1. Выдаем Какухо
            if (!pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Kakuho", false)))
            {
                pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Kakuho"), pawn.RaceProps.body.corePart);
            }

            // 2. Устанавливаем начальное значение RC-клеток
            this.Value = 0.5f;

            // 3. Автоматический выбор типа кагуне
            bool hasKagune = pawn.genes.GenesListForReading.Any(g =>
                g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneType"));

            if (!hasKagune)
            {
                string[] types = { "Gene_Ukaku", "Gene_Koukaku", "Gene_Rinkaku", "Gene_Bikaku" };
                string chosenType = types.RandomElement();

                GeneDef geneDef = DefDatabase<GeneDef>.GetNamed(chosenType, false);
                if (geneDef != null)
                {
                    pawn.genes.AddGene(geneDef, true);

                    // ПРИНУДИТЕЛЬНАЯ ВЫДАЧА СПОСОБНОСТЕЙ
                    // Это гарантирует появление кнопок сразу после добавления гена
                    if (geneDef.abilities != null)
                    {
                        foreach (AbilityDef ab in geneDef.abilities)
                        {
                            if (!pawn.abilities.abilities.Any(a => a.def == ab))
                            {
                                pawn.abilities.GainAbility(ab);
                            }
                        }
                    }

                    Messages.Message("У " + pawn.LabelShort + " проявился тип RC-клеток: " + geneDef.label, pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}