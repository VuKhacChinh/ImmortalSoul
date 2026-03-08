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

    [Tooltip("HideZone size in cells")]
    public int hideZoneSize = 2;

    public float cellSize = 4f;

    bool[,] occupied;

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

        occupied = new bool[cellsX, cellsY];

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

        // Spawn HideZones
        foreach (var cell in allCells)
        {
            if (hideSpawned >= hideZoneCount)
                break;

            if (!CanPlace(cell.x, cell.y, hideZoneSize, cellsX, cellsY))
                continue;

            Vector3 pos = CellToWorld(cell.x, cell.y, startX, startY);

            GameObject hz = Instantiate(hideZonePrefab, pos, Quaternion.identity, transform);

            SpriteRenderer sr = hz.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

            MarkOccupied(cell.x, cell.y, hideZoneSize);

            hideSpawned++;
        }

        // Spawn obstacles
        foreach (var cell in allCells)
        {
            if (occupied[cell.x, cell.y])
                continue;

            if (Random.value > 0.5f)
                continue;

            GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Length)];

            Vector3 pos = CellToWorld(cell.x, cell.y, startX, startY);

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

            float scale = Random.Range(0.9f, 1.2f);
            obj.transform.localScale *= scale;

            SpriteRenderer srObj = obj.GetComponent<SpriteRenderer>();
            if (srObj != null)
                srObj.sortingOrder = Mathf.RoundToInt(-pos.y * 10);

            occupied[cell.x, cell.y] = true;
        }
    }

    bool CanPlace(int x, int y, int size, int maxX, int maxY)
    {
        if (x + size >= maxX || y + size >= maxY)
            return false;

        for (int yy = 0; yy < size; yy++)
        {
            for (int xx = 0; xx < size; xx++)
            {
                if (occupied[x + xx, y + yy])
                    return false;
            }
        }

        return true;
    }

    void MarkOccupied(int x, int y, int size)
    {
        for (int yy = 0; yy < size; yy++)
        {
            for (int xx = 0; xx < size; xx++)
            {
                occupied[x + xx, y + yy] = true;
            }
        }
    }

    Vector3 CellToWorld(int x, int y, float startX, float startY)
    {
        float posX = startX + x * cellSize + cellSize * 0.5f;
        float posY = startY + y * cellSize + cellSize * 0.5f;

        return new Vector3(posX, posY, 0f);
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