using UnityEngine;

public class GroundGenerator : MonoBehaviour
{
    public Sprite groundSprite;
    public int gridSize = 20;
    public float boundaryThickness = 5f;

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

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.parent = transform;

                float posX = (x * tileWidth) - halfWidth + tileWidth * 0.5f;
                float posY = (y * tileHeight) - halfHeight + tileHeight * 0.5f;

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

        CreateWall(new Vector2(0, halfHeight + boundaryThickness * 0.5f), new Vector2(MapWidth, boundaryThickness)); // top
        CreateWall(new Vector2(0, -halfHeight - boundaryThickness * 0.5f), new Vector2(MapWidth, boundaryThickness)); // bottom
        CreateWall(new Vector2(-halfWidth - boundaryThickness * 0.5f, 0), new Vector2(boundaryThickness, MapHeight)); // left
        CreateWall(new Vector2(halfWidth + boundaryThickness * 0.5f, 0), new Vector2(boundaryThickness, MapHeight)); // right
    }

    void CreateWall(Vector2 pos, Vector2 size)
    {
        GameObject wall = new GameObject("Boundary");

        wall.transform.parent = transform;
        wall.transform.position = pos;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

}