using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    public Image fillImage;

    private CreatureBrain creature;

    public void Init(CreatureBrain target)
    {
        creature = target;

        // Set full máu lúc tạo
        SetValue(1f);
    }

    void LateUpdate()
    {
        if (creature == null) return;

        // Chỉ update vị trí, KHÔNG update HP ở đây nữa
        Vector3 worldPos = creature.transform.position + new Vector3(0, 1.5f, 0);
        transform.position = worldPos;
    }

    // =========================================================
    // ĐƯỢC HPBarManager GỌI
    // =========================================================
    public void SetValue(float value)
    {
        fillImage.fillAmount = value;
    }
}