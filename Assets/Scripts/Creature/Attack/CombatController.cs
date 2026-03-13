using UnityEngine;

public class CombatController : MonoBehaviour
{
    public AttackDefinition attack;

    CreatureBrain owner;

    float cooldown;

    void Awake()
    {
        owner = GetComponent<CreatureBrain>();
    }

    public bool CanAttack()
    {
        return cooldown <= 0f;
    }

    public void StartCooldown()
    {
        cooldown = attack.cooldown;
    }

    public void Tick(float dt)
    {
        if (cooldown > 0)
            cooldown -= dt;
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
}