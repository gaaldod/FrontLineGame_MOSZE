using System.Collections.Generic;
using UnityEngine;

public class MapGenerator3D : MonoBehaviour
{
    [Header("Hex Setup")]
    public GameObject hexPrefab;
    public int width = 8;
    public int height = 8; // currently 8x4 tiles per side

    // Hex dimensions (flat topped hexes)
    public float hexWidth = 1.0f;
    public float hexHeight = 0.866f; // sqrt(3)/2 for spacing

    // 2D array storing all generated tiles for fast lookup
    private Tile3D[,] grid;

    // Neighbor offsets for flat-topped hexes using row-based offset coordinates.
    // EVEN rows have different neighbor directions than ODD rows.
    // Order of directions: E, SE, SW, W, NW, NE

    // Offsets for EVEN rows
    private static readonly Vector2Int[] evenOffsets = new Vector2Int[]
    {
        new Vector2Int(+1,  0), // East
        new Vector2Int( 0, +1), // SouthEast
        new Vector2Int(-1, +1), // SouthWest
        new Vector2Int(-1,  0), // West
        new Vector2Int(-1, -1), // NorthWest
        new Vector2Int( 0, -1)  // NorthEast
    };

    // Offsets for ODD rows
    private static readonly Vector2Int[] oddOffsets = new Vector2Int[]
    {
        new Vector2Int(+1,  0), // East
        new Vector2Int(+1, +1), // SouthEast
        new Vector2Int( 0, +1), // SouthWest
        new Vector2Int(-1,  0), // West
        new Vector2Int( 0, -1), // NorthWest
        new Vector2Int(+1, -1)  // NorthEast
    };


    void Start()
    {
        GenerateMap();
    }


    /// <summary>
    /// Generates the hex grid, positions tiles correctly, stores them,
    /// and calculates neighbor references for movement/pathfinding.
    /// </summary>
    void GenerateMap()
    {
        // Allocate storage for tile references
        grid = new Tile3D[width, height];

        // Instantiate all hex tiles in proper hexagonal spacing
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Horizontal offset applied on odd rows for correct hex staggering
                float xOffset = (y % 2 == 0) ? 0 : hexWidth / 2f;

                float xPos = x * hexWidth + xOffset;
                float zPos = y * hexHeight;

                // Spawn hex tile
                GameObject hex = Instantiate(hexPrefab, new Vector3(xPos, 0, zPos), Quaternion.identity);
                hex.transform.parent = this.transform;
                hex.name = $"Hex_{x}_{y}";

                // Store tile coordinate info
                Tile3D tile = hex.GetComponent<Tile3D>();
                tile.hexPosition = new Vector2Int(x, y);

                // Save reference to tile in grid
                grid[x, y] = tile;
            }
        }

        // After generation, assign neighbors to every tile
        AssignAllNeighbors();
    }


    /// <summary>
    /// Finds all valid neighbor tiles of a given tile,
    /// respecting map boundaries and odd/even row offset patterns.
    /// </summary>
    public List<Tile3D> GetNeighbors(Tile3D tile)
    {
        List<Tile3D> neighbors = new List<Tile3D>();

        // Check if this row is odd or even
        bool isOdd = tile.hexPosition.y % 2 != 0;
        var offsets = isOdd ? oddOffsets : evenOffsets;

        // Apply each offset
        foreach (var o in offsets)
        {
            int nx = tile.hexPosition.x + o.x;
            int ny = tile.hexPosition.y + o.y;

            // Check boundaries to avoid out-of-map errors
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                neighbors.Add(grid[nx, ny]);
            }
        }

        return neighbors;
    }


    /// <summary>
    /// Loops through the entire grid and assigns a list of neighbors
    /// to each tile's 'neighbors' field.
    /// This allows movement systems to query tiles instantly.
    /// </summary>
    void AssignAllNeighbors()
    {
        foreach (Tile3D tile in grid)
        {
            tile.neighbors = GetNeighbors(tile);
        }
    }
}
