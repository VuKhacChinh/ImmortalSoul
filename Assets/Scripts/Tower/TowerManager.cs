using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance;

    public CreatureBrain topTowerPrefab;
    public CreatureBrain bottomTowerPrefab;
    public CreatureBrain leftTowerPrefab;
    public CreatureBrain rightTowerPrefab;

    // cái này tạm vẫn giữ (cổng sau này)
    public GameObject centerGatePrefab;

    CreatureBrain topTower;
    CreatureBrain bottomTower;
    CreatureBrain leftTower;
    CreatureBrain rightTower;

    GameObject centerGate;

    int destroyedCount = 0;

    void Awake()
    {
        Instance = this;
    }

    public void SpawnTowers(Vector2 center, float offset)
    {
        topTower = Instantiate(topTowerPrefab, center + new Vector2(0, offset), Quaternion.identity);
        bottomTower = Instantiate(bottomTowerPrefab, center + new Vector2(0, -offset), Quaternion.identity);
        leftTower = Instantiate(leftTowerPrefab, center + new Vector2(-offset, 0), Quaternion.identity);
        rightTower = Instantiate(rightTowerPrefab, center + new Vector2(offset, 0), Quaternion.identity);

        centerGate = Instantiate(centerGatePrefab, center, Quaternion.identity);
        centerGate.SetActive(false);

        // 🔥 QUAN TRỌNG: gắn callback chết cho từng trụ
        BindTower(topTower);
        BindTower(bottomTower);
        BindTower(leftTower);
        BindTower(rightTower);
    }

    void BindTower(CreatureBrain tower)
    {
        tower.OnDeathCallback += OnTowerDestroyed;
    }

    void OnTowerDestroyed(CreatureBrain tower)
    {
        if (!tower.isTower) return;

        destroyedCount++;

        if (destroyedCount >= 4)
        {
            centerGate.SetActive(true);
        }
    }
}