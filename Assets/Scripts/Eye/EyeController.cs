using UnityEngine;

public class EyeController : MonoBehaviour
{
    public GameObject bossPrefab;

    bool activated = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;

        CreatureBrain creature = other.GetComponentInParent<CreatureBrain>();
        if (creature == null || !creature.isPlayerControlled) return;

        activated = true;

        SpawnBoss();
    }

    void SpawnBoss()
    {
        GameObject boss = Instantiate(bossPrefab, transform.position, Quaternion.identity);

        BossLink link = boss.AddComponent<BossLink>();
        link.eye = this;
    }

    public void OnBossKilled()
    {
        EyeFormationManager.Instance.OnOuterBossKilled(this);

        Destroy(gameObject);
    }
}