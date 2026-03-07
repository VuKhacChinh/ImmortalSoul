using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Reward")]
    public int xpReward = 10;
    public float healAmount = 20f;

    [Header("Optional")]
    public float lifeTime = 20f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        CreatureBrain creature = collision.collider.GetComponentInParent<CreatureBrain>();

        if (creature == null) return;
        if (creature.IsDead()) return;

        creature.EatItem(xpReward, healAmount);

        Destroy(gameObject);
    }
}