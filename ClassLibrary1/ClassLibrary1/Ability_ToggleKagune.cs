using RimWorld;
using Verse;
using System.Linq;

namespace TokyoGhoulMod
{
    public class Ability_ToggleKagune : Ability
    {
        public Ability_ToggleKagune() : base() { }
        public Ability_ToggleKagune(Pawn pawn) : base(pawn) { }
        public Ability_ToggleKagune(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public Ability_ToggleKagune(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Activate(target, dest);

            string hediffDefName = this.def.defName.Replace("Release_", "Hediff_");
            HediffDef kaguneHediffDef = DefDatabase<HediffDef>.GetNamed(hediffDefName, false);
            HediffDef kakuganDef = DefDatabase<HediffDef>.GetNamed("Kakugan", false);

            if (kaguneHediffDef == null) return false;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(kaguneHediffDef);

            if (existing != null)
            {
                pawn.health.RemoveHediff(existing);
                if (kakuganDef != null && !IsAnyKaguneActive())
                {
                    Hediff kakuganHediff = pawn.health.hediffSet.GetFirstHediffOfDef(kakuganDef);
                    if (kakuganHediff != null) pawn.health.RemoveHediff(kakuganHediff);
                }
                Messages.Message("TokyoGhoul_KaguneRetracted".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.SilentInput);
            }
            else
            {
                RemoveExistingKagunes(pawn);
                pawn.health.AddHediff(kaguneHediffDef);
                if (kakuganDef != null && !pawn.health.hediffSet.HasHediff(kakuganDef))
                {
                    pawn.health.AddHediff(kakuganDef);
                }

                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
                }
                // ИСПРАВЛЕНО: Использование ключа трансляции с аргументами
                Messages.Message("TG_KaguneReleased".Translate(pawn.LabelShort, kaguneHediffDef.label), MessageTypeDefOf.PositiveEvent);
            }

            if (pawn.meleeVerbs != null) pawn.meleeVerbs.Notify_PawnDespawned();
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            return true;
        }

        private bool IsAnyKaguneActive()
        {
            string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
            return pawn.health.hediffSet.hediffs.Any(h => h.def != null && tags.Any(tag => h.def.defName == "Hediff_" + tag));
        }

        private void RemoveExistingKagunes(Pawn p)
        {
            string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
            var toRemove = p.health.hediffSet.hediffs
                .Where(h => h.def != null && tags.Any(tag => h.def.defName == "Hediff_" + tag))
                .ToList();

            foreach (var h in toRemove) p.health.RemoveHediff(h);
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted) return report;

                bool hasAnyKakuho = pawn.health.hediffSet.hediffs.Any(h => h.def != null && h.def.defName.Contains("Kakuho"));
                // ИСПРАВЛЕНО: Замена текста на ключ
                if (!hasAnyKakuho) return new AcceptanceReport("TG_MissingKakuho".Translate());

                return true;
            }
        }
    }
}