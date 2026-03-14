using UnityEngine;

[System.Serializable]
public class CreatureStats
{
    [Header("Health")]
    public float maxHP = 100;

    [Header("Mana")]
    public float maxMP = 50;
    public float mpRegen = 5f;

    [Header("Combat")]
    public float attackDamage = 10;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("AI")]
    public float visionRange = 5f;
}