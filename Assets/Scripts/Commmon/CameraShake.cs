using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float timer = 0;

        while (timer < duration)
        {
            Vector3 basePos = transform.position;

            transform.position =
                basePos + (Vector3)Random.insideUnitCircle * magnitude;

            timer += Time.deltaTime;
            yield return null;
        }
    }
}