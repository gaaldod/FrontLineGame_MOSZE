using UnityEngine;

public class WorldHexMap3D : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject groundHexPrefab;
    public GameObject castleHexPrefab;

    [Header("Map Settings")]
    public int width = 8;
    public int height = 8;
    public float hexSize = 1f;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        float xOffset = hexSize * 0.5f;
        float zOffset = hexSize * 1.73f;

        height = height / 2;
        width = width * 2;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * xOffset;
                float zPos = z * zOffset + (x % 2 == 1 ? zOffset / 2f : 0f);
                Vector3 position = new Vector3(xPos, 0, zPos);

                GameObject tile;
                bool isCastle = false;

                // Vár generálása a jobb felső sarokba
                if (x == width - 1 && z == height - 1)
                {
                    tile = Instantiate(castleHexPrefab, position, Quaternion.identity, transform);
                    tile.tag = "Castle";
                    isCastle = true;
                }
                else
                {
                    tile = Instantiate(groundHexPrefab, position, Quaternion.identity, transform);
                }

                // Layer beállítás
                if (x < width / 2)
                    tile.layer = LayerMask.NameToLayer("LeftZone");
                else
                    tile.layer = LayerMask.NameToLayer("RightZone");

                // WorldHexTile komponens és koordináták
                var hexTile = tile.GetComponent<WorldHexTile>();
                if (hexTile == null)
                    hexTile = tile.AddComponent<WorldHexTile>();

                hexTile.hexX = x;
                hexTile.hexZ = z;
                hexTile.isCastleTile = isCastle; // ez jelöli a vár mezőt a WorldManager számára
            }
        }

        Debug.Log($"✅ Hex map generálva: {width} x {height}");
    }
}
