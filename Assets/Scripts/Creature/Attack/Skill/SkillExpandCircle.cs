using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Skill/Expand Circle")]
public class SkillExpandCircle : SkillDefinition
{
    public GameObject expandPrefab;

    public float maxRadius = 6f;
    public float expandSpeed = 8f;   // đơn vị: units/second
    public float ringWidth = 0.4f;   // độ dày vành sóng để bắt va chạm
    public float visualScale = 2f;   // scale sprite (đường kính)

    public override void Execute(CreatureBrain owner)
    {
        Vector3 pos = owner.transform.root.position;

        GameObject obj = Instantiate(
            expandPrefab,
            pos,
            Quaternion.identity
        );

        ExpandAOE aoe = obj.GetComponent<ExpandAOE>();

        aoe.Init(
            owner,
            maxRadius,
            expandSpeed,
            ringWidth,
            visualScale
        );
    }
}