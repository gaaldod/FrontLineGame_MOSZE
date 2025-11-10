using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;
    private void Awake() => Instance = this;

    [Header("Battle Settings")]
    public float turnDuration = 1f; // Time between each turn
    public float unitMoveSpeed = 5f;
    public int maxTurns = 100; // Prevent infinite battles

    [Header("Map Reference")]
    public HexMap3D mapGenerator;

    private Dictionary<Vector2Int, HexTile> hexTileMap = new Dictionary<Vector2Int, HexTile>();
    private Dictionary<Unit, Vector2Int> unitHexPositions = new Dictionary<Unit, Vector2Int>();
    private Dictionary<Unit, int> unitOwners = new Dictionary<Unit, int>(); // 0 = left, 1 = right
    private List<Unit> allUnits = new List<Unit>();
    private bool battleInProgress = false;
    private int currentTurn = 0;

    // Hex map parameters (matching MapGenerator3D)
    private float hexSize = 1f;
    private float xOffset;
    private float zOffset;
    private int mapWidth;
    private int mapHeight;

    void Start()
    {
        InitializeHexMap();
        FindAllUnits();
        Debug.Log("BattleManager Start() completed");
    }

    void InitializeHexMap()
    {
        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<HexMap3D>();

        if (mapGenerator == null)
        {
            Debug.LogError("BattleManager: HexMap3D not found!");
            return;
        }

        hexSize = mapGenerator.hexSize;
        xOffset = hexSize * 0.5f;
        zOffset = hexSize * 1.73f;
        // MapGenerator modifies width and height in GenerateMap(), so we need to account for that
        // After GenerateMap(): width = originalWidth * 2, height = originalHeight / 2
        // So we need: mapWidth = currentWidth, mapHeight = currentHeight
        mapWidth = mapGenerator.width;  // Already doubled in GenerateMap
        mapHeight = mapGenerator.height; // Already halved in GenerateMap

        // Build hex coordinate map from all HexTiles
        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        Debug.Log($"BattleManager: Found {allTiles.Length} HexTile components");
        
        foreach (HexTile tile in allTiles)
        {
            if (tile == null) continue;
            
            Vector2Int hexCoord = WorldToHexCoord(tile.transform.position);
            
            // Verify the coordinate is correct by checking distance
            Vector3 expectedPos = HexCoordToWorld(hexCoord);
            float distance = Vector3.Distance(tile.transform.position, expectedPos);
            
            // If distance is too large, this tile might be mapped incorrectly
            if (distance > hexSize * 0.5f)
            {
                Debug.LogWarning($"Tile at {tile.transform.position} mapped to {hexCoord} but expected pos is {expectedPos}, distance: {distance}");
            }
            
            // If coordinate already exists, keep the one closest to expected position
            if (hexTileMap.ContainsKey(hexCoord))
            {
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

        Debug.Log($"BattleManager initialized: {hexTileMap.Count} tiles mapped (expected: {mapWidth * mapHeight})");
    }

    void FindAllUnits()
    {
        allUnits.Clear();
        unitHexPositions.Clear();
        unitOwners.Clear();

        Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
        foreach (Unit unit in units)
        {
            if (unit == null || !unit.IsAlive()) continue;
            
            allUnits.Add(unit);
            Vector2Int hexPos = WorldToHexCoord(unit.transform.position);
            unitHexPositions[unit] = hexPos;

            // Determine owner based on position and find closest tile
            int owner = DetermineUnitOwner(unit, hexPos);
            unitOwners[unit] = owner;
        }

        Debug.Log($"Found {allUnits.Count} units: Left={unitOwners.Values.Count(o => o == 0)}, Right={unitOwners.Values.Count(o => o == 1)}");
    }

    int DetermineUnitOwner(Unit unit, Vector2Int hexPos)
    {
        // Check if unit is on left or right side of map
        // Also check the tile's layer if available
        if (hexTileMap.TryGetValue(hexPos, out HexTile tile))
        {
            int leftLayer = LayerMask.NameToLayer("LeftZone");
            int rightLayer = LayerMask.NameToLayer("RightZone");
            
            if (tile.gameObject.layer == leftLayer)
                return 0;
            if (tile.gameObject.layer == rightLayer)
                return 1;
        }
        
        // Fallback: use position
        return (hexPos.x < mapWidth / 2) ? 0 : 1;
    }

    /// <summary>
    /// Converts world position to hex coordinates (odd-r offset)
    /// Uses reverse of MapGenerator3D's formula: xPos = x * xOffset, zPos = z * zOffset + (x % 2 == 1 ? zOffset / 2f : 0f)
    /// Improved: Tries all nearby x values and finds the best match
    /// </summary>
    Vector2Int WorldToHexCoord(Vector3 worldPos)
    {
        // Try all x values within reasonable range to find the best match
        Vector2Int bestCoord = new Vector2Int(0, 0);
        float bestDistance = float.MaxValue;
        
        // Estimate x range to check (worldPos.x / xOffset ± 2)
        int xCenter = Mathf.RoundToInt(worldPos.x / xOffset);
        int xStart = Mathf.Max(0, xCenter - 2);
        int xEnd = Mathf.Min(mapWidth - 1, xCenter + 2);
        
        for (int xTry = xStart; xTry <= xEnd; xTry++)
        {
            // Calculate z offset for this column
            float zOffsetForRow = (xTry % 2 == 1) ? zOffset / 2f : 0f;
            
            // Calculate z coordinate
            int zTry = Mathf.RoundToInt((worldPos.z - zOffsetForRow) / zOffset);
            zTry = Mathf.Clamp(zTry, 0, mapHeight - 1);
            
            Vector2Int coordTry = new Vector2Int(xTry, zTry);
            Vector3 expectedPosTry = HexCoordToWorld(coordTry);
            float distTry = Vector3.Distance(worldPos, expectedPosTry);
            
            if (distTry < bestDistance)
            {
                bestDistance = distTry;
                bestCoord = coordTry;
            }
        }
        
        return bestCoord;
    }

    /// <summary>
    /// Converts hex coordinates to world position (odd-r offset)
    /// </summary>
    Vector3 HexCoordToWorld(Vector2Int hexCoord)
    {
        float xPos = hexCoord.x * xOffset;
        float zPos = hexCoord.y * zOffset + (hexCoord.x % 2 == 1 ? zOffset / 2f : 0f);
        return new Vector3(xPos, 0.15f, zPos);
    }

    /// <summary>
    /// Gets all neighboring hex coordinates (odd-r offset system)
    /// Based on actual hex grid structure:
    /// - Even columns align vertically, odd columns are offset by zOffset/2
    /// - Forward movement is column n±2 (skipping one column)
    /// - Side diagonals are column n±1
    /// - Same column tiles are NOT neighbors (no vertical connections)
    /// - Validates that neighbors exist and are correctly positioned
    /// </summary>
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

        // Validate neighbors exist in hexTileMap and are actually neighbors (not wrapping around)
        List<Vector2Int> validNeighbors = new List<Vector2Int>();
        foreach (Vector2Int neighbor in neighbors)
        {
            // Check bounds
            if (neighbor.x < 0 || neighbor.x >= mapWidth || neighbor.y < 0 || neighbor.y >= mapHeight)
                continue;
            
            // Check if tile exists at this coordinate
            if (!hexTileMap.ContainsKey(neighbor))
                continue;
            
            // Additional validation: check that the neighbor is actually adjacent (not wrapping)
            // Calculate expected world distance - should be approximately hexSize
            Vector3 currentPos = HexCoordToWorld(hexCoord);
            Vector3 neighborPos = HexCoordToWorld(neighbor);
            float distance = Vector3.Distance(currentPos, neighborPos);
            
            // Neighbors should be close (within reasonable hex distance, accounting for offset)
            // Max distance should be around 1.5 * hexSize for diagonal neighbors
            if (distance > hexSize * 2f)
            {
                Debug.LogWarning($"Skipping invalid neighbor: {hexCoord} -> {neighbor}, distance: {distance}");
                continue;
            }
            
            validNeighbors.Add(neighbor);
        }

        return validNeighbors;
    }

    /// <summary>
    /// Gets valid neighboring tiles (within map bounds and not occupied)
    /// Checks actual world positions of units, not just hex position dictionary
    /// </summary>
    List<HexTile> GetValidNeighborTiles(Vector2Int hexCoord, int owner)
    {
        List<HexTile> validTiles = new List<HexTile>();
        List<Vector2Int> neighbors = GetHexNeighbors(hexCoord);

        foreach (Vector2Int neighbor in neighbors)
        {
            // Check bounds
            if (neighbor.x < 0 || neighbor.x >= mapWidth || neighbor.y < 0 || neighbor.y >= mapHeight)
                continue;

            if (hexTileMap.TryGetValue(neighbor, out HexTile tile))
            {
                // Skip castle tiles (can't move onto castle, only attack)
                if (tile.CompareTag("Castle"))
                    continue;

                // Check if tile is occupied by checking actual world positions of units
                bool occupiedByUnit = false;
                
                foreach (Unit unit in allUnits)
                {
                    if (unit == null || !unit.IsAlive())
                        continue;
                    
                    // Check actual world position, not just hex position dictionary
                    Vector2Int unitActualHex = WorldToHexCoord(unit.transform.position);
                    
                    if (unitActualHex == neighbor)
                    {
                        if (unitOwners[unit] != null)
                        {
                            occupiedByUnit = true;
                            break; // Can't move here if friendly unit is there
                        }
                    }
                }

                // Can move to empty tiles (for combat)
                // Cannot move to tiles with units
                if (!occupiedByUnit)
                {
                    validTiles.Add(tile);
                }
            }
        }

        return validTiles;
    }

    /// <summary>
    /// Starts the battle simulation
    /// </summary>
    public void StartBattle()
    {
        Debug.Log("StartBattle() called");
        
        if (battleInProgress)
        {
            Debug.LogWarning("Battle already in progress!");
            return;
        }

        // Make sure map is initialized
        if (hexTileMap.Count == 0)
        {
            Debug.LogWarning("Hex map not initialized, initializing now...");
            InitializeHexMap();
        }

        FindAllUnits(); // Refresh unit positions
        
        if (allUnits.Count == 0)
        {
            Debug.LogWarning("No units found! Place some units before starting the battle.");
            return;
        }
        
        battleInProgress = true;
        currentTurn = 0;
        StartCoroutine(BattleLoop());
    }

    /// <summary>
    /// Main battle loop - simulates turns
    /// </summary>
    IEnumerator BattleLoop()
    {
        Debug.Log("Battle Started!");

        while (battleInProgress && currentTurn < maxTurns)
        {
            currentTurn++;
            Debug.Log($"\n--- Turn {currentTurn} ---");

            // Check win conditions
            int winner = CheckWinCondition();
            if (winner >= 0)
            {
                Debug.Log($"Player {winner + 1} wins!");
                EndBattle(winner);
                yield break;
            }

            // Process all units (filter out dead ones)
            List<Unit> unitsToProcess = new List<Unit>(allUnits);
            foreach (Unit unit in unitsToProcess)
            {
                if (unit == null || !unit.IsAlive()) 
                {
                    // Clean up dead units
                    if (unit != null && !unit.IsAlive())
                        DestroyUnit(unit);
                    continue;
                }

                ProcessUnitTurn(unit);
            }

            yield return new WaitForSeconds(turnDuration);
        }

        if (currentTurn >= maxTurns)
        {
            Debug.Log("Battle timeout - Draw!");
            EndBattle(-1); // Draw
        }
    }

    void ProcessUnitTurn(Unit unit)
    {
        if (unit == null || !unit.IsAlive())
            return;
            
        if (!unitHexPositions.ContainsKey(unit))
            return;

        // Update unit's hex position based on actual world position (in case it moved)
        Vector2Int currentHex = WorldToHexCoord(unit.transform.position);
        unitHexPositions[unit] = currentHex;
        
        int owner = unitOwners[unit];

        // Find target (enemy unit or enemy castle)
        Vector2Int? targetHex = FindTarget(unit, owner);

        if (!targetHex.HasValue)
        {
            Debug.Log($"Unit at {currentHex} has no target");
            return;
        }

        // Check if already adjacent to target (combat)
        // Use actual positions of enemy units, not just hex coordinates
        List<Vector2Int> neighbors = GetHexNeighbors(currentHex);
        bool isAdjacentToTarget = neighbors.Contains(targetHex.Value);
        
        // Also check if there's an enemy unit actually at a neighbor position
        Unit enemyUnitAtNeighbor = null;
        foreach (Vector2Int neighbor in neighbors)
        {
            Unit enemyUnit = GetUnitAtHex(neighbor);
            if (enemyUnit != null && enemyUnit.IsAlive() && unitOwners[enemyUnit] != owner)
            {
                // Verify unit is actually at this position (not just moving there)
                Vector2Int enemyActualHex = WorldToHexCoord(enemyUnit.transform.position);
                if (enemyActualHex == neighbor)
                {
                    enemyUnitAtNeighbor = enemyUnit;
                    isAdjacentToTarget = true;
                    break;
                }
            }
        }
        
        if (isAdjacentToTarget)
        {
            // Attack target
            Unit enemyUnit = enemyUnitAtNeighbor ?? GetUnitAtHex(targetHex.Value);
            if (enemyUnit != null && enemyUnit.IsAlive())
            {
                AttackUnit(unit, enemyUnit);
            }
            else
            {
                // Attack castle
                AttackCastle(unit, owner);
            }
            return;
        }

        // Move toward target
        Vector2Int? nextHex = FindPathToTarget(currentHex, targetHex.Value, owner);
        if (nextHex.HasValue)
        {
            MoveUnit(unit, currentHex, nextHex.Value);
        }
    }

    Vector2Int? FindTarget(Unit unit, int owner)
    {
        // Use actual world position, not just hex position dictionary
        Vector2Int currentHex = WorldToHexCoord(unit.transform.position);
        Vector2Int? closestTarget = null;
        float closestDistance = float.MaxValue;

        // Find closest enemy unit (only alive ones) - check actual positions
        foreach (Unit enemyUnit in allUnits)
        {
            if (enemyUnit == null || enemyUnit == unit || !enemyUnit.IsAlive())
                continue;
            
            if (unitOwners[enemyUnit] != owner)
            {
                Vector2Int enemyHex = WorldToHexCoord(enemyUnit.transform.position);
                float dist = HexDistance(currentHex, enemyHex);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = enemyHex;
                }
            }
        }

        // If no enemy units, target enemy castle
        if (!closestTarget.HasValue)
        {
            Vector2Int castleHex = new Vector2Int(mapWidth - 1, mapHeight - 1);
            if (owner == 0) // Left player targets right castle
            {
                closestTarget = castleHex;
            }
            else // Right player targets left side (or we could add a left castle)
            {
                // For now, right player moves toward center/left
                closestTarget = new Vector2Int(0, mapHeight - 1);
            }
        }

        return closestTarget;
    }

    Vector2Int? FindPathToTarget(Vector2Int from, Vector2Int to, int owner)
    {
        List<HexTile> neighbors = GetValidNeighborTiles(from, owner);
        
        if (neighbors.Count == 0)
            return null;

        // Improved pathfinding: prioritize moves that help reach the target row
        HexTile bestTile = null;
        float bestScore = float.MaxValue;

        foreach (HexTile tile in neighbors)
        {
            Vector2Int tileHex = WorldToHexCoord(tile.transform.position);
            
            // Double-check: Skip if occupied by another unit (check actual positions)
            bool occupiedByUnit = false;
            foreach (Unit unit in allUnits)
            {
                if (unit == null || !unit.IsAlive())
                    continue;
                
                Vector2Int unitActualHex = WorldToHexCoord(unit.transform.position);
                if (unitActualHex == tileHex && unitOwners[unit] != null)
                {
                    occupiedByUnit = true;
                    break;
                }
            }
            if (occupiedByUnit) continue;

            // Calculate distance to target
            float dist = HexDistance(tileHex, to);
            
            // Special case: If unit is in bottom row (z=0) and target is in top row (z=1)
            // Prioritize moving to odd columns (which can move up)
            if (from.y == 0 && to.y == 1)
            {
                // If this neighbor is an odd column, give it a bonus (reduce score)
                if (tileHex.x % 2 == 1)
                {
                    dist -= 0.5f; // Prefer odd columns
                }
                // If this neighbor is still in bottom row and even column, penalize it
                else if (tileHex.y == 0 && tileHex.x % 2 == 0)
                {
                    dist += 1.0f; // Penalize staying in even bottom row
                }
            }
            
            // Special case: If unit is in top row (z=1) and target is in bottom row (z=0)
            // Prioritize moving to even columns (which can move down)
            if (from.y == 1 && to.y == 0)
            {
                // If this neighbor is an even column in top row, give it a bonus
                if (tileHex.x % 2 == 0 && tileHex.y == 1)
                {
                    dist -= 0.5f; // Prefer even columns in top row
                }
                // If this neighbor is still in top row and odd column, penalize it slightly
                else if (tileHex.y == 1 && tileHex.x % 2 == 1)
                {
                    dist += 0.5f; // Slight penalty for odd columns in top row
                }
            }
            
            // Prefer moves that reduce row distance
            int rowDiffFrom = Mathf.Abs(from.y - to.y);
            int rowDiffTo = Mathf.Abs(tileHex.y - to.y);
            if (rowDiffTo < rowDiffFrom)
            {
                dist -= 0.3f; // Bonus for reducing row distance
            }
            else if (rowDiffTo > rowDiffFrom)
            {
                dist += 0.3f; // Penalty for increasing row distance
            }

            if (dist < bestScore)
            {
                bestScore = dist;
                bestTile = tile;
            }
        }

        return bestTile != null ? WorldToHexCoord(bestTile.transform.position) : null;
    }

    float HexDistance(Vector2Int a, Vector2Int b)
    {
        // Convert odd-r offset to cube coordinates for distance calculation
        int ax = a.x;
        int az = a.x % 2 == 0 ? a.y : a.y - (a.x - 1) / 2;
        int ay = -ax - az;

        int bx = b.x;
        int bz = b.x % 2 == 0 ? b.y : b.y - (b.x - 1) / 2;
        int by = -bx - bz;

        return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2f;
    }

    void MoveUnit(Unit unit, Vector2Int fromHex, Vector2Int toHex)
    {
        // Check if target tile is actually free (double-check with actual positions)
        bool tileIsFree = true;
        foreach (Unit otherUnit in allUnits)
        {
            if (otherUnit == null || otherUnit == unit || !otherUnit.IsAlive())
                continue;
            
            Vector2Int otherUnitHex = WorldToHexCoord(otherUnit.transform.position);
            if (otherUnitHex == toHex)
            {
                tileIsFree = false;
                Debug.LogWarning($"Cannot move unit to {toHex} - tile is occupied by another unit");
                break;
            }
        }
        
        if (!tileIsFree)
            return; // Don't move if tile is occupied
        
        // Update tile occupancy
        if (hexTileMap.TryGetValue(fromHex, out HexTile fromTile))
            fromTile.isOccupied = false;
        
        if (hexTileMap.TryGetValue(toHex, out HexTile toTile))
            toTile.isOccupied = true;

        // Update unit position dictionary (will be verified next turn based on actual position)
        unitHexPositions[unit] = toHex;
        Vector3 targetWorldPos = HexCoordToWorld(toHex);
        unit.MoveTo(targetWorldPos);

        Debug.Log($"Unit moved from {fromHex} to {toHex}");
    }

    Unit GetUnitAtHex(Vector2Int hex)
    {
        // Check actual world positions, not just hex position dictionary
        foreach (Unit unit in allUnits)
        {
            if (unit == null || !unit.IsAlive())
                continue;
            
            Vector2Int unitActualHex = WorldToHexCoord(unit.transform.position);
            if (unitActualHex == hex)
                return unit;
        }
        return null;
    }

    void AttackUnit(Unit attacker, Unit defender)
    {
        if (attacker == null || defender == null || !defender.IsAlive())
            return;

        int damage = attacker.attackDamage;
        Debug.Log($"Unit at {unitHexPositions[attacker]} attacks unit at {unitHexPositions[defender]} for {damage} damage");
        
        defender.TakeDamage(damage);
        
        // If defender died, clean it up
        if (!defender.IsAlive())
        {
            DestroyUnit(defender);
        }
    }

    void AttackCastle(Unit attacker, int owner)
    {
        Debug.Log($"Unit at {unitHexPositions[attacker]} attacks castle!");
        
        // Castle destroyed - attacker's team wins
        int winner = owner;
        EndBattle(winner);
    }

    void DestroyUnit(Unit unit)
    {
        if (unit == null) return;
        if (!unitHexPositions.ContainsKey(unit))
            return;

        Vector2Int hex = unitHexPositions[unit];
        if (hexTileMap.TryGetValue(hex, out HexTile tile))
            tile.isOccupied = false;

        unitHexPositions.Remove(unit);
        unitOwners.Remove(unit);
        allUnits.Remove(unit);

        // Clean up healthbar and unit
        unit.Die();
        Destroy(unit.gameObject);

        Debug.Log($"Unit destroyed at {hex}");
    }

    int CheckWinCondition()
    {
        // Check if one team has no alive units left
        bool leftHasUnits = false;
        bool rightHasUnits = false;
        
        foreach (var kvp in unitOwners)
        {
            if (kvp.Key != null && kvp.Key.IsAlive())
            {
                if (kvp.Value == 0)
                    leftHasUnits = true;
                else if (kvp.Value == 1)
                    rightHasUnits = true;
            }
        }

        if (!leftHasUnits && rightHasUnits)
            return 1; // Right wins
        if (!rightHasUnits && leftHasUnits)
            return 0; // Left wins

        return -1; // No winner yet
    }

    void EndBattle(int winner)
    {
        battleInProgress = false;
        StopAllCoroutines();

        if (GameManager.Instance != null && winner >= 0)
        {
            GameManager.Instance.EndGame(winner);
        }
    }

    // Public method to manually trigger battle
    public void TriggerBattle()
    {
        if (!battleInProgress)
            StartBattle();
	}
}
