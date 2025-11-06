using UnityEngine;

public class HexTile : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    public Color highlightColor = Color.cyan; // kék fény

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color;
        }
    }

    void OnMouseEnter()
    {
        if (rend != null)
            rend.material.color = highlightColor; // egér fölé
    }

    void OnMouseExit()
    {
        if (rend != null)
            rend.material.color = originalColor; // vissza eredeti
    }
}
