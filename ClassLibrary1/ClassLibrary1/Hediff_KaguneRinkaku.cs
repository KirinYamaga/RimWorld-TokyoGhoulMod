using RimWorld;
using Verse;

namespace TokyoGhoulMod
{
    public class Hediff_KaguneRinkaku : HediffWithComps
    {
        private const int CheckInterval = 200; // Проверка примерно каждые 3 секунды игрового времени

        public override void Tick()
        {
            base.Tick();

            if (pawn.IsHashIntervalTick(CheckInterval))
            {
                // 1. Получаем доступ к гену RC-клеток
                Gene_RCCells gene = pawn.genes?.GetFirstGeneOfType<Gene_RCCells>();

                // 2. Проверка на наличие клеток и их потребление
                if (gene == null || gene.Value <= 0.005f) // Небольшой порог, чтобы не уходить в минус
                {
                    Messages.Message("TokyoGhoul_KaguneRetractedCells".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent);
                    pawn.health.RemoveHediff(this);
                    return;
                }

                // Потребляем немного RC-клеток за поддержку формы (например, 0.5% каждые 200 тиков)
                gene.Value -= 0.005f;

                // 3. Проверка на критический голод
                if (pawn.needs.food != null)
                {
                    // Если еда на нуле
                    if (pawn.needs.food.CurLevel <= 0f)
                    {
                        // Кагуне втягивается, так как нет энергии
                        Messages.Message("TokyoGhoul_KaguneRetractedStarvation".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent);
                        pawn.health.RemoveHediff(this);

                        // Шанс 25% сойти с ума от голода (Berserk), если кагуне "доело" последние силы
                        if (Rand.Value < 0.25f && !pawn.InMentalState)
                        {
                            pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Голод гуля стал невыносимым");
                        }
                    }
                }
            }
        }
    }
}