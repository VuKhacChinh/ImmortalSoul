using UnityEngine;

public class ClawEffect : VFXBase
{
    public float moveOffset = 0.2f;

    Vector3 startPos;

    protected override void Awake()
    {
        startPos = transform.localPosition;
    }

    protected override void Animate(float t)
    {
        // di chuyển nhẹ về phía trước
        transform.localPosition = startPos + Vector3.right * moveOffset * t;

        // scale nhẹ
        float scale = Mathf.Lerp(0.8f, 1.1f, t);
        transform.localScale = Vector3.one * scale;
    }
}