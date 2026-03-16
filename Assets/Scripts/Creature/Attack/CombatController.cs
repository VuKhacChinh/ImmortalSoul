using UnityEngine;

public class CombatController : MonoBehaviour
{
    [Header("Basic Attack")]
    public AttackDefinition attack;

    [Header("Creature Skills")]
    public SkillDefinition[] creatureSkills;

    SkillDefinition[] finalSkills = new SkillDefinition[3];

    CreatureBrain owner;

    float cooldown;

    float[] skillCooldowns = new float[3];

    public float AttackCooldownRemaining => cooldown;

    void Awake()
    {
        owner = GetComponent<CreatureBrain>();
        BuildSkills(null);
    }

    public void BuildSkills(SkillDefinition[] playerSkills)
    {
        for (int i = 0; i < finalSkills.Length; i++)
        {
            if (playerSkills != null &&
                i < playerSkills.Length &&
                playerSkills[i] != null)
            {
                finalSkills[i] = playerSkills[i];
            }
            else if (creatureSkills != null &&
                     i < creatureSkills.Length)
            {
                finalSkills[i] = creatureSkills[i];
            }
            else
            {
                finalSkills[i] = null;
            }

            // reset cooldown khi đổi creature
            skillCooldowns[i] = 0f;
        }
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

        for (int i = 0; i < skillCooldowns.Length; i++)
        {
            if (skillCooldowns[i] > 0)
                skillCooldowns[i] -= dt;
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
        if (index < 0 || index >= finalSkills.Length)
            return false;

        SkillDefinition skill = finalSkills[index];

        if (skill == null)
            return false;

        if (skillCooldowns[index] > 0)
            return false;

        if (owner.runtime.MP < skill.mpCost)
            return false;

        return true;
    }

    public void UseSkill(int index)
    {
        if (!CanUseSkill(index))
            return;

        SkillDefinition skill = finalSkills[index];

        owner.runtime.MP -= skill.mpCost;

        skill.Execute(owner);

        skillCooldowns[index] = skill.cooldown;
    }

    public float AttackCooldownMax
    {
        get
        {
            if (attack == null) return 0;
            return attack.cooldown;
        }
    }

    public float GetSkillCooldownRemaining(int index)
    {
        if (index < 0 || index >= skillCooldowns.Length)
            return 0;

        return skillCooldowns[index];
    }

    public float GetSkillCooldownMax(int index)
    {
        if (index < 0 || index >= finalSkills.Length)
            return 0;

        SkillDefinition skill = finalSkills[index];

        if (skill == null)
            return 0;

        return skill.cooldown;
    }

    public SkillDefinition GetSkill(int index)
    {
        if (index < 0 || index >= finalSkills.Length)
            return null;

        return finalSkills[index];
    }
}