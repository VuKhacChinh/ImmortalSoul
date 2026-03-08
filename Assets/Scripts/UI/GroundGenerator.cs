using UnityEngine;

public class GroundGenerator : MonoBehaviour
{
    public Sprite groundSprite;
    public int gridSize = 20;

    [Header("Boundary")]
    public float boundaryThickness = 5f;
    public GameObject boundaryHorizontalPrefab;
    public GameObject boundaryVerticalPrefab;

    public float MapWidth { get; private set; }
    public float MapHeight { get; private set; }

    public System.Action OnGroundReady;

    void Start()
    {
        GenerateGround();
    }

    void GenerateGround()
    {
        transform.position = Vector3.zero;

        float tileWidth = groundSprite.bounds.size.x;
        float tileHeight = groundSprite.bounds.size.y;

        MapWidth = gridSize * tileWidth;
        MapHeight = gridSize * tileHeight;

        float halfWidth = MapWidth * 0.5f;
        float halfHeight = MapHeight * 0.5f;

        // ===== CAMERA SIZE =====

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;

        // Spawn thêm vừa đủ để che camera
        int extraX = Mathf.CeilToInt((camWidth * 0.5f) / tileWidth);
        int extraY = Mathf.CeilToInt((camHeight * 0.5f) / tileHeight);

        int totalX = gridSize + extraX * 2;
        int totalY = gridSize + extraY * 2;

        float startX = -halfWidth - extraX * tileWidth;
        float startY = -halfHeight - extraY * tileHeight;

        for (int y = 0; y < totalY; y++)
        {
            for (int x = 0; x < totalX; x++)
            {
                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.parent = transform;

                float posX = startX + x * tileWidth + tileWidth * 0.5f;
                float posY = startY + y * tileHeight + tileHeight * 0.5f;

                tile.transform.position = new Vector3(posX, posY, 0f);

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = groundSprite;

                sr.flipX = (x % 2 == 1);
                sr.flipY = (y % 2 == 1);
            }
        }

        OnGroundReady?.Invoke();
        CreateBoundary();
    }

    void CreateBoundary()
    {
        float halfWidth = MapWidth * 0.5f;
        float halfHeight = MapHeight * 0.5f;

        float hSize = boundaryHorizontalPrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        float vSize = boundaryVerticalPrefab.GetComponent<SpriteRenderer>().bounds.size.y;

        // ===== TOP / BOTTOM =====

        int fullH = Mathf.FloorToInt(MapWidth / hSize);
        float remainH = MapWidth - fullH * hSize;

        for (int i = 0; i < fullH; i++)
        {
            float x = -halfWidth + hSize * 0.5f + i * hSize;

            SpawnBoundary(boundaryHorizontalPrefab, new Vector2(x, halfHeight));
            SpawnBoundary(boundaryHorizontalPrefab, new Vector2(x, -halfHeight));
        }

        if (remainH > 0.01f)
        {
            float x = -halfWidth + fullH * hSize + remainH * 0.5f;

            SpawnBoundaryScaled(boundaryHorizontalPrefab, new Vector2(x, halfHeight), remainH / hSize);
            SpawnBoundaryScaled(boundaryHorizontalPrefab, new Vector2(x, -halfHeight), remainH / hSize);
        }

        // ===== LEFT / RIGHT =====

        int fullV = Mathf.FloorToInt(MapHeight / vSize);
        float remainV = MapHeight - fullV * vSize;

        for (int i = 0; i < fullV; i++)
        {
            float y = -halfHeight + vSize * 0.5f + i * vSize;

            SpawnBoundary(boundaryVerticalPrefab, new Vector2(-halfWidth, y));
            SpawnBoundary(boundaryVerticalPrefab, new Vector2(halfWidth, y));
        }

        if (remainV > 0.01f)
        {
            float y = -halfHeight + fullV * vSize + remainV * 0.5f;

            SpawnBoundaryScaled(boundaryVerticalPrefab, new Vector2(-halfWidth, y), remainV / vSize, false);
            SpawnBoundaryScaled(boundaryVerticalPrefab, new Vector2(halfWidth, y), remainV / vSize, false);
        }
    }

    void SpawnBoundary(GameObject prefab, Vector2 pos)
    {
        Instantiate(prefab, pos, Quaternion.identity, transform);
    }

    void SpawnBoundaryScaled(GameObject prefab, Vector2 pos, float scale, bool horizontal = true)
    {
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

        Vector3 baseScale = prefab.transform.localScale;

        if (horizontal)
            obj.transform.localScale = new Vector3(baseScale.x * scale, baseScale.y, baseScale.z);
        else
            obj.transform.localScale = new Vector3(baseScale.x, baseScale.y * scale, baseScale.z);
    }
}