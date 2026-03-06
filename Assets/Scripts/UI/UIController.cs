using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    public UIMovePad MovePad { get; private set; }

    private CreatureBrain player;

    public void SetPlayer(CreatureBrain newPlayer)
    {
        player = newPlayer;
    }

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
    }
}
