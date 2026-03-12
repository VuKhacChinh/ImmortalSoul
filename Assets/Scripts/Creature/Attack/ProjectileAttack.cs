using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Projectile Attack")]
public class ProjectileAttack : AttackDefinition
{
    public GameObject projectilePrefab;
    public float projectileSpeed = 8f;

    public override void Execute(CreatureBrain owner)
    {
        var target = owner.CurrentTarget;
        if (target == null) return;

        Vector2 dir = (target.transform.position - owner.transform.position).normalized;

        GameObject proj = Instantiate(
            projectilePrefab,
            owner.transform.position,
            Quaternion.identity
        );

        proj.GetComponent<Projectile>().Init(
            dir,
            projectileSpeed,
            owner.stats.attackDamage,
            owner
        );
    }
}