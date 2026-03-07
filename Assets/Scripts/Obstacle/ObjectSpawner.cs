using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    public GroundGenerator ground;

    [Header("Environment Objects")]
    public GameObject[] objectPrefabs;

    [Header("HideZone")]
    public GameObject hideZonePrefab;
    public int hideZoneCount = 10;

    public float cellSize = 4f;

    void OnEnable()
    {
        ground.OnGroundReady += SpawnObjects;
    }

    void OnDisable()
    {
        ground.OnGroundReady -= SpawnObjects;
    }

    void SpawnObjects()
    {
        float width = ground.MapWidth;
        float height = ground.MapHeight;

        int cellsX = Mathf.FloorToInt(width / cellSize);
        int cellsY = Mathf.FloorToInt(height / cellSize);

        float startX = -width * 0.5f;
        float startY = -height * 0.5f;

        List<Vector2Int> allCells = new List<Vector2Int>();

        for (int y = 0; y < cellsY; y++)
        {
            for (int x = 0; x < cellsX; x++)
            {
                allCells.Add(new Vector2Int(x, y));
            }
        }

        Shuffle(allCells);

        int hideSpawned = 0;

        for (int i = 0; i < allCells.Count; i++)
        {
            Vector2Int cell = allCells[i];

            float offsetX = Random.Range(0.5f, cellSize - 0.5f);
            float offsetY = Random.Range(0.5f, cellSize - 0.5f);

            float posX = startX + cell.x * cellSize + offsetX;
            float posY = startY + cell.y * cellSize + offsetY;

            Vector3 pos = new Vector3(posX, posY, 0f);

            // Spawn HideZone trước
            if (hideSpawned < hideZoneCount)
            {
                GameObject hz = Instantiate(hideZonePrefab, pos, Quaternion.identity, transform);

                SpriteRenderer sr = hz.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

                hideSpawned++;
                continue;
            }

            // Spawn object bình thường
            if (Random.value > 0.5f)
                continue;

            GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

            float scale = Random.Range(0.9f, 1.2f);
            obj.transform.localScale *= scale;

            SpriteRenderer srObj = obj.GetComponent<SpriteRenderer>();
            if (srObj != null)
                srObj.sortingOrder = Mathf.RoundToInt(-pos.y * 10);
        }
    }

    void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            Vector2Int temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}