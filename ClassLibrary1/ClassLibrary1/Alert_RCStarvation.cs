using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TokyoGhoulMod
{
    public class Alert_RCStarvation : Alert
    {
        public Alert_RCStarvation()
        {
            this.defaultLabel = "TG_AlertRCStarvationLabel".Translate();
            this.defaultExplanation = "TG_AlertRCStarvationDesc".Translate();
            this.defaultPriority = AlertPriority.Critical;
        }

        public override AlertReport GetReport()
        {
            return AlertReport.CulpritsAre(GhoulsInDanger);
        }

        private List<Pawn> GhoulsInDanger
        {
            get
            {
                List<Pawn> culprits = new List<Pawn>();

                // Используем AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists из вашего PawnsFinder
                var candidates = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists;

                foreach (Pawn p in candidates)
                {
                    if (p.genes == null) continue;
                    Gene_RCCells gene = p.genes.GetFirstGeneOfType<Gene_RCCells>();

                    if (gene != null && gene.Value < 0.05f)
                    {
                        culprits.Add(p);
                    }
                }
                return culprits;
            }
        }
    }
}