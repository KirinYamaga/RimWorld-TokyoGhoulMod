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

                // ИСПРАВЛЕНО: Проверка наличия хедиффа Укаку через ключ локализации
                if (!pawn.health.hediffSet.HasHediff(HediffDef.Named("Hediff_Ukaku")))
                    return new AcceptanceReport("TG_NeedUkaku".Translate());

                // ИСПРАВЛЕНО: Проверка запаса RC-клеток через ключ локализации
                Gene_RCCells gene = pawn.genes?.GetFirstGeneOfType<Gene_RCCells>();
                if (gene == null || gene.Value < 0.02f)
                    return new AcceptanceReport("TG_LowRCCells".Translate());

                return true;
            }
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
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

            Projectile projectile = (Projectile)GenSpawn.Spawn(projectileDef, pawn.Position, pawn.Map);
            Vector3 targetVec = target.CenterVector3;

            targetVec.x += Rand.Range(-0.3f, 0.3f);
            targetVec.z += Rand.Range(-0.3f, 0.3f);

            LocalTargetInfo finalTarget = new LocalTargetInfo(targetVec.ToIntVec3());

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