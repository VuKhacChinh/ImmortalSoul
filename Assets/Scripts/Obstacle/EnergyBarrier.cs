using UnityEngine;

public class EnergyBarrier : MonoBehaviour
{
    public int requiredLevel = 5;

    [Header("Block Config")]
    public float pushForce = 8f;
    public float knockbackDuration = 0.2f;

    void OnTriggerStay2D(Collider2D other)
    {
        CreatureBrain creature = other.GetComponentInParent<CreatureBrain>();
        if (creature == null)
            return;

        // đủ level → cho đi
        if (creature.level >= requiredLevel)
            return;

        HandleBlocked(creature);
    }

    void HandleBlocked(CreatureBrain creature)
    {
        Rigidbody2D rb = creature.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        Collider2D barrierCol = GetComponent<Collider2D>();

        // điểm gần nhất trên barrier
        Vector2 closest = barrierCol.ClosestPoint(creature.transform.position);

        // hướng đẩy ra ngoài
        Vector2 dir = (creature.transform.position - (Vector3)closest).normalized;

        // fallback nếu bị kẹt chính giữa
        if (dir == Vector2.zero)
            dir = (creature.transform.position - transform.position).normalized;

        // 🚫 KHÔNG dùng AddForce nữa → dùng velocity trực tiếp để CHẶN
        rb.linearVelocity = dir * pushForce;

        // chặn AI/player override
        creature.ApplyKnockback(knockbackDuration);

        // chat (chỉ hiện 1 lần cho đỡ spam)
        if (creature.isPlayerControlled && SpeechBubbleSystem.Instance != null)
        {
            SpeechBubbleSystem.Instance.Say(
                $"Đáng ghét... cần level >= {requiredLevel}!",
                Emotion.Angry,
                2f
            );
        }
    }
}