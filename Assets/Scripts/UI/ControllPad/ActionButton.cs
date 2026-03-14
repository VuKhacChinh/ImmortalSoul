using UnityEngine;
using UnityEngine.UI;

public class ActionButton : MonoBehaviour
{
    public int skillIndex = -1; // -1 = attack
    public Image cooldownImage;

    CombatController combat;

    void Update()
    {
        CreatureBrain player = GameManager.Instance.GetPlayer();

        if (player == null) return;

        CombatController newCombat = player.GetComponent<CombatController>();

        if (combat != newCombat)
            combat = newCombat;

        if (combat == null) return;

        float remain;
        float max;

        if (skillIndex < 0)
        {
            remain = combat.AttackCooldownRemaining;
            max = combat.AttackCooldownMax;
        }
        else
        {
            remain = combat.GetSkillCooldownRemaining(skillIndex);
            max = combat.GetSkillCooldownMax(skillIndex);
        }

        if (max > 0)
            cooldownImage.fillAmount = remain / max;
    }

    public void OnPressed()
    {
        CreatureBrain player = GameManager.Instance.GetPlayer();

        if (player == null) return;

        if (skillIndex < 0)
            player.TryAttack();
        else
            player.GetComponent<CombatController>().UseSkill(skillIndex);
    }
}