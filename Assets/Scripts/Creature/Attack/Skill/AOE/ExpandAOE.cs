using UnityEngine;
using System.Collections.Generic;

public class ExpandAOE : AOEBase
{
    float maxRadius;
    float expandSpeed;
    float ringWidth;
    float visualScale;

    float currentRadius = 0f;
    float prevRadius = 0f;

    SpriteRenderer sr;

    HashSet<CreatureBrain> hitTargets = new HashSet<CreatureBrain>();

    public void Init(
        CreatureBrain owner,
        float maxRadius,
        float expandSpeed,
        float ringWidth,
        float visualScale
    )
    {
        InitBase(owner, 0);

        this.maxRadius = maxRadius;
        this.expandSpeed = expandSpeed;
        this.ringWidth = ringWidth;
        this.visualScale = visualScale;

        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        prevRadius = currentRadius;

        currentRadius += expandSpeed * Time.deltaTime;

        radius = currentRadius;

        // scale visual
        transform.localScale = Vector3.one * currentRadius * visualScale;

        // fade shockwave
        if (sr != null)
        {
            float t = currentRadius / maxRadius;

            Color c = sr.color;
            c.a = 1f - t;

            sr.color = c;
        }

        // ring width dynamic (shockwave tự nhiên hơn)
        float dynamicRingWidth = ringWidth + currentRadius * 0.05f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            currentRadius
        );

        foreach (var hit in hits)
        {
            CreatureBrain target = hit.GetComponentInParent<CreatureBrain>();

            if (target == null || target == owner) continue;
            if (hitTargets.Contains(target)) continue;

            float dist = Vector2.Distance(
                transform.position,
                target.transform.position
            );

            if (dist >= prevRadius - dynamicRingWidth && dist <= currentRadius)
            {
                hitTargets.Add(target);

                DamageTarget(target);
            }
        }

        if (currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }
}