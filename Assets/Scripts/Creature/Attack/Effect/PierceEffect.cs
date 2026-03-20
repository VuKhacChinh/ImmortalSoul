using UnityEngine;

public class PierceEffect : VFXBase
{
    public float lengthStart = 0.2f;
    public float lengthEnd = 1.5f;

    protected override void Animate(float t)
    {
        float length = Mathf.Lerp(lengthStart, lengthEnd, t);

        transform.localScale = new Vector3(length, 1f, 1f);

        // fade cuối
        float alpha = 1f;
        if (t > 0.6f)
            alpha = 1 - (t - 0.6f) / 0.4f;

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}