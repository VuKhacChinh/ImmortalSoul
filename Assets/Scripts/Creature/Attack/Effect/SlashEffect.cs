using UnityEngine;

public class SlashEffect : VFXBase
{
    Material mat;
    SpriteRenderer[] renderers;

    public float startScale = 0.8f;
    public float endScale = 1.2f;

    protected override void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var r in renderers)
        {
            r.material = new Material(r.material); // clone
            mat = r.material;
        }
    }

    protected override void Animate(float t)
    {
        // reveal shader
        if (mat != null)
            mat.SetFloat("_Progress", t);

        // scale
        float scale = Mathf.Lerp(startScale, endScale, t);
        transform.localScale = Vector3.one * scale;

        // fade (chỉ fade cuối)
        float alpha = 1f;
        if (t > 0.7f)
            alpha = 1 - (t - 0.7f) / 0.3f;

        foreach (var r in renderers)
        {
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }
}