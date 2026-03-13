using UnityEngine;

public class Projectile : MonoBehaviour
{
    Vector2 dir;
    float speed;
    float damage;
    CreatureBrain owner;

    public void Init(Vector2 d, float s, float dmg, CreatureBrain o)
    {
        dir = d;
        speed = s;
        damage = dmg;
        owner = o;

        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        CreatureBrain target = col.attachedRigidbody?.GetComponent<CreatureBrain>();

        if (target == null) return;
        if (target == owner) return;

        target.TakeDamage(damage, owner);

        Destroy(gameObject);
    }
}