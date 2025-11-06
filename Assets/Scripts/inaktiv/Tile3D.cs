using System.Collections.Generic;
using UnityEngine;

public class Tile3D : MonoBehaviour
{
    // Logical position on the hex grid
    public Vector2Int hexPosition;

    // Who occupies this tile (null if empty)
    public Unit3D occupant;

    // Cached neighbor list (filled by MapGenerator3D)
    public List<Tile3D> neighbors;

    // Material colors for visual feedback (optional)
    private Renderer _renderer;
    private Color _defaultColor;
    public Color highlightColor = Color.yellow;
    public Color blockedColor = Color.red;

    void Awake()
    {
        // Cache the renderer for highlight changes
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
            _defaultColor = _renderer.material.color;
    }

    /// <summary>
    /// Returns true if the tile has a unit.
    /// </summary>
    public bool Occupied => occupant != null;

    void OnMouseEnter()
    {
        // Visual feedback: red if blocked, yellow if free
        if (_renderer != null)
            _renderer.material.color = Occupied ? blockedColor : highlightColor;
    }

    void OnMouseExit()
    {
        // Reset appearance
        ResetColor();
    }

    /// <summary>
    /// LEFT-CLICK behavior: attempt to place a unit on this tile.
    /// Later this can trigger selection, movement, attacks, etc.
    /// </summary>
    void OnMouseDown()
    {
        Debug.Log($"Tile clicked at {hexPosition}");

        if (Occupied)
        {
            Debug.Log("Tile is occupied — cannot place a unit here.");
            return;
        }

        // Simple prototype spawning (Editor ONLY demo!)
        // Later you should move this into a GameManager.
        TrySpawnTestUnit();
    }

    /// <summary>
    /// Sets the occupant of this tile and updates occupancy.
    /// </summary>
    public void SetOccupant(Unit3D unit)
    {
        occupant = unit;

        if (unit != null)
        {
            // Move the unit's GameObject onto tile center
            unit.transform.position = this.transform.position;
            unit.currentTile = this;
        }
    }

    /// <summary>
    /// Remove occupant cleanly (when moving or dying).
    /// </summary>
    public void ClearOccupant()
    {
        if (occupant != null)
        {
            occupant.currentTile = null;
            occupant = null;
        }
    }

    /// <summary>
    /// Called by the spawner logic to check if this tile can be used.
    /// </summary>
    public bool CanPlaceUnit()
    {
        return !Occupied;
    }

    /// <summary>
    /// Reset tile color to normal.
    /// </summary>
    public void ResetColor()
    {
        if (_renderer != null)
            _renderer.material.color = _defaultColor;
    }

    // ----------------------------------------------------------------------
    //        TEMPORARY DEMO UNIT SPAWNING (REMOVE LATER)
    // ----------------------------------------------------------------------
    public GameObject testUnitPrefab;

    private void TrySpawnTestUnit()
    {
        if (testUnitPrefab == null)
        {
            Debug.LogWarning("No testUnitPrefab assigned on Tile3D");
            return;
        }

        GameObject unitObj = Instantiate(testUnitPrefab, transform.position, Quaternion.identity);

        Unit3D unit = unitObj.GetComponent<Unit3D>();
        if (unit == null)
        {
            Debug.LogError("Unit prefab lacks Unit3D script!");
            Destroy(unitObj);
            return;
        }

        SetOccupant(unit);
        Debug.Log($"Unit spawned at tile {hexPosition}");
    }
}
