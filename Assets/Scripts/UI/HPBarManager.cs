using System.Collections.Generic;
using UnityEngine;

public class HPBarManager : MonoBehaviour
{
    public static HPBarManager Instance;

    [Header("Prefab")]
    public HPBar hpBarPrefab;

    // Map Creature -> HPBar
    private Dictionary<CreatureBrain, HPBar> barMap = new();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // =========================================================
    // CREATE
    // =========================================================
    public void CreateHPBar(CreatureBrain creature)
    {
        if (creature == null) return;

        // Nếu đã tồn tại thì không tạo lại
        if (barMap.ContainsKey(creature)) return;

        HPBar bar = Instantiate(hpBarPrefab, transform);
        bar.Init(creature);

        barMap.Add(creature, bar);
    }

    // =========================================================
    // UPDATE HP (THÊM MỚI)
    // =========================================================
    public void UpdateHP(CreatureBrain creature, float normalizedValue)
    {
        if (creature == null) return;

        if (barMap.TryGetValue(creature, out HPBar bar))
        {
            bar.SetValue(normalizedValue);
        }
    }

    // =========================================================
    // REMOVE
    // =========================================================
    public void RemoveHPBar(CreatureBrain creature)
    {
        if (creature == null) return;

        if (barMap.TryGetValue(creature, out HPBar bar))
        {
            Destroy(bar.gameObject);
            barMap.Remove(creature);
        }
    }

    // =========================================================
    // OPTIONAL: CLEAR ALL (tiện reset stage)
    // =========================================================
    public void ClearAll()
    {
        foreach (var pair in barMap)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        barMap.Clear();
    }
}