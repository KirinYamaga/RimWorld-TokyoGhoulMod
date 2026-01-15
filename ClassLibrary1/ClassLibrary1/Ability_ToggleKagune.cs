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

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Activate(target, dest);

            // Определяем тип (например, Ukaku) из названия способности
            string typeTag = this.def.defName.Replace("Release_", "");
            HediffDef kaguneHediffDef = DefDatabase<HediffDef>.GetNamed("Hediff_" + typeTag, false);

            if (kaguneHediffDef == null) return false;

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(kaguneHediffDef);

            if (existing != null)
            {
                // Если кагуне этого типа уже активно — убираем его
                pawn.health.RemoveHediff(existing);
                Messages.Message("TokyoGhoul_KaguneRetracted".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.SilentInput);
            }
            else
            {
                // ХИМЕРА: Мы БОЛЬШЕ НЕ вызываем RemoveExistingKagunes.
                // Просто добавляем новое кагуне к уже имеющимся.
                pawn.health.AddHediff(kaguneHediffDef);

                if (pawn.Map != null)
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);

                Messages.Message("TG_KaguneReleased".Translate(pawn.LabelShort, kaguneHediffDef.label), MessageTypeDefOf.PositiveEvent);
            }

            if (pawn.meleeVerbs != null) pawn.meleeVerbs.Notify_PawnDespawned();
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            return true;
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted) return report;

                // Уточняем: нужно иметь какухо именно того типа, который мы пытаемся выпустить
                string typeTag = this.def.defName.Replace("Release_", "");
                HediffDef requiredKakuho = DefDatabase<HediffDef>.GetNamed("Hediff_Kakuho" + typeTag, false);

                if (requiredKakuho == null || !pawn.health.hediffSet.HasHediff(requiredKakuho))
                {
                    return new AcceptanceReport("TG_MissingSpecificKakuho".Translate(typeTag));
                }

                return true;
            }
        }
    }
}