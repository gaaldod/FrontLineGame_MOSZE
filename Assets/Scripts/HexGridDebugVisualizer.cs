using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexGridDebugVisualizer : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showNeighborConnections = true;
    public bool showCoordinates = false;
    public Color connectionColor = Color.yellow;
    public Color invalidConnectionColor = Color.red;
    public bool useLineRenderers = true; // Use LineRenderer instead of Gizmos for better visibility
    
    private HexMap3D mapGenerator;
    private Dictionary<Vector2Int, HexTile> hexTileMap = new Dictionary<Vector2Int, HexTile>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private float hexSize = 1f;
    private float xOffset;
    private float zOffset;
    private int mapWidth;
    private int mapHeight;
    private bool mapInitialized = false;

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }
    
    IEnumerator InitializeWithDelay()
    {
        // Wait a frame to ensure map is generated
        yield return null;
        yield return null; // Wait another frame to be safe
        
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<HexMap3D>();

        if (mapGenerator == null)
        {
            Debug.LogError("HexGridDebugVisualizer: HexMap3D not found!");
            yield break;
        }

        hexSize = mapGenerator.hexSize;
        xOffset = hexSize * 0.5f;
        zOffset = hexSize * 1.73f;
        mapWidth = mapGenerator.width * 2;
        mapHeight = mapGenerator.height / 2;

        // Keep trying to build map until we find tiles
        int attempts = 0;
        while (hexTileMap.Count == 0 && attempts < 10)
        {
            BuildHexMap();
            if (hexTileMap.Count == 0)
            {
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        Debug.Log($"HexGridDebugVisualizer initialized. Map size: {mapWidth}x{mapHeight}, Tiles found: {hexTileMap.Count}");
        
        if (hexTileMap.Count > 0)
        {
            mapInitialized = true;
            if (useLineRenderers)
            {
                CreateLineRenderers();
            }
        }
        else
        {
            Debug.LogError("HexGridDebugVisualizer: No tiles found after multiple attempts!");
        }
    }
    
    void CreateLineRenderers()
    {
        // Clean up old line renderers
        foreach (LineRenderer lr in lineRenderers)
        {
            if (lr != null) Destroy(lr.gameObject);
        }
        lineRenderers.Clear();
        
        if (hexTileMap.Count == 0) return;
        
        int lineIndex = 0;
        foreach (var kvp in hexTileMap)
        {
            Vector2Int hexCoord = kvp.Key;
            HexTile tile = kvp.Value;
            
            if (tile == null) continue;

            Vector3 tilePos = tile.transform.position + Vector3.up * 0.2f;
            List<Vector2Int> neighbors = GetHexNeighbors(hexCoord);

            foreach (Vector2Int neighborCoord in neighbors)
            {
                bool isValid = neighborCoord.x >= 0 && neighborCoord.x < mapWidth &&
                              neighborCoord.y >= 0 && neighborCoord.y < mapHeight &&
                              hexTileMap.ContainsKey(neighborCoord);

                if (hexTileMap.TryGetValue(neighborCoord, out HexTile neighborTile))
                {
                    Vector3 neighborPos = neighborTile.transform.position + Vector3.up * 0.2f;
                    
                    GameObject lineObj = new GameObject($"Line_{lineIndex++}");
                    lineObj.transform.SetParent(transform);
                    LineRenderer lr = lineObj.AddComponent<LineRenderer>();
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    Color lineColor = isValid ? connectionColor : invalidConnectionColor;
                    lr.startColor = lineColor;
                    lr.endColor = lineColor;
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.positionCount = 2;
                    lr.useWorldSpace = true;
                    lr.SetPosition(0, tilePos);
                    lr.SetPosition(1, neighborPos);
                    lineRenderers.Add(lr);
                }
            }
        }
        
        Debug.Log($"Created {lineRenderers.Count} line renderers");
    }

    void BuildHexMap()
    {
        hexTileMap.Clear();
        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        Debug.Log($"HexGridDebugVisualizer: Found {allTiles.Length} HexTile components");
        
        foreach (HexTile tile in allTiles)
        {
            if (tile == null) continue;
            
            // Don't clamp during mapping - we want to see all tiles
            int x = Mathf.RoundToInt(tile.transform.position.x / xOffset);
            float zOffsetForRow = (x % 2 == 1) ? zOffset / 2f : 0f;
            int z = Mathf.RoundToInt((tile.transform.position.z - zOffsetForRow) / zOffset);
            Vector2Int hexCoord = new Vector2Int(x, z);
            
            if (hexTileMap.ContainsKey(hexCoord))
            {
                Vector3 expectedPos = HexCoordToWorld(hexCoord);
                float existingDist = Vector3.Distance(hexTileMap[hexCoord].transform.position, expectedPos);
                float newDist = Vector3.Distance(tile.transform.position, expectedPos);
                
                if (newDist < existingDist)
                    hexTileMap[hexCoord] = tile;
            }
            else
            {
                hexTileMap[hexCoord] = tile;
            }
        }

        Debug.Log($"HexGridDebugVisualizer: Mapped {hexTileMap.Count} tiles (from {allTiles.Length} found)");
    }

    Vector2Int WorldToHexCoord(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / xOffset);
        float zOffsetForRow = (x % 2 == 1) ? zOffset / 2f : 0f;
        int z = Mathf.RoundToInt((worldPos.z - zOffsetForRow) / zOffset);
        x = Mathf.Clamp(x, 0, mapWidth - 1);
        z = Mathf.Clamp(z, 0, mapHeight - 1);
        return new Vector2Int(x, z);
    }

    Vector3 HexCoordToWorld(Vector2Int hexCoord)
    {
        float xPos = hexCoord.x * xOffset;
        float zPos = hexCoord.y * zOffset + (hexCoord.x % 2 == 1 ? zOffset / 2f : 0f);
        return new Vector3(xPos, 0.15f, zPos);
    }

    List<Vector2Int> GetHexNeighbors(Vector2Int hexCoord)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int x = hexCoord.x;
        int z = hexCoord.y;

        if (x % 2 == 0) // Even column
        {
            // Forward diagonals: column n±2 (these are the "forward" neighbors)
            if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));     // Forward-right
            if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));          // Forward-left
            
            // Side diagonals: column n±1, same row
            if (x + 1 < mapWidth && z < mapHeight) neighbors.Add(new Vector2Int(x + 1, z));      // Right side
            if (x - 1 >= 0 && z < mapHeight) neighbors.Add(new Vector2Int(x - 1, z));             // Left side
            
            // Additional diagonals: column n±1, row z+1 (upward diagonals) - only if z+1 is valid
            if (x + 1 < mapWidth && z + 1 < mapHeight) neighbors.Add(new Vector2Int(x + 1, z + 1));  // Right-up diagonal
            if (x - 1 >= 0 && z + 1 < mapHeight) neighbors.Add(new Vector2Int(x - 1, z + 1));       // Left-up diagonal
        }
        else // Odd column
        {
            // Forward diagonals: column n±2 (these are the "forward" neighbors)
            if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));     // Forward-right
            if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));            // Forward-left
            
            // Side diagonals: column n±1, same row
            if (x + 1 < mapWidth && z < mapHeight) neighbors.Add(new Vector2Int(x + 1, z));      // Right side
            if (x - 1 >= 0 && z < mapHeight) neighbors.Add(new Vector2Int(x - 1, z));            // Left side
            
            // Additional diagonals: column n±1, row z-1 (downward diagonals) - only if z-1 is valid
            if (x + 1 < mapWidth && z - 1 >= 0) neighbors.Add(new Vector2Int(x + 1, z - 1));   // Right-down diagonal
            if (x - 1 >= 0 && z - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z - 1));          // Left-down diagonal
        }

        // Validate neighbors exist and are actually adjacent (not wrapping)
        List<Vector2Int> validNeighbors = new List<Vector2Int>();
        foreach (Vector2Int neighbor in neighbors)
        {
            // Check bounds
            if (neighbor.x < 0 || neighbor.x >= mapWidth || neighbor.y < 0 || neighbor.y >= mapHeight)
                continue;
            
            // Check if tile exists at this coordinate
            if (!hexTileMap.ContainsKey(neighbor))
                continue;
            
            // Validate world distance to prevent wrapping
            Vector3 currentPos = HexCoordToWorld(hexCoord);
            Vector3 neighborPos = HexCoordToWorld(neighbor);
            float distance = Vector3.Distance(currentPos, neighborPos);
            
            // Neighbors should be close (max distance ~1.5 * hexSize for diagonals)
            if (distance > hexSize * 2f)
                continue;
            
            validNeighbors.Add(neighbor);
        }

        return validNeighbors;
    }

    void OnDrawGizmos()
    {
        if (!showNeighborConnections)
            return;
            
        // Make sure map is built (for editor preview)
        if (hexTileMap.Count == 0 && Application.isPlaying)
        {
            BuildHexMap();
        }
        
        if (hexTileMap.Count == 0)
            return;

        int linesDrawn = 0;
        foreach (var kvp in hexTileMap)
        {
            Vector2Int hexCoord = kvp.Key;
            HexTile tile = kvp.Value;
            
            if (tile == null) continue;

            Vector3 tilePos = tile.transform.position + Vector3.up * 0.2f;
            List<Vector2Int> neighbors = GetHexNeighbors(hexCoord);

            foreach (Vector2Int neighborCoord in neighbors)
            {
                // Check if neighbor is valid
                bool isValid = neighborCoord.x >= 0 && neighborCoord.x < mapWidth &&
                              neighborCoord.y >= 0 && neighborCoord.y < mapHeight &&
                              hexTileMap.ContainsKey(neighborCoord);

                Color lineColor = isValid ? connectionColor : invalidConnectionColor;
                Gizmos.color = lineColor;

                if (hexTileMap.TryGetValue(neighborCoord, out HexTile neighborTile))
                {
                    Vector3 neighborPos = neighborTile.transform.position + Vector3.up * 0.2f;
                    Gizmos.DrawLine(tilePos, neighborPos);
                    linesDrawn++;
                }
                else if (isValid)
                {
                    // Neighbor should exist but doesn't - draw to expected position
                    Vector3 expectedPos = HexCoordToWorld(neighborCoord) + Vector3.up * 0.2f;
                    Gizmos.color = invalidConnectionColor;
                    Gizmos.DrawLine(tilePos, expectedPos);
                    linesDrawn++;
                }
            }

            // Draw coordinate label
            if (showCoordinates)
            {
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(tilePos + Vector3.up * 0.3f, $"({hexCoord.x},{hexCoord.y})");
                #endif
            }
        }
        
        // Debug output
        if (Application.isPlaying && Time.frameCount % 60 == 0) // Every 60 frames
        {
            Debug.Log($"HexGridDebugVisualizer: Drawing {linesDrawn} lines from {hexTileMap.Count} tiles");
        }
    }

    void Update()
    {
        // Toggle visualization with 'V' key
        if (Input.GetKeyDown(KeyCode.V))
        {
            showNeighborConnections = !showNeighborConnections;
            Debug.Log($"Hex grid visualization: {(showNeighborConnections ? "ON" : "OFF")}, Tiles mapped: {hexTileMap.Count}");
        }

        // Toggle coordinates with 'C' key
        if (Input.GetKeyDown(KeyCode.C))
        {
            showCoordinates = !showCoordinates;
            Debug.Log($"Hex coordinates display: {(showCoordinates ? "ON" : "OFF")}");
        }
        
        // Refresh map if needed
        if (hexTileMap.Count == 0 && mapGenerator != null)
        {
            BuildHexMap();
            if (useLineRenderers && showNeighborConnections)
            {
                CreateLineRenderers();
            }
        }
        
        // Update line renderer visibility
        foreach (LineRenderer lr in lineRenderers)
        {
            if (lr != null)
            {
                lr.enabled = showNeighborConnections;
            }
        }
    }
}
