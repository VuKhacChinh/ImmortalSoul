using UnityEngine;

public abstract class VFXBase : MonoBehaviour
{
    public float duration = 0.25f;

    protected float time;
    protected float t;

    protected virtual void Update()
    {
        time += Time.deltaTime;
        t = time / duration;

        Animate(t);

        if (t >= 1f)
            Destroy(gameObject);
    }

    protected abstract void Animate(float t);
    protected virtual void Awake() {}
}