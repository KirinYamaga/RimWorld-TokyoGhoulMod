using RimWorld;
using Verse;
using System.Collections.Generic;

namespace TokyoGhoulMod
{
    public class Ability_ReleaseKagune : Ability
    {
        public Ability_ReleaseKagune() : base() { }
        public Ability_ReleaseKagune(Pawn pawn) : base(pawn) { }
        public Ability_ReleaseKagune(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public Ability_ReleaseKagune(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Activate(target, dest);

            HediffDef rinkakuDef = DefDatabase<HediffDef>.GetNamed("KaguneRinkaku", false);
            HediffDef kakuganDef = DefDatabase<HediffDef>.GetNamed("Kakugan", false);

            if (rinkakuDef == null) return false;

            Hediff rinkakuHediff = pawn.health.hediffSet.GetFirstHediffOfDef(rinkakuDef);

            if (rinkakuHediff != null)
            {
                pawn.health.RemoveHediff(rinkakuHediff);
                if (kakuganDef != null)
                {
                    Hediff kakuganHediff = pawn.health.hediffSet.GetFirstHediffOfDef(kakuganDef);
                    if (kakuganHediff != null) pawn.health.RemoveHediff(kakuganHediff);
                }
                Messages.Message("Кагуне втянуто.", pawn, MessageTypeDefOf.SilentInput);
            }
            else
            {
                pawn.health.AddHediff(rinkakuDef);
                if (kakuganDef != null) pawn.health.AddHediff(kakuganDef);

                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
                }
                Messages.Message(pawn.LabelShort + ": Ринкаку высвобожден!", pawn, MessageTypeDefOf.PositiveEvent);
            }

            // --- ИСПРАВЛЕННЫЙ БЛОК ОБНОВЛЕНИЯ ---

            // 1. Сброс кэша ударов. В 1.5/1.6 доступ идет через поле meleeVerbs (с маленькой буквы)
            // Это заставит ChooseMeleeVerb сработать заново в следующем бою.
            if (pawn.meleeVerbs != null)
            {
                // Мы вызываем обнуление текущего верба, чтобы пешка перевыбрала атаку
                pawn.meleeVerbs.Notify_PawnDespawned();
            }

            // 2. Обновление визуальной части
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            return true;
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport baseReport = base.CanCast;
                if (!baseReport.Accepted) return baseReport;

                if (!pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Kakuho", false)))
                {
                    return new AcceptanceReport("Отсутствует какухо");
                }
                return true;
            }
        }
    }
}