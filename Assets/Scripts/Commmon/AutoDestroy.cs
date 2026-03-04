using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public float lifeTime = 0.3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}