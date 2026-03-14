using UnityEngine;

public class AOEBase : MonoBehaviour
{
    protected CreatureBrain owner;
    protected float radius;

    public GameObject hitEffectPrefab;

    protected void InitBase(CreatureBrain owner, float radius)
    {
        this.owner = owner;
        this.radius = radius;
    }

    protected void DamageTarget(CreatureBrain target)
    {
        target.TakeDamage(owner.stats.attackDamage, owner);

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(
                hitEffectPrefab,
                target.transform.position,
                Quaternion.identity
            );

            Destroy(fx, 1.5f);
        }
    }

    protected void DoAOEDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius
        );

        foreach (var hit in hits)
        {
            CreatureBrain target = hit.GetComponentInParent<CreatureBrain>();

            if (target != null && target != owner)
            {
                DamageTarget(target);
            }
        }
    }
}