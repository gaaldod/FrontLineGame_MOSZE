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
        mapWidth = mapGenerator.width * 2;
        mapHeight = mapGenerator.height / 2;

        // Build hex coordinate map from all HexTiles
        HexTile[] allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        foreach (HexTile tile in allTiles)
        {
            Vector2Int hexCoord = WorldToHexCoord(tile.transform.position);
            
            // If coordinate already exists, keep the one closest to expected position
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

        Debug.Log($"BattleManager initialized: {hexTileMap.Count} tiles mapped");
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
    /// </summary>
    Vector2Int WorldToHexCoord(Vector3 worldPos)
    {
        // First estimate x coordinate
        int x = Mathf.RoundToInt(worldPos.x / xOffset);
        
        // Calculate z offset for this row
        float zOffsetForRow = (x % 2 == 1) ? zOffset / 2f : 0f;
        
        // Calculate z coordinate
        int z = Mathf.RoundToInt((worldPos.z - zOffsetForRow) / zOffset);
        
        // Clamp to valid range
        x = Mathf.Clamp(x, 0, mapWidth - 1);
        z = Mathf.Clamp(z, 0, mapHeight - 1);
        
        return new Vector2Int(x, z);
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
    /// Gets all 6 neighboring hex coordinates (odd-r offset system)
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

                // Check if tile is occupied by enemy unit
                bool occupiedByEnemy = false;
                Unit enemyUnit = null;
                foreach (var kvp in unitHexPositions)
                {
                    if (kvp.Value == neighbor && kvp.Key != null && unitOwners[kvp.Key] != owner)
                    {
                        occupiedByEnemy = true;
                        enemyUnit = kvp.Key;
                        break;
                    }
                }

                // Check if occupied by friendly unit
                bool occupiedByFriendly = false;
                foreach (var kvp in unitHexPositions)
                {
                    if (kvp.Value == neighbor && kvp.Key != null && unitOwners[kvp.Key] == owner)
                    {
                        occupiedByFriendly = true;
                        break;
                    }
                }

                // Can move to empty tiles or tiles with enemies (for combat)
                // Cannot move to tiles with friendly units
                if (!occupiedByFriendly && (!tile.isOccupied || occupiedByEnemy))
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

        Vector2Int currentHex = unitHexPositions[unit];
        int owner = unitOwners[unit];

        // Find target (enemy unit or enemy castle)
        Vector2Int? targetHex = FindTarget(unit, owner);

        if (!targetHex.HasValue)
        {
            Debug.Log($"Unit at {currentHex} has no target");
            return;
        }

        // Check if already adjacent to target (combat)
        List<Vector2Int> neighbors = GetHexNeighbors(currentHex);
        if (neighbors.Contains(targetHex.Value))
        {
            // Attack target
            Unit enemyUnit = GetUnitAtHex(targetHex.Value);
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
        Vector2Int currentHex = unitHexPositions[unit];
        Vector2Int? closestTarget = null;
        float closestDistance = float.MaxValue;

        // Find closest enemy unit (only alive ones)
        foreach (var kvp in unitHexPositions)
        {
            if (kvp.Key != null && kvp.Key.IsAlive() && unitOwners[kvp.Key] != owner && kvp.Key != unit)
            {
                float dist = HexDistance(currentHex, kvp.Value);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestTarget = kvp.Value;
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

        // Simple pathfinding: move to neighbor closest to target
        HexTile bestTile = null;
        float bestDistance = float.MaxValue;

        foreach (HexTile tile in neighbors)
        {
            Vector2Int tileHex = WorldToHexCoord(tile.transform.position);
            
            // Skip if occupied by friendly unit
            bool occupiedByFriendly = false;
            foreach (var kvp in unitHexPositions)
            {
                if (kvp.Value == tileHex && unitOwners[kvp.Key] == owner && kvp.Key != null)
                {
                    occupiedByFriendly = true;
                    break;
                }
            }
            if (occupiedByFriendly) continue;

            float dist = HexDistance(tileHex, to);
            if (dist < bestDistance)
            {
                bestDistance = dist;
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
        // Update tile occupancy
        if (hexTileMap.TryGetValue(fromHex, out HexTile fromTile))
            fromTile.isOccupied = false;
        
        if (hexTileMap.TryGetValue(toHex, out HexTile toTile))
            toTile.isOccupied = true;

        // Update unit position
        unitHexPositions[unit] = toHex;
        Vector3 targetWorldPos = HexCoordToWorld(toHex);
        unit.MoveTo(targetWorldPos);

        Debug.Log($"Unit moved from {fromHex} to {toHex}");
    }

    Unit GetUnitAtHex(Vector2Int hex)
    {
        foreach (var kvp in unitHexPositions)
        {
            if (kvp.Value == hex && kvp.Key != null)
                return kvp.Key;
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
