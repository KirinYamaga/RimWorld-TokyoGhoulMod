using RimWorld;
using Verse;
using UnityEngine;

namespace TokyoGhoulMod
{
    public class Ability_UkakuFire : Ability
    {
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

                if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_Ukaku")))
                {
                    return new AcceptanceReport("Нужно активировать Укаку");
                }

                Gene_RCCells gene = pawn.genes?.GetFirstGeneOfType<Gene_RCCells>();
                if (gene == null || gene.Value < 0.02f)
                {
                    return new AcceptanceReport("Недостаточно RC-клеток");
                }

                return true;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Списываем 2% за каждый снаряд из очереди
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
            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);

            // Уменьшаем разброс до минимума (0.05 вместо 0.5)
            // Или вообще удалите эти 3 строки для 100% точности "в точку"
            Vector3 targetVec = target.CenterVector3;
            targetVec.x += Rand.Range(-0.005f, 0.005f);
            targetVec.z += Rand.Range(-0.005f, 0.005f);

            LocalTargetInfo spreadTarget = new LocalTargetInfo(targetVec.ToIntVec3());

            projectile.Launch(
                pawn,
                pawn.DrawPos,
                spreadTarget,
                target,
                ProjectileHitFlags.All,
                false,
                null
            );
        }
    }
}