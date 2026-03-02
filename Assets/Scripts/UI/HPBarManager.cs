using System.Collections.Generic;
using UnityEngine;

public class HPBarManager : MonoBehaviour
{
    public static HPBarManager Instance;

    public HPBar hpBarPrefab;

    private Dictionary<CreatureBrain, HPBar> barMap = new();

    void Awake()
    {
        Instance = this;
    }

    public void CreateHPBar(CreatureBrain creature)
    {
        HPBar bar = Instantiate(hpBarPrefab, transform);
        bar.Init(creature);

        barMap.Add(creature, bar);
    }

    public void RemoveHPBar(CreatureBrain creature)
    {
        if (barMap.TryGetValue(creature, out HPBar bar))
        {
            Destroy(bar.gameObject);
            barMap.Remove(creature);
        }
    }
}