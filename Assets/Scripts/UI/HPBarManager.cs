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

        if (barMap.ContainsKey(creature)) return;

        HPBar bar = Instantiate(hpBarPrefab, transform);
        bar.Init(creature);

        barMap.Add(creature, bar);

        // set màu sau khi add
        Color c = LevelSystem.Instance.GetLevelColor(creature.level);
        bar.SetColor(c);
    }

    public void SetHPBarColor(CreatureBrain creature, Color color)
    {
        if (!barMap.TryGetValue(creature, out HPBar bar))
            return;

        bar.SetColor(color);
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

        if (creature.currentHP > 0)
            return;

        if (barMap.TryGetValue(creature, out HPBar bar))
        {
            Destroy(bar.gameObject);
            barMap.Remove(creature);
        }
    }

    public void SetHPBarVisible(CreatureBrain creature, bool visible)
    {
        if (barMap.TryGetValue(creature, out HPBar bar))
        {
            bar.gameObject.SetActive(visible);
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