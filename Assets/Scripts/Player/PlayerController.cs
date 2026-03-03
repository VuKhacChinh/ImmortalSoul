using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    private UIMovePad movePad;

    void Awake()
    {
        Instance = this;
    }

    public void SetMovePad(UIMovePad pad)
    {
        movePad = pad;
    }

    public Vector2 GetMoveInput()
    {
        if (movePad == null)
            return Vector2.zero;

        return movePad.Input;
    }
}