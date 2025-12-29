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

        public override float InitialResourceMax => 1f;
        protected override Color BarColor => new Color(0.75f, 0f, 0f);
        protected override Color BarHighlightColor => new Color(1f, 0.2f, 0.2f);
        public override float MinLevelForAlert => 0.15f;
        public override string ResourceLabel => "RC-клетки";

        // Пороги для отображения на шкале (заменяет отсутствующий resourceGizmoThresholds)
        private static readonly List<float> Thresholds = new List<float> { 0.2f, 0.5f, 0.8f };

        public override IEnumerable<Gizmo> GetGizmos()
        {
            // Сначала возвращаем базовые гизмо, пропуская стандартную шкалу ресурса
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!(gizmo is GeneGizmo_Resource)) yield return gizmo;
            }

            // Добавляем нашу кастомную шкалу
            if (pawn.IsColonistPlayerControlled)
            {
                // Аргументы: (сам ген, список потребителей ресурса, основной цвет, цвет подсветки)
                // Мы передаем null в качестве списка потребителей, так как RC-клетки тратит код, а не другие гены.
                yield return new GeneGizmo_RCCells(this, null, BarColor, BarHighlightColor);
            }
        }

        public override void Tick()
        {
            base.Tick();

            // Расчет кагуне и глаз делаем раз в 100 тиков для оптимизации
            if (pawn.IsHashIntervalTick(100))
            {
                bool isActive = pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("KaguneRinkaku", false));
                ManageKakugan(isActive);

                float dailyDrain = isActive ? (PassiveDrainPerDay * KaguneDrainMultiplier) : PassiveDrainPerDay;
                Value -= (dailyDrain / 60000f) * 100f;
            }

            // ЛОГИКА ГОЛОДА ДОЛЖНА РАБОТАТЬ КАЖДЫЙ ТИК (без IntervalTick)
            if (pawn.needs.food != null)
            {
                bool isActive = pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("KaguneRinkaku", false));

                if (!isActive)
                {
                    // Каждые 20 тиков позволяем еде упасть. В остальное время — восстанавливаем.
                    // 1/20 = 5% скорости голода. Это обеспечит примерно 12-15 дней.
                    if (!pawn.IsHashIntervalTick(20))
                    {
                        pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick;
                    }
                }
            }
        }

        // Остальные методы (ManageKakugan, UpdateStatusEffects, PostAdd) остаются без изменений
        private void ManageKakugan(bool isKaguneActive)
        {
            bool shouldHave = isKaguneActive || pawn.InMentalState;
            HediffDef kakuganDef = DefDatabase<HediffDef>.GetNamed("Kakugan", false);
            if (kakuganDef == null) return;
            bool currentlyHas = pawn.health.hediffSet.HasHediff(kakuganDef);

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

        private void UpdateStatusEffects(bool isKaguneActive)
        {
            if (pawn == null || !pawn.Spawned) return;
            if (pawn.needs.food != null && pawn.needs.food.CurLevel <= 0.01f && Value > 0.1f)
            {
                if (!pawn.InMentalState && !IsDeathresting(pawn) && Rand.Value < 0.1f)
                {
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Голод гуля", true);
                }
            }
            if (Value <= 0 && isKaguneActive)
            {
                Hediff kagu = pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("KaguneRinkaku"));
                if (kagu != null) pawn.health.RemoveHediff(kagu);
            }
        }

        private bool IsDeathresting(Pawn p) => ModsConfig.BiotechActive && p.Deathresting;

        public override void PostAdd()
        {
            base.PostAdd();
            if (!pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Kakuho", false)))
                pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Kakuho"), pawn.RaceProps.body.corePart);
            Value = 0.5f;
        }
    }
}