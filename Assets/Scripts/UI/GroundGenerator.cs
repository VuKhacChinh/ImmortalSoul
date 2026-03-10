using UnityEngine;

public class GroundGenerator : MonoBehaviour
{
    public Sprite groundSprite;
    public int gridSize = 20;

    [Header("Boundary")]
    public float boundaryThickness = 5f;
    public GameObject boundaryHorizontalPrefab;
    public GameObject boundaryVerticalPrefab;

    [Header("Boss Zone")]
    public GameObject breakableWallPrefab;

    public float MapWidth { get; private set; }
    public float MapHeight { get; private set; }

    float bossZoneSize;

    float tileWidth;
    float tileHeight;
    float tileSize;

    public System.Action OnGroundReady;

    void Start()
    {
        GenerateGround();
        SpawnEyeFormation();
    }

    void GenerateGround()
    {
        transform.position = Vector3.zero;

        tileWidth = groundSprite.bounds.size.x;
        tileHeight = groundSprite.bounds.size.y;

        tileSize = Mathf.Max(tileWidth, tileHeight);

        MapWidth = gridSize * tileSize;
        MapHeight = gridSize * tileSize;

        bossZoneSize = MapWidth * 0.25f;

        float halfWidth = MapWidth * 0.5f;
        float halfHeight = MapHeight * 0.5f;

        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;

        int extraX = Mathf.CeilToInt((camWidth * 0.5f) / tileSize);
        int extraY = Mathf.CeilToInt((camHeight * 0.5f) / tileSize);

        int totalX = gridSize + extraX * 2;
        int totalY = gridSize + extraY * 2;

        int startIX = -gridSize/2 - extraX;
        int startIY = -gridSize/2 - extraY;

        for (int y = 0; y < totalY; y++)
        {
            for (int x = 0; x < totalX; x++)
            {
                SpawnTileFromIndex(startIX + x, startIY + y);
            }
        }

        CreateBossZones();

        OnGroundReady?.Invoke();

        CreateBoundary();
        CreateOuterBossBoundaries();
    }

    void SpawnEyeFormation()
    {
        float offset = MapWidth * 0.5f + bossZoneSize * 0.5f;

        EyeFormationManager.Instance.SpawnEyes(Vector2.zero, offset);
    }

    void SpawnTileFromIndex(int ix, int iy)
    {
        GameObject tile = new GameObject($"Tile_{ix}_{iy}");
        tile.transform.parent = transform;

        float posX = ix * tileSize + tileSize * 0.5f;
        float posY = iy * tileSize + tileSize * 0.5f;

        tile.transform.position = new Vector3(posX, posY, 0f);

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = groundSprite;

        float scaleX = tileSize / tileWidth;
        float scaleY = tileSize / tileHeight;

        tile.transform.localScale = new Vector3(scaleX, scaleY, 1);

        sr.flipX = (ix % 2 != 0);
        sr.flipY = (iy % 2 != 0);
    }

    void CreateBossZones()
    {
        int tiles = Mathf.RoundToInt((MapWidth * 0.25f) / tileSize);
        bossZoneSize = tiles * tileSize;
        int halfMain = gridSize / 2;

        SpawnZone(-tiles/2, halfMain, tiles, tiles, 4, 4);          
        SpawnZone(-tiles/2, -halfMain - tiles, tiles, tiles, 4, 4); 

        SpawnZone(-halfMain - tiles, -tiles/2, tiles, tiles, 2, 2); 
        SpawnZone(halfMain, -tiles/2, tiles, tiles, 2, 2);          

        SpawnWall(new Vector2(0, MapHeight * 0.5f), true);
        SpawnWall(new Vector2(0, -MapHeight * 0.5f), true);
        SpawnWall(new Vector2(-MapWidth * 0.5f, 0), false);
        SpawnWall(new Vector2(MapWidth * 0.5f, 0), false);
    }

    void SpawnZone(int startX, int startY, int sizeX, int sizeY, int padX, int padY)
    {
        for (int y = -padY; y < sizeY + padY; y++)
        {
            for (int x = -padX; x < sizeX + padX; x++)
            {
                SpawnTileFromIndex(startX + x, startY + y);
            }
        }
    }

    void SpawnWall(Vector2 pos, bool horizontal)
    {
        SpriteRenderer sr = breakableWallPrefab.GetComponent<SpriteRenderer>();

        float pieceLength = sr.bounds.size.x;

        int count = Mathf.CeilToInt(bossZoneSize / pieceLength);

        float startOffset = -bossZoneSize * 0.5f + pieceLength * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float offset = startOffset + i * pieceLength;

            Vector2 spawnPos;

            if (horizontal)
                spawnPos = new Vector2(pos.x + offset, pos.y);
            else
                spawnPos = new Vector2(pos.x, pos.y + offset);

            Quaternion rot = horizontal
                ? Quaternion.identity
                : Quaternion.Euler(0, 0, 90f);

            Instantiate(breakableWallPrefab, spawnPos, rot, transform);
        }
    }

    void CreateBoundary()
    {
        float halfWidth = MapWidth * 0.5f;
        float halfHeight = MapHeight * 0.5f;

        float hSize = boundaryHorizontalPrefab.GetComponent<SpriteRenderer>().bounds.size.x;
        float vSize = boundaryVerticalPrefab.GetComponent<SpriteRenderer>().bounds.size.y;

        int fullH = Mathf.FloorToInt(MapWidth / hSize);

        for (int i = 0; i < fullH; i++)
        {
            float x = -halfWidth + hSize * 0.5f + i * hSize;

            if (Mathf.Abs(x) < MapWidth * 0.125f) continue;

            SpawnBoundary(boundaryHorizontalPrefab, new Vector2(x, halfHeight));
            SpawnBoundary(boundaryHorizontalPrefab, new Vector2(x, -halfHeight));
        }

        int fullV = Mathf.FloorToInt(MapHeight / vSize);

        for (int i = 0; i < fullV; i++)
        {
            float y = -halfHeight + vSize * 0.5f + i * vSize;

            if (Mathf.Abs(y) < MapHeight * 0.125f) continue;

            SpawnBoundary(boundaryVerticalPrefab, new Vector2(-halfWidth, y));
            SpawnBoundary(boundaryVerticalPrefab, new Vector2(halfWidth, y));
        }
    }

    void CreateOuterBossBoundaries()
    {
        float halfWidth = MapWidth * 0.5f;
        float halfHeight = MapHeight * 0.5f;
        float halfBoss = bossZoneSize * 0.5f;

        float leftCenterX = -halfWidth - halfBoss;
        float rightCenterX = halfWidth + halfBoss;
        float topCenterY = halfHeight + halfBoss;
        float bottomCenterY = -halfHeight - halfBoss;

        SpawnScaledHorizontal(leftCenterX, halfBoss);
        SpawnScaledHorizontal(leftCenterX, -halfBoss);

        SpawnScaledHorizontal(rightCenterX, halfBoss);
        SpawnScaledHorizontal(rightCenterX, -halfBoss);

        SpawnScaledVertical(-halfBoss, topCenterY);
        SpawnScaledVertical(halfBoss, topCenterY);

        SpawnScaledVertical(-halfBoss, bottomCenterY);
        SpawnScaledVertical(halfBoss, bottomCenterY);

        SpawnScaledVertical(leftCenterX - halfBoss, 0);
        SpawnScaledVertical(rightCenterX + halfBoss, 0);

        SpawnScaledHorizontal(0, topCenterY + halfBoss);
        SpawnScaledHorizontal(0, bottomCenterY - halfBoss);
    }

    void SpawnScaledHorizontal(float centerX, float y)
    {
        float piece = boundaryHorizontalPrefab.GetComponent<SpriteRenderer>().bounds.size.x;

        int full = Mathf.FloorToInt(bossZoneSize / piece);
        float remain = bossZoneSize - full * piece;

        float start = centerX - bossZoneSize * 0.5f;

        for (int i = 0; i < full; i++)
        {
            float x = start + piece * 0.5f + i * piece;
            SpawnBoundary(boundaryHorizontalPrefab, new Vector2(x, y));
        }

        if (remain > 0.01f)
        {
            float x = start + full * piece + remain * 0.5f;

            GameObject obj = Instantiate(boundaryHorizontalPrefab,
                new Vector2(x, y),
                Quaternion.identity,
                transform);

            Vector3 s = obj.transform.localScale;
            obj.transform.localScale = new Vector3(s.x * remain / piece, s.y, s.z);
        }
    }

    void SpawnScaledVertical(float x, float centerY)
    {
        float piece = boundaryVerticalPrefab.GetComponent<SpriteRenderer>().bounds.size.y;

        int full = Mathf.FloorToInt(bossZoneSize / piece);
        float remain = bossZoneSize - full * piece;

        float start = centerY - bossZoneSize * 0.5f;

        for (int i = 0; i < full; i++)
        {
            float y = start + piece * 0.5f + i * piece;
            SpawnBoundary(boundaryVerticalPrefab, new Vector2(x, y));
        }

        if (remain > 0.01f)
        {
            float y = start + full * piece + remain * 0.5f;

            GameObject obj = Instantiate(boundaryVerticalPrefab,
                new Vector2(x, y),
                Quaternion.identity,
                transform);

            Vector3 s = obj.transform.localScale;
            obj.transform.localScale = new Vector3(s.x, s.y * remain / piece, s.z);
        }
    }

    void SpawnBoundary(GameObject prefab, Vector2 pos)
    {
        Instantiate(prefab, pos, Quaternion.identity, transform);
    }
}