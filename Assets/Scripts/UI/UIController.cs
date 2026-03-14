using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    public UIMovePad MovePad { get; private set; }

    private CreatureBrain player;

    public Button[] skillButtons;
    CombatController combat;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        MovePad = GetComponentInChildren<UIMovePad>(true);

        for (int i = 0; i < skillButtons.Length; i++)
        {
            int index = i;

            skillButtons[i].onClick.AddListener(() =>
            {
                if (combat != null)
                    combat.UseSkill(index);
            });
        }
    }

        public void SetPlayer(CreatureBrain newPlayer)
    {
        player = newPlayer;

        combat = player.GetComponent<CombatController>();

        RefreshSkillButtons();
    }

    void RefreshSkillButtons()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var skill = combat.GetSkill(i);

            if (skill == null)
            {
                skillButtons[i].gameObject.SetActive(false);
            }
            else
            {
                skillButtons[i].gameObject.SetActive(true);
            }
        }
    }
    
}
