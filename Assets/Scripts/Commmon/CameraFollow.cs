using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float followSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position;
        targetPos.z = transform.position.z;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followSpeed * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}