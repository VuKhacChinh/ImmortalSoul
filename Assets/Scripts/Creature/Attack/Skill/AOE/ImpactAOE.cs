using UnityEngine;
using System.Collections;

public class ImpactAOE : AOEBase
{
    float fallSpeed;
    float targetY;

    bool impacted = false;

    public void Init(
        CreatureBrain owner,
        Vector3 targetPos,
        float radius,
        float fallSpeed,
        float spawnHeight
    )
    {
        InitBase(owner, radius);

        this.fallSpeed = fallSpeed;
        targetY = targetPos.y;

        transform.position = new Vector3(
            targetPos.x,
            targetPos.y + spawnHeight,
            targetPos.z
        );

        StartCoroutine(FallRoutine());
    }

    IEnumerator FallRoutine()
    {
        while (!impacted)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            if (transform.position.y <= targetY)
            {
                Impact();
                yield break;
            }

            yield return null;
        }
    }

    void Impact()
    {
        impacted = true;

        transform.position = new Vector3(
            transform.position.x,
            targetY,
            transform.position.z
        );

        DoAOEDamage();

        CameraShake.Instance.Shake(0.15f, 0.08f);

        Destroy(gameObject);
    }
}