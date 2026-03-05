using UnityEngine;

public class AttackButton : MonoBehaviour
{
    public void OnAttackPressed()
    {
        CreatureBrain player = GameManager.Instance.GetPlayer();

        if (player != null)
        {
            player.TryAttack();
        }
    }
}