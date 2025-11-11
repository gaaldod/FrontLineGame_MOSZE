using UnityEngine;

public class HexTile : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    public bool isOccupied = false;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;
    }

    void OnMouseEnter()
    {
        if (rend == null || isOccupied || !GameManager.Instance) return;
        if (CompareTag("Castle")) return; // ne highlightolja a kastelyt

        int activePlayer = GameManager.Instance.ActivePlayer;

        if (activePlayer == 0)
        {
            rend.material.color = (gameObject.layer == LayerMask.NameToLayer("LeftZone"))
                ? GameManager.Instance.leftPlayerColor
                : GameManager.Instance.rightPlayerColor;
        }
        else
        {
            rend.material.color = (gameObject.layer == LayerMask.NameToLayer("RightZone"))
                ? GameManager.Instance.rightPlayerColor
                : GameManager.Instance.leftPlayerColor;
        }
    }

    void OnMouseExit()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }

    void OnMouseDown()
    {
        if (!isOccupied && GameManager.Instance != null)
        {
            GameManager.Instance.TryPlaceUnit(this);
        }
    }
}
