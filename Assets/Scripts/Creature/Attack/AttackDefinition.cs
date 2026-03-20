using UnityEngine;

public abstract class AttackDefinition : ScriptableObject
{
    [Header("VFX")]
    public GameObject vfxPrefab;   // slash / claw / stab
    public GameObject hitVFX;      // impact khi trúng

    public float range = 1.5f;
    public float cooldown = 1f;

    public abstract void Execute(CreatureBrain owner);

    protected void SpawnAttackVFX(CreatureBrain owner)
    {
        if (vfxPrefab == null) return;

        Vector2 dir = owner.GetFacingDirection();

        Vector3 pos = owner.transform.position + (Vector3)(dir * 0.4f);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Instantiate(vfxPrefab, pos, Quaternion.Euler(0, 0, angle));
    }

    protected void SpawnHitVFX(Vector3 pos)
    {
        if (hitVFX == null) return;

        Instantiate(hitVFX, pos, Quaternion.identity);
    }
}