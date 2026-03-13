using UnityEngine;

public abstract class SkillDefinition : ScriptableObject
{
    public float range = 2f;
    public float cooldown = 5f;

    public float mpCost = 10f;

    public abstract void Execute(CreatureBrain owner);
}