using UnityEngine;

[CreateAssetMenu(menuName="Combat/ArcAttack")]
public class ArcAttack : AttackDefinition
{
    public float arcAngle = 180f;

    static Collider2D[] buffer = new Collider2D[32];

    public override void Execute(CreatureBrain owner)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            owner.transform.position,
            range,
            buffer
        );

        Vector2 forward = owner.GetFacingDirection();

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
                owner.SpawnHitEffectAt(other.transform.position);
            }
        }
    }
}