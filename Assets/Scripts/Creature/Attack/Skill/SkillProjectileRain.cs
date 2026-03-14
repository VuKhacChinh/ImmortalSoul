using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Skill/Projectile Rain")]
public class SkillProjectileRain : SkillDefinition
{
    public GameObject projectilePrefab;

    public float radius = 6f;
    public float spawnHeight = 6f;
    public float projectileSpeed = 10f;

    public int fallbackProjectileCount = 6;

    public GameObject hitEffectPrefab;

    public override void Execute(CreatureBrain owner)
    {
        Vector3 center = owner.transform.root.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            center,
            radius
        );

        bool foundTarget = false;

        foreach (var hit in hits)
        {
            CreatureBrain target = hit.GetComponentInParent<CreatureBrain>();

            if (target == null || target == owner) continue;

            foundTarget = true;

            SpawnProjectile(owner, target.transform.position);
        }

        // không có target → random điểm
        if (!foundTarget)
        {
            for (int i = 0; i < fallbackProjectileCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;

                Vector3 targetPos = center + (Vector3)offset;

                SpawnProjectile(owner, targetPos);
            }
        }
    }

    void SpawnProjectile(CreatureBrain owner, Vector3 targetPos)
    {
        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;

        GameObject proj = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.identity
        );

        proj.GetComponent<Projectile>().InitFall(
            targetPos,
            projectileSpeed,
            owner.stats.attackDamage,
            owner
        );
    }
}