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
    private Dictionary<Vector2Int, GameObject> coordinateLabels = new Dictionary<Vector2Int, GameObject>();
    private float hexSize = 1f;
    private float xOffset;
    private float zOffset;
    private int mapWidth;
    private int mapHeight;

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
        // MapGenerator modifies width and height in GenerateMap(), so we need to account for that
        // After GenerateMap(): width = originalWidth * 2, height = originalHeight / 2
        // So we need: mapWidth = currentWidth, mapHeight = currentHeight
        mapWidth = mapGenerator.width;  // Already doubled in GenerateMap
        mapHeight = mapGenerator.height; // Already halved in GenerateMap

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
            if (useLineRenderers)
            {
                CreateLineRenderers();
            }
            CreateCoordinateLabels();
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
        
        // Track which connections we've already drawn to avoid duplicates
        HashSet<string> drawnConnections = new HashSet<string>();
        
        int lineIndex = 0;
        int topRowTilesProcessed = 0;
        int topRowNeighborsFound = 0;
        
        foreach (var kvp in hexTileMap)
        {
            Vector2Int hexCoord = kvp.Key;
            HexTile tile = kvp.Value;
            
            if (tile == null) continue;
            
            bool isTopRow = hexCoord.y == 1;
            if (isTopRow) topRowTilesProcessed++;

            Vector3 tilePos = tile.transform.position + Vector3.up * 0.2f;
            List<Vector2Int> neighbors = GetHexNeighbors(hexCoord);
            
            if (isTopRow)
            {
                Debug.Log($"Top row tile ({hexCoord.x}, {hexCoord.y}) at {tile.transform.position} has {neighbors.Count} neighbors");
                topRowNeighborsFound += neighbors.Count;
            }

            foreach (Vector2Int neighborCoord in neighbors)
            {
                // Create a unique key for this connection (smaller coord first to avoid duplicates)
                Vector2Int coord1 = hexCoord.x < neighborCoord.x || (hexCoord.x == neighborCoord.x && hexCoord.y < neighborCoord.y) 
                    ? hexCoord 
                    : neighborCoord;
                Vector2Int coord2 = coord1 == hexCoord ? neighborCoord : hexCoord;
                string connectionKey = $"{coord1.x},{coord1.y}-{coord2.x},{coord2.y}";
                
                // Skip if we've already drawn this connection
                if (drawnConnections.Contains(connectionKey))
                    continue;
                
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
                    
                    // Mark this connection as drawn
                    drawnConnections.Add(connectionKey);
                }
            }
        }
        
        Debug.Log($"Created {lineRenderers.Count} line renderers (unique connections). Top row: {topRowTilesProcessed} tiles processed, {topRowNeighborsFound} neighbors found");
    }

    void CreateCoordinateLabels()
    {
        // Clean up old labels
        foreach (GameObject label in coordinateLabels.Values)
        {
            if (label != null) Destroy(label);
        }
        coordinateLabels.Clear();

        if (hexTileMap.Count == 0) return;

        foreach (var kvp in hexTileMap)
        {
            Vector2Int hexCoord = kvp.Key;
            HexTile tile = kvp.Value;

            if (tile == null) continue;

            // Create a TextMesh for the coordinate label
            GameObject labelObj = new GameObject($"CoordLabel_{hexCoord.x}_{hexCoord.y}");
            labelObj.transform.SetParent(transform);
            labelObj.transform.position = tile.transform.position + Vector3.up * 0.5f;

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = $"({hexCoord.x},{hexCoord.y})";
            textMesh.fontSize = 20;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            textMesh.fontStyle = FontStyle.Bold;

            // Make it face the camera
            labelObj.transform.LookAt(Camera.main.transform);
            labelObj.transform.Rotate(0, 180, 0); // Flip to face camera

            coordinateLabels[hexCoord] = labelObj;
            labelObj.SetActive(showCoordinates);
        }

        Debug.Log($"Created {coordinateLabels.Count} coordinate labels");
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
            // Special case: Top row tiles (z=1) use same logic as bottom row
            if (z == 1)
            {
                // Connect downward to odd bottom row tiles: (x+1, z-1) and (x-1, z-1)
                if (x + 1 < mapWidth && z - 1 >= 0) neighbors.Add(new Vector2Int(x + 1, z - 1));  // Connect to odd tile below-right
                if (x - 1 >= 0 && z - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z - 1));       // Connect to odd tile below-left
                
                // Forward neighbors: (x+2, z) and (x-2, z) - same as bottom row
                if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));
                if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));
                
                // Side neighbors: (x+1, z) and (x-1, z) - same as bottom row
                if (x + 1 < mapWidth) neighbors.Add(new Vector2Int(x + 1, z));
                if (x - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z));
            }
            else if (z == 0)
            {
                // Special case: Even bottom row tiles (z=0) - NO upward diagonals, only horizontal
                // Forward neighbors: (x+2, z) and (x-2, z)
                if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));
                if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));
                
                // Side neighbors: (x+1, z) and (x-1, z)
                if (x + 1 < mapWidth) neighbors.Add(new Vector2Int(x + 1, z));
                if (x - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z));
                
                // NO upward diagonals - only odd bottom row tiles connect to top row
            }
            else
            {
                // Regular even column logic (not top or bottom row)
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
        }
        else // Odd column
        {
            // Special case: Odd-numbered bottom row tiles (x odd, z=0)
            // Pattern: Use top row neighbors (z+1) with x±1, plus forward and side neighbors
            if (z == 0)
            {
                // Top row neighbors: (x-1, z+1) and (x+1, z+1)
                if (x - 1 >= 0 && z + 1 < mapHeight) neighbors.Add(new Vector2Int(x - 1, z + 1));
                if (x + 1 < mapWidth && z + 1 < mapHeight) neighbors.Add(new Vector2Int(x + 1, z + 1));
                
                // Forward neighbors: (x+2, z) and (x-2, z)
                if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));
                if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));
                
                // Side neighbors: (x+1, z) and (x-1, z)
                if (x + 1 < mapWidth) neighbors.Add(new Vector2Int(x + 1, z));
                if (x - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z));
            }
            else if (z == 1)
            {
                // Special case: Odd top row tiles (z=1) - NO downward connections to bottom row
                // Only odd bottom row tiles connect upward, not odd top row tiles downward
                // Forward neighbors: (x+2, z) and (x-2, z)
                if (x + 2 < mapWidth) neighbors.Add(new Vector2Int(x + 2, z));
                if (x - 2 >= 0) neighbors.Add(new Vector2Int(x - 2, z));
                
                // Side neighbors: (x+1, z) and (x-1, z)
                if (x + 1 < mapWidth) neighbors.Add(new Vector2Int(x + 1, z));
                if (x - 1 >= 0) neighbors.Add(new Vector2Int(x - 1, z));
            }
            else
            {
                // Regular odd column logic (not bottom or top row)
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
            
            // Neighbors should be close (max distance ~2.7 for top-bottom row connections)
            // Top row to bottom row connections are about 2.64 units apart
            if (distance > hexSize * 3f)
            {
                if (hexCoord.y == 1 || neighbor.y == 1)
                {
                    Debug.LogWarning($"Visualizer: Skipping neighbor {hexCoord} -> {neighbor}, distance: {distance} (threshold: {hexSize * 3f})");
                }
                continue;
            }
            
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

            // Coordinate labels are now handled by TextMesh objects, not Gizmos
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
            
            // Update label visibility
            foreach (GameObject label in coordinateLabels.Values)
            {
                if (label != null)
                {
                    label.SetActive(showCoordinates);
                }
            }
        }
        
        // Refresh map if needed
        if (hexTileMap.Count == 0 && mapGenerator != null)
        {
            BuildHexMap();
            if (useLineRenderers && showNeighborConnections)
            {
                CreateLineRenderers();
            }
            CreateCoordinateLabels();
        }
        
        // Update line renderer visibility
        foreach (LineRenderer lr in lineRenderers)
        {
            if (lr != null)
            {
                lr.enabled = showNeighborConnections;
            }
        }

        // Update label positions to face camera
        if (showCoordinates && Camera.main != null)
        {
            foreach (var kvp in coordinateLabels)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.transform.LookAt(Camera.main.transform);
                    kvp.Value.transform.Rotate(0, 180, 0);
                }
            }
        }
    }
}
