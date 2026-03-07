using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [System.Serializable]
    public class SpawnZone
    {
        [Tooltip("Bán kính ngoài của khu vực")]
        public float outerRadius;

        [Tooltip("Tổng số creature spawn trong khu vực")]
        public int spawnCount;

        [Tooltip("Danh sách creature có thể spawn")]
        public List<CreatureBrain> creaturePrefabs = new();
    }

    [Header("Spawn Zones")]
    public List<SpawnZone> zones = new();

    [Header("Respawn")]
    public float respawnCheckInterval = 5f;

    [Header("Player Highlight")]
    public GameObject controlRingPrefab;

    GameObject controlRingInstance;

    private List<CreatureBrain> allCreatures = new();

    private Dictionary<CreatureBrain, int> creatureZoneMap = new();
    private Dictionary<int, List<CreatureBrain>> zoneCreatures = new();

    private CreatureBrain playerCreature;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnCreatures();
        SelectRandomPlayer();

        InvokeRepeating(nameof(CheckRespawn), respawnCheckInterval, respawnCheckInterval);
    }

    // =========================================================
    // SPAWN
    // =========================================================

    void SpawnCreatures()
    {
        float innerRadius = 0f;

        for (int z = 0; z < zones.Count; z++)
        {
            var zone = zones[z];

            if (!zoneCreatures.ContainsKey(z))
                zoneCreatures[z] = new List<CreatureBrain>();

            for (int i = 0; i < zone.spawnCount; i++)
            {
                SpawnCreatureInZone(z, innerRadius, zone.outerRadius);
            }

            innerRadius = zone.outerRadius;
        }
    }

    void SpawnCreatureInZone(int zoneIndex, float innerRadius, float outerRadius)
    {
        var zone = zones[zoneIndex];

        Vector2 pos = RandomPointInRing(innerRadius, outerRadius);

        CreatureBrain prefab =
            zone.creaturePrefabs[
                Random.Range(0, zone.creaturePrefabs.Count)
            ];

        CreatureBrain creature =
            Instantiate(prefab, pos, Quaternion.identity);

        creature.isPlayerControlled = false;

        allCreatures.Add(creature);
        creatureZoneMap[creature] = zoneIndex;
        zoneCreatures[zoneIndex].Add(creature);
    }

    Vector2 RandomPointInRing(float inner, float outer)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);

        float innerSq = inner * inner;
        float outerSq = outer * outer;

        float radius = Mathf.Sqrt(Random.Range(innerSq, outerSq));

        float x = Mathf.Cos(angle) * radius;
        float y = Mathf.Sin(angle) * radius;

        return new Vector2(x, y);
    }

    // =========================================================
    // RESPAWN SYSTEM
    // =========================================================

    void CheckRespawn()
    {
        float innerRadius = 0f;

        for (int z = 0; z < zones.Count; z++)
        {
            var zone = zones[z];

            zoneCreatures[z].RemoveAll(c => c == null);

            int missing = zone.spawnCount - zoneCreatures[z].Count;

            for (int i = 0; i < missing; i++)
            {
                SpawnCreatureInZone(z, innerRadius, zone.outerRadius);
            }

            innerRadius = zone.outerRadius;
        }
    }

    // =========================================================
    // PLAYER
    // =========================================================

    void SelectRandomPlayer()
    {
        if (allCreatures.Count == 0) return;

        int index = Random.Range(0, allCreatures.Count);

        playerCreature = allCreatures[index];
        AttachControlRing(playerCreature);

        foreach (var c in allCreatures)
            if (c != null)
                c.isPlayerControlled = false;

        playerCreature.isPlayerControlled = true;

        UpdateCameraAndUI();
    }

    void AttachControlRing(CreatureBrain creature)
    {
        if (controlRingInstance == null)
        {
            controlRingInstance = Instantiate(controlRingPrefab);
        }

        controlRingInstance.transform.SetParent(creature.transform);
        controlRingInstance.transform.localPosition = new Vector3(0, 0, 0);
    }

    void ChooseNewPlayer()
    {
        List<CreatureBrain> aliveCreatures = new();

        foreach (var c in allCreatures)
        {
            if (c != null && c.currentHP > 0)
                aliveCreatures.Add(c);
        }

        if (aliveCreatures.Count == 0)
        {
            Debug.Log("GAME OVER");
            return;
        }

        int index = Random.Range(0, aliveCreatures.Count);

        playerCreature = aliveCreatures[index];
        AttachControlRing(playerCreature);

        foreach (var c in allCreatures)
            if (c != null)
                c.isPlayerControlled = false;

        playerCreature.isPlayerControlled = true;

        UpdateCameraAndUI();
    }

    void UpdateCameraAndUI()
    {
        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();

        if (cam != null)
            cam.SetTarget(playerCreature.transform);

        if (UIController.Instance != null)
            UIController.Instance.SetPlayer(playerCreature);
    }

    // =========================================================
    // EVENT
    // =========================================================

    public void OnCreatureDeath(CreatureBrain creature)
    {
        if (creatureZoneMap.ContainsKey(creature))
        {
            int zone = creatureZoneMap[creature];

            zoneCreatures[zone].Remove(creature);
            creatureZoneMap.Remove(creature);
        }

        allCreatures.Remove(creature);

        if (creature == playerCreature)
        {
            Invoke(nameof(ChooseNewPlayer), 0.3f);
        }
    }

    public CreatureBrain GetPlayer()
    {
        return playerCreature;
    }
}