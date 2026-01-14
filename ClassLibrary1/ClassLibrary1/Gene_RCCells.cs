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
        private const float DaysToStarve = 15f;
        private const float PassiveDrainPerDay = 1f / DaysToStarve;
        private const float KaguneDrainMultiplier = 15f;
        private const float UkakuDrainMultiplier = 25f;

        private const float ThresholdHungerLight = 0.30f;
        private const float ThresholdHungerCritical = 0.05f;

        public float cannibalismCount = 0f;

        public override float InitialResourceMax => 1f;
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
            {
                Messages.Message("TG_KakujaStirring".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.CautionInput);
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                if (!(gizmo is GeneGizmo_Resource)) yield return gizmo;
            }
            if (pawn.IsColonistPlayerControlled)
            {
                // Исправленный вызов Гизмо
                yield return new GeneGizmo_RCCells(this, null, BarColor, BarHighlightColor);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!pawn.Spawned || pawn.Dead) return;

            bool active = IsKaguneActive();

            if (pawn.IsHashIntervalTick(100))
            {
                ManageKakugan(active);

                float currentMultiplier = PassiveDrainPerDay;
                if (active)
                {
                    bool isUkaku = pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Hediff_Ukaku", false));
                    currentMultiplier *= isUkaku ? UkakuDrainMultiplier : KaguneDrainMultiplier;
                }

                Value -= (currentMultiplier / 60000f) * 100f;

                UpdateStatusEffects(active);
                UpdateRCStarvationHediff();
            }

            if (pawn.needs.food != null && !active)
            {
                pawn.needs.food.CurLevel += pawn.needs.food.FoodFallPerTick * 0.95f;
            }
        }

        private bool IsKaguneActive()
        {
            if (pawn.health?.hediffSet?.hediffs == null) return false;
            return pawn.health.hediffSet.hediffs.Any(h =>
                h.def.defName == "Hediff_Ukaku" ||
                h.def.defName == "Hediff_Koukaku" ||
                h.def.defName == "Hediff_Rinkaku" ||
                h.def.defName == "Hediff_Bikaku");
        }

        private void UpdateRCStarvationHediff()
        {
            HediffDef starvationDef = DefDatabase<HediffDef>.GetNamed("RCStarvation", false);
            if (starvationDef == null) return;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(starvationDef);

            if (Value < ThresholdHungerLight)
            {
                if (existing == null) existing = pawn.health.AddHediff(starvationDef);
                existing.Severity = 1.0f - (Value / ThresholdHungerLight);

                if (Value < ThresholdHungerCritical && !pawn.InMentalState && !pawn.Downed)
                {
                    if (Rand.Value < 0.05f)
                        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "TG_GhoulHungerReason".Translate(), true);
                }

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
            if (pawn.jobs?.curJob != null)
            {
                JobDef curJobDef = pawn.jobs.curJob.def;
                if (curJobDef == JobDefOf.LayDown || curJobDef == JobDefOf.Wait_MaintainPosture)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                }
            }

            DamageInfo dinfo = new DamageInfo(DamageDefOf.Stab, 1.2f, 0, -1, null, pawn.RaceProps.body.corePart);
            pawn.TakeDamage(dinfo);

            if (pawn.IsHashIntervalTick(1000))
                Messages.Message("TG_GhoulStarvingToDeath".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeHealthEvent);
        }

        private void UpdateStatusEffects(bool isKaguneActive)
        {
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
                // ИСПРАВЛЕНИЕ: Берем только ТЕ части, которые НЕ отсутствуют
                var eyes = pawn.health.hediffSet.GetNotMissingParts()
                              .Where(p => p.def == BodyPartDefOf.Eye);

                foreach (var part in eyes)
                {
                    pawn.health.AddHediff(kakuganDef, part);
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
            if (Value <= 0) Value = 0.5f;

            // Запускаем инициализацию цвета и наследования
            GeneticsHelper.InitializeGhoulishTraits(pawn);

            // Логика какухо при инициализации
            bool alreadyHasKakuho = pawn.health.hediffSet.hediffs.Any(h => h.def.defName.Contains("Kakuho"));
            if (pawn.genes == null) return;

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
                    if (!alreadyHasKakuho && kakuhoDef != null)
                    {
                        pawn.health.AddHediff(kakuhoDef, pawn.RaceProps.body.corePart);
                    }
                    Messages.Message("TG_KaguneTypeAwakened".Translate(pawn.LabelShort, geneDef.label), pawn, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
    }
}