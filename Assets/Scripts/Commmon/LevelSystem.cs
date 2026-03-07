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
    }

    void LevelUp(CreatureBrain creature)
    {
        creature.level++;

        creature.maxHP += hpPerLevel;
        creature.attackDamage += damagePerLevel + creature.level * 0.15f;
        creature.moveSpeed += speedPerLevel;

        creature.currentHP = creature.maxHP;

        Vector3 s = creature.transform.localScale;

        float sign = Mathf.Sign(s.x);

        s.x = sign * (Mathf.Abs(s.x) + scalePerLevel);
        s.y += scalePerLevel;

        creature.transform.localScale = s;

        // ===== UPDATE HP BAR COLOR =====

        Color c = GetLevelColor(creature.level);
        HPBarManager.Instance.SetHPBarColor(creature, c);

        Debug.Log(creature.name + " LEVEL UP -> " + creature.level);
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

}