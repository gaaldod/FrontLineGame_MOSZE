using UnityEngine;

public class MapGenerator3D : MonoBehaviour
{
    public GameObject hexPrefab;
    public int width = 8;
    public int height = 8; //currently 8x4 tiles per side
    public float hexWidth = 1.0f;
    public float hexHeight = 0.866f; // sqrt(3)/2 for spacing

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xOffset = (y % 2 == 0) ? 0 : hexWidth / 2f;
                float xPos = x * hexWidth + xOffset;
                float zPos = y * hexHeight;

                GameObject hex = Instantiate(hexPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity);
                hex.transform.parent = this.transform;
                hex.name = $"Hex_{x}_{y}";
                hex.GetComponent<Tile3D>().hexPosition = new Vector2Int(x, y);
            }
        }
    }
}
