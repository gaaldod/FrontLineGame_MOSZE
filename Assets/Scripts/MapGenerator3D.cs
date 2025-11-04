using UnityEngine;

public class HexMap3D : MonoBehaviour
{
    public GameObject groundHexPrefab;
    public GameObject castleHexPrefab;
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
        float zOffset = hexSize * 1.73f; // sqrt(3)/2

        height = height / 2;
        width = width * 2;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * xOffset;
                float zPos = z * zOffset + (x % 2 == 1 ? zOffset / 2f : 0f);
                Vector3 position = new Vector3(xPos, 0, zPos);

                // Középre tesszük a kastélyt
                if (x == width - 1 && z == height - 1)
                {
                    Instantiate(castleHexPrefab, position, Quaternion.identity, transform);
                }
                else
                {
                    Instantiate(groundHexPrefab, position, Quaternion.identity, transform);
                }
            }
        }
    }
}

//float xOffset = hexSize * 0.5f;
//float zOffset = hexSize * 1.73f; // sqrt(3)/2
