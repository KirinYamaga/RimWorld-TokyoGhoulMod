using RimWorld;
using Verse;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace TokyoGhoulMod
{
    public static class GeneticsHelper
    {
        private const float ChimeraChance = 0.10f; // Шанс химеры (Гуль + Гуль)
        private const float HalfGhoulChance = 0.10f; // Шанс Полугуля (Гуль + Человек)
        private const float HalfHumanChance = 0.50f; // Шанс Получеловека (Гуль + Человек)
        // Остальные 40% — мертворожденный

        public static void InitializeGhoulishTraits(Pawn pawn)
        {
            if (pawn?.genes == null) return;

            Pawn father = pawn.GetFather();
            Pawn mother = pawn.GetMother();

            bool fatherIsGhoul = IsGhoul(father);
            bool motherIsGhoul = IsGhoul(mother);

            // 1. СЛУЧАЙ: Оба родителя гули
            if (fatherIsGhoul && motherIsGhoul)
            {
                ApplyInheritance(pawn, father, mother);
            }
            // 2. СЛУЧАЙ: Союз Гуля и Человека (Гибриды)
            else if (fatherIsGhoul || motherIsGhoul)
            {
                ApplyHybridLogic(pawn, fatherIsGhoul ? father : mother);
            }
            // 3. СЛУЧАЙ: Нет родителей (спавн в мире)
            else if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Gene_RCCells", false)))
            {
                AssignRandomTraits(pawn);
            }

            // Финальный фикс названия ксенотипа
            UpdateXenotypeLabel(pawn);
        }

        private static void ApplyHybridLogic(Pawn child, Pawn ghoulParent)
        {
            float roll = Rand.Value;

            // 1. ПОЛУГУЛЬ (10%)
            if (roll < 0.10f)
            {
                // Устанавливаем ксенотип
                child.genes.SetXenotype(DefDatabase<XenotypeDef>.GetNamed("TG_HalfGhoul"));

                // Добавляем тип кагуне от родителя (так как в ксенотипе его нет по умолчанию)
                GeneDef parentType = GetActiveKaguneTypeGene(ghoulParent);
                if (parentType != null) AddGeneAsInheritable(child, parentType);
            }
            // 2. ПОЛУЧЕЛОВЕК (50%)
            else if (roll < 0.60f)
            {
                child.genes.SetXenotype(DefDatabase<XenotypeDef>.GetNamed("TG_HalfHuman"));
            }
            // 3. СМЕРТЬ (40%)
            else
            {
                child.Kill(null);
                Messages.Message("TG_HybridStillborn".Translate(child.LabelShort), MessageTypeDefOf.NegativeEvent);
            }
        }

        private static void ApplyInheritance(Pawn child, Pawn father, Pawn mother)
        {
            // --- НАСЛЕДОВАНИЕ ЦВЕТА ---
            if (!HasColorGene(child))
            {
                GeneDef fColor = GetActiveKaguneColorGene(father);
                GeneDef mColor = GetActiveKaguneColorGene(mother);
                if (fColor != null || mColor != null)
                {
                    GeneDef chosen = (Rand.Value < 0.5f) ? (fColor ?? mColor) : (mColor ?? fColor);
                    AddGeneAsInheritable(child, chosen);
                }
            }

            // --- НАСЛЕДОВАНИЕ ТИПА (С ШАНСОМ ХИМЕРЫ) ---
            if (!HasTypeGene(child))
            {
                GeneDef fType = GetActiveKaguneTypeGene(father);
                GeneDef mType = GetActiveKaguneTypeGene(mother);

                if (fType != null && mType != null)
                {
                    if (fType != mType)
                    {
                        if (Rand.Value < ChimeraChance)
                        {
                            AddGeneAsInheritable(child, fType);
                            AddGeneAsInheritable(child, mType);
                            Messages.Message("TG_ChimeraBorn".Translate(child.LabelShort), child, MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            AddGeneAsInheritable(child, Rand.Value < 0.5f ? fType : mType);
                        }
                    }
                    else AddGeneAsInheritable(child, fType);
                }
            }
        }

        private static void AssignRandomTraits(Pawn pawn)
        {
            if (!HasColorGene(pawn)) AddGeneAsInheritable(pawn, GetRandomColorGene());
            if (!HasTypeGene(pawn))
            {
                string[] types = { "Ukaku", "Koukaku", "Rinkaku", "Bikaku" };
                GeneDef randomType = DefDatabase<GeneDef>.GetNamed("Gene_" + types.RandomElement(), false);
                if (randomType != null) AddGeneAsInheritable(pawn, randomType);
            }
        }

        private static void UpdateXenotypeLabel(Pawn pawn)
        {
            if (pawn.genes.xenotypeName != null) return;

            if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("TG_HalfGhoul", false)))
                pawn.genes.xenotypeName = "TG_HalfGhoulLabel".Translate();
            else if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("TG_HalfHuman", false)))
                pawn.genes.xenotypeName = "TG_HalfHumanLabel".Translate();
            else if (pawn.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Gene_RCCells", false)))
            {
                int count = CountKaguneGenes(pawn);
                pawn.genes.xenotypeName = (count >= 2) ? "TG_ChimeraXenotypeLabel".Translate() : "TG_Ghoul".Translate();
            }
        }

        private static void AddGeneAsInheritable(Pawn pawn, GeneDef geneDef)
        {
            if (geneDef == null || pawn.genes.HasActiveGene(geneDef)) return;
            pawn.genes.AddGene(geneDef, false);
        }

        private static bool IsGhoul(Pawn p) => p != null && p.genes != null && p.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Gene_RCCells", false));
        private static bool HasColorGene(Pawn p) => p.genes.GenesListForReading.Any(g => g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneColor"));
        private static bool HasTypeGene(Pawn p) => CountKaguneGenes(p) > 0;

        private static int CountKaguneGenes(Pawn p)
        {
            string[] types = { "Gene_Ukaku", "Gene_Koukaku", "Gene_Rinkaku", "Gene_Bikaku" };
            return p.genes.GenesListForReading.Count(g => types.Contains(g.def.defName));
        }

        private static GeneDef GetRandomColorGene() => DefDatabase<GeneDef>.AllDefs.Where(g => g.exclusionTags != null && g.exclusionTags.Contains("KaguneColor")).RandomElement();
        private static GeneDef GetActiveKaguneColorGene(Pawn p) => p?.genes?.GenesListForReading.FirstOrDefault(g => g.Active && g.def.exclusionTags != null && g.def.exclusionTags.Contains("KaguneColor"))?.def;
        private static GeneDef GetActiveKaguneTypeGene(Pawn p)
        {
            string[] types = { "Gene_Ukaku", "Gene_Koukaku", "Gene_Rinkaku", "Gene_Bikaku" };
            return p?.genes?.GenesListForReading.FirstOrDefault(g => g.Active && types.Contains(g.def.defName))?.def;
        }
    }
}