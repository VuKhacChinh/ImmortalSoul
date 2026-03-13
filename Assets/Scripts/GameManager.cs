using System.Collections.Generic;
using System.Collections;
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
    public GameObject controlEffectPrefab;
    GameObject controlEffectInstance;

    [Header("Level Spawn Range")]
    public int levelRange = 3;

    [Header("Possession")]
    public GameObject possessEffectPrefab;
    public GameObject soulPrefab;
    public float soulSpeed = 20f;

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
        ChooseInitialPlayer();

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

        // =========================================
        // LEVEL RANDOM THEO PLAYER
        // =========================================

        int playerLevel = 1;

        if (playerCreature != null)
            playerLevel = playerCreature.level;

        int minLevel = Mathf.Max(1, playerLevel - levelRange);
        int maxSpawnLevel = Mathf.Min(LevelSystem.Instance.maxLevel, playerLevel + levelRange);

        int randomLevel = Mathf.RoundToInt(
            Mathf.Lerp(minLevel, maxSpawnLevel, Random.value * Random.value)
        );

        LevelSystem.Instance.SetLevel(creature, randomLevel);

        // =========================================

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

    void ChooseInitialPlayer()
    {
        CreatureBrain chosen = ChooseWeightedCreature(int.MaxValue);

        if (chosen == null)
        {
            Debug.Log("No creature available");
            return;
        }

        SetPlayer(chosen);
    }

    void AttachControlEffect(CreatureBrain creature)
    {
        if (controlEffectInstance == null)
            controlEffectInstance = Instantiate(controlEffectPrefab);

        HPBar bar = HPBarManager.Instance.GetHPBar(creature);

        if (bar == null)
            return;

        controlEffectInstance.transform.SetParent(bar.transform, false);

        // ===== FIX SCALE THEO HPBAR =====
        float scale = bar.transform.lossyScale.x;

        float fixScale = 1f / scale;

        controlEffectInstance.transform.localScale = Vector3.one * fixScale;

        // ===== VỊ TRÍ BÊN PHẢI HP =====
        controlEffectInstance.transform.localPosition = new Vector3(0f, 50f, 0f);
    }

    void SetPlayer(CreatureBrain creature)
    {
        if (playerCreature != null)
            playerCreature.isPlayerControlled = false;

        playerCreature = creature;
        playerCreature.isPlayerControlled = true;
        playerCreature.SetHidden(false);

        playerCreature.currentHP = playerCreature.stats.maxHP;

        StartCoroutine(AttachEffectNextFrame(playerCreature));

        PlayPossessEffect(playerCreature);

        UpdateCameraAndUI();
    }

    IEnumerator AttachEffectNextFrame(CreatureBrain creature)
    {
        yield return null; // chờ HPBar spawn

        AttachControlEffect(creature);
    }

    CreatureBrain ChooseWeightedCreature(int maxLevel)
    {
        List<CreatureBrain> candidates = new();

        foreach (var c in allCreatures)
        {
            if (c != null && c.currentHP > 0 && c.level <= maxLevel)
                candidates.Add(c);
        }

        if (candidates.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (var c in candidates)
            totalWeight += c.level;

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var c in candidates)
        {
            cumulative += c.level;

            if (roll < cumulative)
                return c;
        }

        return candidates[0];
    }

    void PlayPossessEffect(CreatureBrain creature)
    {
        if (possessEffectPrefab == null) return;

        GameObject fx = Instantiate(
            possessEffectPrefab,
            creature.transform.position,
            Quaternion.identity,
            creature.transform
        );

        Destroy(fx, 1f);
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
        if (creatureZoneMap.TryGetValue(creature, out int zone))
        {
            zoneCreatures[zone].Remove(creature);
            creatureZoneMap.Remove(creature);
        }

        allCreatures.Remove(creature);

        if (creature == playerCreature)
        {
            StartCoroutine(SoulPossessionSequence(creature.transform.position, creature.level));
        }
    }

    IEnumerator SoulPossessionSequence(Vector3 deathPos, int deadLevel)
    {
        GameObject soul = Instantiate(soulPrefab, deathPos, Quaternion.identity);

        CreatureBrain target = ChooseWeightedCreature(deadLevel);

        // =================================================
        // nếu không tìm được creature thì spawn 1 con mới
        // =================================================

        if (target == null)
        {
            target = SpawnFallbackCreature(deadLevel, deathPos);
        }

        CameraFollow cam = Camera.main.GetComponent<CameraFollow>();

        if (cam != null)
            cam.SetTarget(soul.transform);

        while (soul != null)
        {
            if (target == null || target.currentHP <= 0)
            {
                target = ChooseWeightedCreature(deadLevel);

                if (target == null)
                {
                    target = SpawnFallbackCreature(deadLevel, soul.transform.position);
                }
            }

            Vector3 dir = target.transform.position - soul.transform.position;

            soul.transform.position += dir.normalized * soulSpeed * Time.deltaTime;

            soul.transform.position += Vector3.up * Mathf.Sin(Time.time * 12f) * 0.1f;

            if (Vector3.Distance(soul.transform.position, target.transform.position) < 0.2f)
                break;

            yield return null;
        }

        Destroy(soul);

        if (target != null)
        {
            SetPlayer(target);

            if (cam != null)
                cam.SetTarget(playerCreature.transform);
        }
    }

    CreatureBrain SpawnFallbackCreature(int level, Vector3 nearPos)
    {
        if (zones.Count == 0)
            return null;

        int zoneIndex = Random.Range(0, zones.Count);
        var zone = zones[zoneIndex];

        if (zone.creaturePrefabs.Count == 0)
            return null;

        CreatureBrain prefab =
            zone.creaturePrefabs[Random.Range(0, zone.creaturePrefabs.Count)];

        Vector2 spawnPos = nearPos + (Vector3)Random.insideUnitCircle * 3f;

        CreatureBrain creature =
            Instantiate(prefab, spawnPos, Quaternion.identity);

        creature.isPlayerControlled = false;

        LevelSystem.Instance.SetLevel(creature, level);

        allCreatures.Add(creature);
        creatureZoneMap[creature] = zoneIndex;
        zoneCreatures[zoneIndex].Add(creature);

        return creature;
    }

    public CreatureBrain GetPlayer()
    {
        return playerCreature;
    }
}