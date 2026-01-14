using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TokyoGhoulMod
{
    public static class GeneticsHelper
    {
        public static void InitializeGhoulishTraits(Pawn pawn)
        {
            if (pawn?.genes == null) return;

            Pawn father = pawn.GetFather();
            Pawn mother = pawn.GetMother();

            if (father != null || mother != null)
            {
                ApplyInheritance(pawn, father, mother);
            }
            else
            {
                AssignRandomTraits(pawn);
            }
        }

        private static void ApplyInheritance(Pawn child, Pawn father, Pawn mother)
        {
            // --- 1. НАСЛЕДОВАНИЕ И СМЕШИВАНИЕ ЦВЕТА ---
            if (!HasColorGene(child))
            {
                GeneDef fatherColor = GetActiveKaguneColorGene(father);
                GeneDef motherColor = GetActiveKaguneColorGene(mother);

                if (fatherColor != null && motherColor != null && fatherColor != motherColor)
                {
                    float rand = Rand.Value;
                    if (rand < 0.40f) // 40% шанс на смешивание
                    {
                        Color blended = (GetColorFromGene(fatherColor) + GetColorFromGene(motherColor)) / 2f;
                        AddGeneAsInheritable(child, FindClosestColorGene(blended));
                    }
                    else if (rand < 0.70f) // 30% цвет отца
                    {
                        AddGeneAsInheritable(child, fatherColor);
                    }
                    else // 30% цвет матери
                    {
                        AddGeneAsInheritable(child, motherColor);
                    }
                }
                else if (fatherColor != null || motherColor != null)
                {
                    AddGeneAsInheritable(child, fatherColor ?? motherColor);
                }
            }

            // --- 2. НАСЛЕДОВАНИЕ ТИПА (ХИМЕРЫ) ---
            GeneDef fatherType = GetActiveKaguneTypeGene(father);
            GeneDef motherType = GetActiveKaguneTypeGene(mother);

            if (fatherType != null && motherType != null)
            {
                if (fatherType != motherType)
                {
                    if (Rand.Value < 0.15f) // Шанс на Химеру
                    {
                        AddGeneAsInheritable(child, fatherType);
                        AddGeneAsInheritable(child, motherType);
                        Messages.Message("TG_ChimeraBorn".Translate(child.LabelShort), child, MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        AddGeneAsInheritable(child, Rand.Value < 0.5f ? fatherType : motherType);
                    }
                }
                else
                {
                    AddGeneAsInheritable(child, fatherType);
                }
            }
        }

        private static void AssignRandomTraits(Pawn pawn)
        {
            if (!HasColorGene(pawn))
            {
                AddGeneAsInheritable(pawn, GetRandomColorGene());
            }
        }

        // ВАЖНО: Метод для добавления именно наследуемого (белого) гена
        private static void AddGeneAsInheritable(Pawn pawn, GeneDef geneDef)
        {
            if (geneDef == null) return;

            // Если у пешки уже есть этот ген, не добавляем дубликат
            if (pawn.genes.HasActiveGene(geneDef)) return;

            // xenogene: false делает ген ЭНДОГЕНОМ (белым и наследуемым)
            pawn.genes.AddGene(geneDef, xenogene: false);
        }

        private static bool HasColorGene(Pawn p) =>
            p.genes.GenesListForReading.Any(g => g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneColor"));

        private static GeneDef GetRandomColorGene() =>
            DefDatabase<GeneDef>.AllDefs.Where(g => g.exclusionTags != null && g.exclusionTags.Contains("KaguneColor")).RandomElement();

        private static GeneDef GetActiveKaguneColorGene(Pawn p) =>
            p?.genes?.GenesListForReading.FirstOrDefault(g => g.Active && g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneColor"))?.def;

        private static GeneDef GetActiveKaguneTypeGene(Pawn p) =>
            p?.genes?.GenesListForReading.FirstOrDefault(g => g.Active && g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneType"))?.def;

        private static Color GetColorFromGene(GeneDef gene)
        {
            var prop = gene.renderNodeProperties?.FirstOrDefault();
            return prop?.color ?? Color.red;
        }

        private static GeneDef FindClosestColorGene(Color target)
        {
            GeneDef closest = null;
            float minDistance = float.MaxValue;
            var colorGenes = DefDatabase<GeneDef>.AllDefs.Where(g => g.exclusionTags != null && g.exclusionTags.Contains("KaguneColor"));

            foreach (var g in colorGenes)
            {
                Color c = GetColorFromGene(g);
                float distance = Mathf.Sqrt(Mathf.Pow(c.r - target.r, 2) + Mathf.Pow(c.g - target.g, 2) + Mathf.Pow(c.b - target.b, 2));
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = g;
                }
            }
            return closest ?? colorGenes.First();
        }
    }
}