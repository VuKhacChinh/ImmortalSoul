using UnityEngine;

public class ImpactEffect : VFXBase
{
    public float startScale = 0.5f;
    public float endScale = 1.5f;

    SpriteRenderer sr;

    protected override void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void Animate(float t)
    {
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        float alpha = 1 - t;

        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}