using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Skill/AOE Circle")]
public class SkillAOECircle : SkillDefinition
{
    public float radius = 2f;

    public override void Execute(CreatureBrain owner)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            owner.transform.position,
            radius
        );

        foreach (var hit in hits)
        {
            CreatureBrain target = hit.GetComponent<CreatureBrain>();

            if (target != null && target != owner)
            {
                target.TakeDamage(owner.stats.attackDamage, owner);
            }
        }
    }
}