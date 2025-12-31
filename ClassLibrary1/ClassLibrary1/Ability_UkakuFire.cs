using RimWorld;
using Verse;
using UnityEngine;

namespace TokyoGhoulMod
{
    public class Ability_UkakuFire : Ability
    {
        // Конструкторы для 1.6
        public Ability_UkakuFire() : base() { }
        public Ability_UkakuFire(Pawn pawn) : base(pawn) { }
        public Ability_UkakuFire(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public Ability_UkakuFire(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override AcceptanceReport CanCast
        {
            get
            {
                AcceptanceReport report = base.CanCast;
                if (!report.Accepted) return report;

                // Проверяем наличие хедиффа Укаку
                if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_Ukaku")))
                    return new AcceptanceReport("Нужно активировать Укаку");

                // Проверяем запас RC-клеток
                Gene_RCCells gene = pawn.genes?.GetFirstGeneOfType<Gene_RCCells>();
                if (gene == null || gene.Value < 0.02f)
                    return new AcceptanceReport("Недостаточно RC-клеток");

                return true;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Списываем RC-клетки за каждый выстрел
            Gene_RCCells gene = pawn.genes?.GetFirstGeneOfType<Gene_RCCells>();
            if (gene != null)
            {
                gene.Value -= 0.02f;
            }

            ShootProjectile(target);
            return base.Activate(target, dest);
        }

        private void ShootProjectile(LocalTargetInfo target)
        {
            if (pawn.Map == null || !target.IsValid) return;

            ThingDef projectileDef = this.def.verbProperties.defaultProjectile;
            if (projectileDef == null) return;

            // Спавним снаряд
            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);

            // РАБОТА С ВЕКТОРАМИ И ЦЕЛЬЮ
            Vector3 targetVec = target.CenterVector3;

            // Добавляем небольшой рандомный разброс
            targetVec.x += Rand.Range(-0.3f, 0.3f);
            targetVec.z += Rand.Range(-0.3f, 0.3f);

            // ИСПРАВЛЕНИЕ ОШИБКИ CS1503: 
            // Создаем LocalTargetInfo из координат клетки (IntVec3), полученных из нашего Vector3
            LocalTargetInfo finalTarget = new LocalTargetInfo(targetVec.ToIntVec3());

            // Запуск снаряда
            // Аргументы: launcher, origin, usedTarget, intendedTarget, hitFlags, preventSpecialEffects, equipment
            projectile.Launch(
                pawn,
                pawn.DrawPos,
                finalTarget,
                target,
                ProjectileHitFlags.All,
                false,
                null
            );
        }
    }
}