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
        private const float KaguneDrainMultiplier = 15f;
        private const float UkakuDrainMultiplier = 25f;

        public override float InitialResourceMax => 1f;
        protected override Color BarColor => new Color(0.75f, 0f, 0f);
        protected override Color BarHighlightColor => new Color(1f, 0.2f, 0.2f);
        public override float MinLevelForAlert => 0.15f;
        public override string ResourceLabel => "RC-клетки";

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!(gizmo is GeneGizmo_Resource)) yield return gizmo;
            }
            if (pawn.IsColonistPlayerControlled)
            {
                yield return new GeneGizmo_RCCells(this, null, BarColor, BarHighlightColor);
            }
        }

        public override void Tick()
        {
            base.Tick();

            bool active = IsKaguneActive(); // Кэшируем состояние для текущего тика

            if (pawn.IsHashIntervalTick(100))
            {
                bool isUkaku = pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_Ukaku"));

                ManageKakugan(active);

                float currentMultiplier = PassiveDrainPerDay;
                if (active)
                {
                    currentMultiplier *= isUkaku ? UkakuDrainMultiplier : KaguneDrainMultiplier;
                }

                Value -= (currentMultiplier / 60000f) * 100f;
                UpdateStatusEffects(active);
            }

            // Компенсация голода
            if (pawn.needs.food != null && !active)
            {
                pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick * 0.95f;
            }
        }

        private bool IsKaguneActive()
        {
            // Проверяем только боевые формы (те, что начинаются на Hediff_ и не являются Какухо)
            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName == "Hediff_Ukaku" ||
                h.def.defName == "Hediff_Koukaku" ||
                h.def.defName == "Hediff_Rinkaku" ||
                h.def.defName == "Hediff_Bikaku");
        }

        private void UpdateStatusEffects(bool isKaguneActive)
        {
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            if (pawn.needs.food != null && pawn.needs.food.CurLevel <= 0.01f && Value > 0.1f)
            {
                if (!pawn.InMentalState && !pawn.Downed && !IsDeathresting(pawn))
                {
                    if (Rand.Value < 0.1f)
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Голод гуля", true);
                    }
                }
            }

            if (Value <= 0 && isKaguneActive)
            {
                string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
                var toRemove = pawn.health.hediffSet.hediffs
                    .Where(h => tags.Any(tag => h.def.defName == "Hediff_" + tag))
                    .ToList();

                foreach (var h in toRemove) pawn.health.RemoveHediff(h);

                Messages.Message("TokyoGhoul_KaguneRetractedCells".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent);
                pawn.Drawer.renderer.SetAllGraphicsDirty(); // Обязательно обновляем визуал
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
            Value = 0.5f;

            // 1. ПРОВЕРКА НАЛИЧИЯ ОРГАНА (чтобы не дублировать при операции)
            bool alreadyHasKakuho = pawn.health.hediffSet.hediffs.Any(h => h.def.defName.Contains("Kakuho"));

            // 2. АВТОМАТИЧЕСКИЙ ВЫБОР ТИПА (для врожденных гулей)
            bool hasKaguneGene = pawn.genes.GenesListForReading.Any(g =>
                g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneType"));

            if (!hasKaguneGene)
            {
                string[] types = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
                string chosenType = types.RandomElement();

                GeneDef geneDef = DefDatabase<GeneDef>.GetNamed("Gene_" + chosenType, false);
                HediffDef kakuhoDef = DefDatabase<HediffDef>.GetNamed("Hediff_Kakuho" + chosenType, false);

                if (geneDef != null)
                {
                    pawn.genes.AddGene(geneDef, true);
                    if (geneDef.abilities != null)
                    {
                        foreach (AbilityDef ab in geneDef.abilities)
                        {
                            if (!pawn.abilities.abilities.Any(a => a.def == ab))
                                pawn.abilities.GainAbility(ab);
                        }
                    }
                }

                // Выдаем орган только если его вообще нет (ни общего, ни специфического)
                if (!alreadyHasKakuho && kakuhoDef != null)
                {
                    pawn.health.AddHediff(kakuhoDef, pawn.RaceProps.body.corePart);
                }

                Messages.Message("TG_KaguneTypeAwakened".Translate(pawn.LabelShort, geneDef.label), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}