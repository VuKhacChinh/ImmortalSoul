using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GroundGenerator ground;
    public GameObject[] objectPrefabs;

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

        for (int y = 0; y < cellsY; y++)
        {
            for (int x = 0; x < cellsX; x++)
            {
                if (Random.value > 0.5f)
                    continue;

                float offsetX = Random.Range(0.5f, cellSize - 0.5f);
                float offsetY = Random.Range(0.5f, cellSize - 0.5f);

                float posX = startX + x * cellSize + offsetX;
                float posY = startY + y * cellSize + offsetY;

                Vector3 pos = new Vector3(posX, posY, 0f);

                GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

                float scale = Random.Range(0.9f, 1.2f);
                obj.transform.localScale *= scale;

                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);
            }
        }
    }
}