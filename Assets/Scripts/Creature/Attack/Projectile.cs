using UnityEngine;

public class Projectile : MonoBehaviour
{
    Vector2 dir;
    float speed;
    float damage;
    CreatureBrain owner;

    Vector3 targetPos;
    bool useImpactPoint = false;

    public float lifetime = 3f;
    public GameObject hitEffectPrefab;

    float timer;

    public void Init(Vector2 d, float s, float dmg, CreatureBrain o)
    {
        dir = d;
        speed = s;
        damage = dmg;
        owner = o;

        // 👉 Xoay theo hướng bay
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        timer = lifetime;
    }

    public void InitFall(
        Vector3 impactPoint,
        float s,
        float dmg,
        CreatureBrain o
    )
    {
        targetPos = impactPoint;
        speed = s;
        damage = dmg;
        owner = o;

        dir = (impactPoint - transform.position).normalized;

        // 👉 Xoay luôn ở đây
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        useImpactPoint = true;
        timer = lifetime;
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // projectile thường
        if (!useImpactPoint)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }

        // projectile rơi
        else
        {
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                Impact();
            }
        }
    }

    void Impact()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            0.6f
        );

        foreach (var hit in hits)
        {
            CreatureBrain target = hit.GetComponentInParent<CreatureBrain>();

            if (target != null && target != owner)
            {
                target.TakeDamage(damage, owner);
            }
        }

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(
                hitEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            Destroy(fx, 1.5f);
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (useImpactPoint) return;

        CreatureBrain target = col.attachedRigidbody?.GetComponent<CreatureBrain>();

        if (target == null) return;
        if (target == owner) return;

        target.TakeDamage(damage, owner);

        GameObject fx = Instantiate(
                hitEffectPrefab,
                transform.position,
                Quaternion.identity
            );

        Destroy(gameObject);
    }
}