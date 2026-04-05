using UnityEngine;

public class SlashEffect : VFXBase
{
    Material mat;
    SpriteRenderer[] renderers;

    Material[] mats;

    protected override void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        mats = new Material[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            mats[i] = new Material(renderers[i].material);
            renderers[i].material = mats[i];
        }
    }

    protected override void Animate(float t)
    {
        // ✅ apply cho TẤT CẢ material
        foreach (var m in mats)
        {
            m.SetFloat("_Progress", t);
        }

        // fade
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