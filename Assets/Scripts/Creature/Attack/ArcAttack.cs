using UnityEngine;

[CreateAssetMenu(menuName="Combat/ArcAttack")]
public class ArcAttack : AttackDefinition
{
    public float arcAngle = 180f;

    static Collider2D[] buffer = new Collider2D[32];

    public override void Execute(CreatureBrain owner)
    {
        Vector2 forward = owner.GetFacingDirection();

        // ✅ 1. Spawn slash effect
        SpawnAttackVFX(owner);

        // ✅ 2. Damage
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            owner.transform.position,
            range,
            buffer
        );

        for (int i = 0; i < hitCount; i++)
        {
            CreatureBrain other =
                buffer[i].attachedRigidbody?.GetComponent<CreatureBrain>();

            if (other == null || other == owner || other.IsDead() || other.isHidden)
                continue;

            Vector2 dir =
                (other.transform.position - owner.transform.position).normalized;

            float angle = Vector2.Angle(forward, dir);

            if (angle <= arcAngle * 0.5f)
            {
                other.TakeDamage(owner.stats.attackDamage, owner);

                // ✅ spawn hit effect đúng chỗ
                SpawnHitVFX(other.transform.position);
            }
        }
    }
}