using UnityEngine;

public class Tile3D : MonoBehaviour
{
    public Vector2Int hexPosition;
    public bool occupied = false;

    void OnMouseDown()
    {
        Debug.Log($"Tile clicked at {hexPosition}");
        // késõbb ide jön majd az egység lehelyezés
    }
}
