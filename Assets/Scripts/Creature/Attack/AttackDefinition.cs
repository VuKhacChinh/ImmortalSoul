using UnityEngine;

public abstract class AttackDefinition : ScriptableObject
{
    public float range = 1.5f;
    public float cooldown = 1f;

    public abstract void Execute(CreatureBrain owner);
}