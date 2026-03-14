using UnityEngine;

public class LevelSystem : MonoBehaviour
{
    public static LevelSystem Instance;

    [Header("Level")]
    public int maxLevel = 100;

    [Header("HP Bar Level Colors")]
    public int levelColorStep = 10;

    public Color[] levelColors =
    {
        Color.green,
        Color.cyan,
        Color.blue,
        Color.magenta,
        Color.red
    };

    [Header("XP Curve")]
    public int baseXP = 20;
    public float xpMultiplier = 1.35f;

    [Header("Stat Growth")]
    public float hpPerLevel = 10f;
    public float damagePerLevel = 2f;
    public float speedPerLevel = 0.01f;
    public float scalePerLevel = 0.02f;

    void OnEnable()
    {
        Instance = this;
    }

    public int GetXPRequired(int level)
    {
        int xp = baseXP;

        for (int i = 1; i < level; i++)
            xp = Mathf.RoundToInt(xp * xpMultiplier);

        return xp;
    }

    public void GainXP(CreatureBrain creature, int xp)
    {
        creature.currentXP += xp;

        while (creature.currentXP >= GetXPRequired(creature.level)
            && creature.level < maxLevel)
        {
            creature.currentXP -= GetXPRequired(creature.level);
            LevelUp(creature);
        }

        UpdateXPBar(creature);
    }

    void UpdateXPBar(CreatureBrain creature)
    {
        if (!creature.isPlayerControlled) return;

        if (BarManager.Instance == null) return;

        float percent = GetXPPercent(creature);

        BarManager.Instance.UpdateXP(creature, percent);
    }

    void LevelUp(CreatureBrain creature)
    {
        creature.level++;

        ApplyLevelStats(creature);

        creature.runtime.HP = creature.stats.maxHP;

        UpdateHPBar(creature);

        Debug.Log(creature.name + " LEVEL UP -> " + creature.level);
    }

    public void SetLevel(CreatureBrain creature, int targetLevel)
    {
        if (creature == null) return;

        targetLevel = Mathf.Clamp(targetLevel, 1, maxLevel);

        int diff = targetLevel - creature.level;

        for (int i = 0; i < diff; i++)
        {
            creature.level++;
            ApplyLevelStats(creature);
        }

        creature.runtime.HP = creature.stats.maxHP;

        UpdateHPBar(creature);
    }

    void UpdateHPBar(CreatureBrain creature)
    {
        if (BarManager.Instance == null) return;

        BarManager.Instance.UpdateHP(creature, 1f);

        Color c = GetLevelColor(creature.level);
        BarManager.Instance.SetHPBarColor(creature, c);
    }

    void ApplyLevelStats(CreatureBrain creature)
    {
        creature.stats.maxHP += hpPerLevel;
        creature.stats.attackDamage += damagePerLevel + creature.level * 0.15f;
        creature.stats.moveSpeed += speedPerLevel;

        Vector3 s = creature.transform.localScale;

        float sign = Mathf.Sign(s.x);

        s.x = sign * (Mathf.Abs(s.x) + scalePerLevel);
        s.y += scalePerLevel;

        creature.transform.localScale = s;
    }

    public int GetLevelColorTier(int level)
    {
        if (levelColorStep <= 0)
            return 0;

        return level / levelColorStep;
    }

    public Color GetLevelColor(int level)
    {
        int tier = GetLevelColorTier(level);

        if (levelColors == null || levelColors.Length == 0)
            return Color.green;

        if (tier >= levelColors.Length)
            return levelColors[levelColors.Length - 1];

        return levelColors[tier];
    }

    public float GetXPPercent(CreatureBrain creature)
    {
        int required = GetXPRequired(creature.level);

        if (required <= 0) return 0;

        return (float)creature.currentXP / required;
    }

}