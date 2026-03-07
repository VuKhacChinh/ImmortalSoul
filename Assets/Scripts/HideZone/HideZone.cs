using UnityEngine;

public class HideZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        CreatureBrain creature = other.GetComponentInParent<CreatureBrain>();

        if (creature != null)
        {
            creature.SetHidden(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        CreatureBrain creature = other.GetComponentInParent<CreatureBrain>();

        if (creature != null)
        {
            creature.SetHidden(false);
        }
    }
}