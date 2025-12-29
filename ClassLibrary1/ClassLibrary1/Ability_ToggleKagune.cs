using RimWorld;
using Verse;
using System.Linq;

namespace TokyoGhoulMod
{
    public class Ability_ToggleKagune : Ability
    {
        // --- ОБЯЗАТЕЛЬНЫЕ КОНСТРУКТОРЫ ДЛЯ 1.6 ---
        public Ability_ToggleKagune() : base() { }
        public Ability_ToggleKagune(Pawn pawn) : base(pawn) { }
        public Ability_ToggleKagune(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public Ability_ToggleKagune(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Activate(target, dest);

            // 1. Определяем имя хедиффа. 
            // Если способность "Release_Ukaku", то ищем хедифф "Hediff_Ukaku"
            string hediffDefName = this.def.defName.Replace("Release_", "Hediff_");

            HediffDef kaguneHediffDef = DefDatabase<HediffDef>.GetNamed(hediffDefName, false);
            HediffDef kakuganDef = DefDatabase<HediffDef>.GetNamed("Kakugan", false);

            if (kaguneHediffDef == null)
            {
                Log.Error($"[TokyoGhoul] Не удалось найти HediffDef с именем {hediffDefName}. Проверьте XML.");
                return false;
            }

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(kaguneHediffDef);

            if (existing != null)
            {
                // --- ВТЯГИВАНИЕ ---
                pawn.health.RemoveHediff(existing);

                // Убираем Какуган только если не осталось других активных кагуне
                if (kakuganDef != null && !IsAnyKaguneActive())
                {
                    Hediff kakuganHediff = pawn.health.hediffSet.GetFirstHediffOfDef(kakuganDef);
                    if (kakuganHediff != null) pawn.health.RemoveHediff(kakuganHediff);
                }

                Messages.Message("TokyoGhoul_KaguneRetracted".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.SilentInput);
            }
            else
            {
                // --- ВЫСВОБОЖДЕНИЕ ---
                // Сначала удаляем другие типы кагуне, если они были активны
                RemoveExistingKagunes(pawn);

                pawn.health.AddHediff(kaguneHediffDef);

                // Включаем Какуган (глаза)
                if (kakuganDef != null && !pawn.health.hediffSet.HasHediff(kakuganDef))
                {
                    pawn.health.AddHediff(kakuganDef);
                }

                if (pawn.Map != null)
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.PsycastAreaEffect);
                }
                Messages.Message(pawn.LabelShort + ": " + kaguneHediffDef.label + " высвобожден!", MessageTypeDefOf.PositiveEvent);
            }

            // --- ОБНОВЛЕНИЕ БОЕВОЙ СИСТЕМЫ ---
            // Заставляем игру пересчитать доступные удары ближнего боя
            if (pawn.meleeVerbs != null)
            {
                pawn.meleeVerbs.Notify_PawnDespawned();
            }

            // Обновляем визуальную часть (RenderNode)
            pawn.Drawer.renderer.SetAllGraphicsDirty();

            return true;
        }

        // Проверка: активен ли хоть какой-то тип кагуне прямо сейчас
        private bool IsAnyKaguneActive()
        {
            string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
            return pawn.health.hediffSet.hediffs.Any(h => tags.Any(tag => h.def.defName == "Hediff_" + tag));
        }

        // Удаление всех активных форм кагуне
        private void RemoveExistingKagunes(Pawn p)
        {
            string[] tags = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
            var toRemove = p.health.hediffSet.hediffs
                .Where(h => tags.Any(tag => h.def.defName == "Hediff_" + tag))
                .ToList();

            foreach (var h in toRemove) p.health.RemoveHediff(h);
        }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted) return report;

                // Пешка не может использовать способность без органа Какухо
                if (!pawn.health.hediffSet.HasHediff(DefDatabase<HediffDef>.GetNamed("Kakuho", false)))
                {
                    return new AcceptanceReport("Отсутствует какухо");
                }

                return true;
            }
        }
    }
}