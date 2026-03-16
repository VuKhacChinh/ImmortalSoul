using UnityEngine;

public class PlayerSkillSystem : MonoBehaviour
{
    public static PlayerSkillSystem Instance;

    public SkillDefinition[] skills = new SkillDefinition[3];

    void Awake()
    {
        Instance = this;
    }
}