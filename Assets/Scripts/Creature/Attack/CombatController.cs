using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Basic Attack")]
    public AttackDefinition attack;

    [Header("Skills")]
    public SkillDefinition[] skills;

    CreatureBrain owner;

    float cooldown;

    float[] skillCooldowns;

    void Awake()
    {
        owner = GetComponent<CreatureBrain>();

        if (skills != null)
            skillCooldowns = new float[skills.Length];
    }

    public bool CanAttack()
    {
        return cooldown <= 0f;
    }

    public void StartCooldown()
    {
        if (attack != null)
            cooldown = attack.cooldown;
    }

    public void Tick(float dt)
    {
        if (cooldown > 0)
            cooldown -= dt;

        if (skillCooldowns != null)
        {
            for (int i = 0; i < skillCooldowns.Length; i++)
            {
                if (skillCooldowns[i] > 0)
                    skillCooldowns[i] -= dt;
            }
        }

        // MP regen
        owner.runtime.MP += owner.stats.mpRegen * dt;
        owner.runtime.MP = Mathf.Min(owner.runtime.MP, owner.stats.maxMP);
    }

    public void DoDamage()
    {
        if (attack == null) return;

        attack.Execute(owner);
    }

    public float AttackRange
    {
        get
        {
            if (attack == null) return 0f;
            return attack.range;
        }
    }

    // ========================
    // SKILL SYSTEM
    // ========================

    public bool CanUseSkill(int index)
    {
        if (skills == null || index < 0 || index >= skills.Length)
            return false;

        SkillDefinition skill = skills[index];

        if (skillCooldowns[index] > 0)
            return false;

        if (owner.runtime.MP < skill.mpCost)
            return false;

        return true;
    }

    public void UseSkill(int index)
    {
        if (!CanUseSkill(index)) return;

        SkillDefinition skill = skills[index];

        owner.runtime.MP -= skill.mpCost;

        skill.Execute(owner);

        skillCooldowns[index] = skill.cooldown;
    }
}