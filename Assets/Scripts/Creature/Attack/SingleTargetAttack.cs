using UnityEngine;

[CreateAssetMenu(menuName="Combat/SingleTargetAttack")]
public class SingleTargetAttack : AttackDefinition
{
    public override void Execute(CreatureBrain owner)
    {
        CreatureBrain target = owner.CurrentTarget;

        // ===== AUTO RETARGET =====
        if (target == null || target.IsDead() || target.isHidden)
        {
            target = owner.FindBestTargetInRange(owner.Combat.AttackRange);
        }

        if (target == null) return;

        var myCol = owner.GetComponentInChildren<Collider2D>();
        var targetCol = target.GetComponentInChildren<Collider2D>();

        float dist = Vector2.Distance(
            myCol.ClosestPoint(target.transform.position),
            targetCol.ClosestPoint(owner.transform.position)
        );

        if (dist <= range)
        {
            // ✅ 1. Spawn attack VFX (đâm / cào)
            SpawnAttackVFX(owner);

            // ✅ 2. Damage
            target.TakeDamage(owner.stats.attackDamage, owner);

            // ✅ 3. Hit VFX (impact)
            SpawnHitVFX(target.transform.position);
        }
    }
}