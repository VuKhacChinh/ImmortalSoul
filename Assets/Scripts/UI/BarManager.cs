using System.Collections.Generic;
using UnityEngine;

public class BarManager : MonoBehaviour
{
    public static BarManager Instance;

    [Header("Prefab")]
    public Bar barPrefab;

    [Header("VFX")]
    public GameObject healEffectPrefab;

    private Dictionary<CreatureBrain, Bar> barMap = new();
    private Dictionary<CreatureBrain, float> lastHPMap = new();    

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

    public void CreateBar(CreatureBrain creature)
    {
        if (creature == null) return;
        if (barMap.ContainsKey(creature)) return;

        Bar bar = Instantiate(barPrefab, transform);
        bar.Init(creature);

        barMap.Add(creature, bar);

        // ✅ INIT HP
        lastHPMap[creature] = creature.runtime.HP;

        Color c = LevelSystem.Instance.GetLevelColor(creature.level);
        bar.SetColor(c);

        bar.gameObject.SetActive(creature.isPlayerControlled);
    }

    public Bar GetHPBar(CreatureBrain creature)
    {
        if (barMap.TryGetValue(creature, out Bar bar))
            return bar;

        return null;
    }

    public void SetHPBarColor(CreatureBrain creature, Color color)
    {
        if (!barMap.TryGetValue(creature, out Bar bar))
            return;

        bar.SetColor(color);
    }

    // =========================================================
    // UPDATE HP
    // =========================================================

    public void UpdateHP(CreatureBrain creature, float normalizedValue)
    {
        if (creature == null) return;

        if (barMap.TryGetValue(creature, out Bar bar))
        {
            float currentHP = creature.runtime.HP;

            // ===== CHECK HEAL =====
            if (lastHPMap.TryGetValue(creature, out float lastHP))
            {
                if (currentHP > lastHP)
                {
                    PlayHealEffect(creature);
                }
            }

            lastHPMap[creature] = currentHP;

            bar.SetHP(normalizedValue);
        }
    }

    void PlayHealEffect(CreatureBrain creature)
    {
        if (healEffectPrefab == null) return;

        GameObject fx = Instantiate(
            healEffectPrefab,
            creature.transform.position,
            Quaternion.identity,
            creature.transform
        );

        Destroy(fx, 1.5f);
    }

    // =========================================================
    // UPDATE MP
    // =========================================================

    public void UpdateMP(CreatureBrain creature, float normalizedValue)
    {
        if (creature == null) return;

        if (barMap.TryGetValue(creature, out Bar bar))
        {
            bar.SetMP(normalizedValue);
        }
    }

    public void SetMPVisible(CreatureBrain creature, bool visible)
    {
        if (barMap.TryGetValue(creature, out Bar bar))
        {
            bar.SetMPVisible(visible);
        }
    }

    // =========================================================
    // REMOVE
    // =========================================================

    public void RemoveHPBar(CreatureBrain creature)
    {
        if (creature == null) return;

        if (barMap.TryGetValue(creature, out Bar bar))
        {
            Destroy(bar.gameObject);
            barMap.Remove(creature);
        }

        lastHPMap.Remove(creature);
    }

    public void SetHPBarVisible(CreatureBrain creature, bool visible)
    {
        if (barMap.TryGetValue(creature, out Bar bar))
        {
            bar.gameObject.SetActive(visible);
        }
    }

    public void UpdateXP(CreatureBrain creature, float normalizedValue)
    {
        if (barMap.TryGetValue(creature, out Bar bar))
        {
            bar.SetXP(normalizedValue);
        }
    }

    public void RefreshBar(CreatureBrain creature)
    {
        if (barMap.TryGetValue(creature, out Bar bar))
        {
            bar.UpdatePlayerState();
        }
    }

    // =========================================================
    // CLEAR
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