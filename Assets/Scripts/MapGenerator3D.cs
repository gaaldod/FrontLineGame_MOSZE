using UnityEngine;

public class HexMap3D : MonoBehaviour
{
    public GameObject hexPrefab;
    public int width = 8;
    public int height = 8;
    public float hexSize = 1f;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        float xOffset = hexSize * 0.75f;
        float zOffset = hexSize * 0.8660254f; // sqrt(3)/2

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * xOffset;
                float zPos = z * zOffset + (x % 2 == 1 ? zOffset / 2f : 0f);

                Vector3 position = new Vector3(xPos, 0, zPos);
                Instantiate(hexPrefab, position, Quaternion.identity, transform);
            }
        }
    }
}