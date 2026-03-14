using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Skill/Radial Projectile")]
public class SkillRadialProjectile : SkillDefinition
{
    public GameObject projectilePrefab;
    public int projectileCount = 8;
    public float projectileSpeed = 7f;

    public override void Execute(CreatureBrain owner)
    {
        float angleStep = 360f / projectileCount;

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            );

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
}