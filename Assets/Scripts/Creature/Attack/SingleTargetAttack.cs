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
            target = owner.FindBestTarget();
        }

        if (target == null) return;

        float dist = (target.transform.position - owner.transform.position).sqrMagnitude;

        if (dist <= range * range)
        {
            target.TakeDamage(owner.stats.attackDamage, owner);
            owner.SpawnHitEffectAt(target.transform.position);
        }
    }
}