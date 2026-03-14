using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Combat/Skill/Impact Circle")]
public class SkillImpactCircle : SkillDefinition
{
    public GameObject impactPrefab;

    public float baseRadius = 1.5f;
    public float ringStep = 1.5f;
    public int ringCount = 3;

    public float fallSpeed = 10f;
    public float spawnHeight = 6f;

    public float ringDelay = 0.35f;

    public override void Execute(CreatureBrain owner)
    {
        owner.StartCoroutine(SpawnRings(owner));
    }

    IEnumerator SpawnRings(CreatureBrain owner)
    {
        Vector3 center = owner.transform.root.position;

        for (int r = 0; r < ringCount; r++)
        {
            float currentRadius = baseRadius + r * ringStep;

            GameObject obj = Instantiate(
                impactPrefab,
                center,
                Quaternion.identity
            );

            // scale vòng tròn
            obj.transform.localScale = Vector3.one * currentRadius;

            ImpactAOE impact = obj.GetComponent<ImpactAOE>();

            impact.Init(
                owner,
                center,
                currentRadius,
                fallSpeed,
                spawnHeight
            );

            yield return new WaitForSeconds(ringDelay);
        }
    }
}