using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace TokyoGhoulMod
{
    public class Gene_RCCells : Gene_Resource
    {
        // Базовые параметры расхода
        private const float DaysToStarve = 15f;
        private const float PassiveDrainPerDay = 1f / DaysToStarve;
        private const float KaguneDrainMultiplier = 15f;
        private const float UkakuDrainMultiplier = 25f;

        private const float ThresholdHungerLight = 0.30f;
        private const float ThresholdHungerCritical = 0.05f;

        public float cannibalismCount = 0f;

        public override float InitialResourceMax
        {
            get
            {
                // Полугуль может хранить 1.5 RC-клеток
                if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("TG_HalfGhoul", false)))
                    return 2.0f;
                return 1.0f;
            }
        }

        protected override Color BarColor => new Color(0.75f, 0f, 0f);
        protected override Color BarHighlightColor => new Color(1f, 0.2f, 0.2f);
        public override float MinLevelForAlert => 0.15f;
        public override string ResourceLabel => "TG_RCCellsLabel".Translate();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref cannibalismCount, "cannibalismCount", 0f);
        }

        public void ConsumeGhoulMatter(float nutrition)
        {
            cannibalismCount += nutrition;
            Value += nutrition * 0.8f;
            if (cannibalismCount > 20f && pawn.IsHashIntervalTick(2500))
                Messages.Message("TG_KakujaStirring".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.CautionInput);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                if (!(gizmo is GeneGizmo_Resource)) yield return gizmo;
            if (pawn.IsColonistPlayerControlled)
                yield return new GeneGizmo_RCCells(this, null, BarColor, BarHighlightColor);
        }

        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned || pawn.Dead) return;

            // Считаем количество активных боевых форм кагуне
            int activeKaguneCount = CountActiveKagunes();
            bool anyActive = activeKaguneCount > 0;

            if (pawn.IsHashIntervalTick(100))
            {
                ManageKakugan(anyActive);

                float currentMultiplier = PassiveDrainPerDay;
                if (anyActive)
                {
                    bool isUkaku = pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_Ukaku", false));
                    float baseActiveDrain = isUkaku ? UkakuDrainMultiplier : KaguneDrainMultiplier;

                    // БАЛАНС ХИМЕРЫ: Каждое дополнительное кагуне увеличивает расход на 70%
                    currentMultiplier *= baseActiveDrain * (1f + (activeKaguneCount - 1) * 0.7f);
                }

                Value -= (currentMultiplier / 60000f) * 100f;
                UpdateStatusEffects(anyActive);
                UpdateRCStarvationHediff();
            }

            if (pawn.needs.food != null && !anyActive)
                pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick * 0.95f;
        }

        private int CountActiveKagunes()
        {
            string[] tags = { "Hediff_Ukaku", "Hediff_Koukaku", "Hediff_Rinkaku", "Hediff_Bikaku" };
            return pawn.health.hediffSet.hediffs.Count(h => tags.Contains(h.def.defName));
        }

        // Остальные методы (UpdateRCStarvationHediff, ApplyStarvationDamage, ManageKakugan) остаются без изменений
        // ... (пропускаю для краткости, они должны быть в коде)

        private void UpdateRCStarvationHediff()
        {
            HediffDef starvationDef = DefDatabase<HediffDef>.GetNamed("RCStarvation", false);
            if (starvationDef == null) return;
            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(starvationDef);
            if (Value < ThresholdHungerLight)
            {
                if (existing == null) existing = pawn.health.AddHediff(starvationDef);
                existing.Severity = 1.0f - (Value / ThresholdHungerLight);
                if (Value < ThresholdHungerCritical && !pawn.InMentalState && !pawn.Downed && Rand.Value < 0.05f)
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "TG_GhoulHungerReason".Translate(), true);
                if (Value <= 0.001f && existing.Severity >= 0.95f) ApplyStarvationDamage();
            }
            else if (existing != null)
            {
                existing.Severity -= 0.05f;
                if (existing.Severity <= 0) pawn.health.RemoveHediff(existing);
            }
        }

        private void ApplyStarvationDamage()
        {
            if (pawn.jobs?.curJob != null && (pawn.jobs.curJob.def == JobDefOf.LayDown || pawn.jobs.curJob.def == JobDefOf.Wait_MaintainPosture))
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            pawn.TakeDamage(new DamageInfo(DamageDefOf.Stab, 1.2f, 0, -1, null, pawn.RaceProps.body.corePart));
            if (pawn.IsHashIntervalTick(1000))
                Messages.Message("TG_GhoulStarvingToDeath".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
        }

        private void UpdateStatusEffects(bool isKaguneActive)
        {
            if (Value <= 0 && isKaguneActive)
            {
                string[] tags = { "Hediff_Ukaku", "Hediff_Koukaku", "Hediff_Rinkaku", "Hediff_Bikaku" };
                var toRemove = pawn.health.hediffSet.hediffs.Where(h => tags.Contains(h.def.defName)).ToList();
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
                var eyes = pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def == BodyPartDefOf.Eye).ToList();

                // ПРОВЕРКА НА ПОЛУГУЛЯ
                if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("TG_HalfGhoul", false)))
                {
                    // Берем только один глаз (например, левый)
                    if (eyes.Count > 0) pawn.health.AddHediff(kakuganDef, eyes[0]);
                }
                else
                {
                    // Обычный гуль - оба глаза
                    foreach (var part in eyes) pawn.health.AddHediff(kakuganDef, part);
                }
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
            else if (!shouldHave && currentlyHas)
            {
                pawn.health.hediffSet.hediffs.RemoveAll(x => x.def == kakuganDef);
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();
            Value = 0.5f;

            // 1. ЗАПУСК ГЕНЕТИКИ (Наследование или рандом)
            GeneticsHelper.InitializeGhoulishTraits(pawn);

            // 2. ВЫДАЧА ВСЕХ ОРГАНОВ КАКОХО ДЛЯ ХИМЕР
            string[] types = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
            foreach (var type in types)
            {
                GeneDef typeGene = DefDatabase<GeneDef>.GetNamed("Gene_" + type, false);
                if (pawn.genes.HasActiveGene(typeGene))
                {
                    HediffDef kakuhoDef = DefDatabase<HediffDef>.GetNamed("Hediff_Kakuho" + type, false);
                    // Добавляем орган, если его еще нет
                    if (kakuhoDef != null && !pawn.health.hediffSet.HasHediff(kakuhoDef))
                    {
                        pawn.health.AddHediff(kakuhoDef, pawn.RaceProps.body.corePart);
                    }
                }
            }
        }
    }
}