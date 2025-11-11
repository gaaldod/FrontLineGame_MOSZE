using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class WorldHexTile : MonoBehaviour
{
    private Renderer rend;
    private Color originalColor;
    private Vector3 originalPosition;

    private Coroutine pulseCoroutine;
    private bool isRaised = false;

    [Header("Hex Tile adatok")]
    public int hexX;
    public int hexZ;
    public bool isOccupied = false;
    public bool isCastleTile = false; // új mező — ezzel jelöljük a vár mezőt az Inspectorban

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
            originalColor = rend.material.color;

        originalPosition = transform.position;
    }

    void OnMouseDown()
    {
        if (WorldManager.Instance == null) return;
        if (WorldManager.Instance.gameObject == null) return;

        // Ha már Game Over van, ne engedje új csata indítását
        var manager = WorldManager.Instance;
        if (manager == null) return;

        // csak akkor reagálunk, ha ez kattintható tile
        if (manager.IsTileClickable(this))
        {
            Debug.Log($"Kattintottál egy ÉRVÉNYES tile-ra: {hexX},{hexZ}");

            // ha ez egy vár tile, akkor automatikus győzelem következhet
            if (isCastleTile)
            {
                Debug.Log("Kastély tile-ra kattintottál – automatikus Game Over logika aktiválódhat.");
            }

            // jelezzük a WorldManagernek, melyik tile-ról indul a csata
            manager.RecordBattleStart(hexX, hexZ);

            // betöltjük a csata jelenetet
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.Log($"Ez a tile nem kattintható ({hexX},{hexZ})");
        }
    }

    void OnMouseEnter()
    {
        if (rend == null || WorldManager.Instance == null) return;

        bool isClickable = WorldManager.Instance.IsTileClickable(this);
        int layer = gameObject.layer;

        if (isClickable)
        {
            rend.material.color = Color.yellow;
            RaiseTile(true);
        }
        else
        {
            // nem kattintható, de zóna színt kap
            if (layer == LayerMask.NameToLayer("LeftZone"))
                rend.material.color = WorldManager.Instance.leftPlayerColor;
            else if (layer == LayerMask.NameToLayer("RightZone"))
                rend.material.color = WorldManager.Instance.rightPlayerColor;
        }
    }

    void OnMouseExit()
    {
        if (rend == null || WorldManager.Instance == null) return;

        ResetColor();

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        RaiseTile(false);
    }

    public void ResetColor()
    {
        if (rend != null)
            rend.material.color = originalColor;
    }

    void RaiseTile(bool raise)
    {
        if (raise && !isRaised)
        {
            transform.position = originalPosition + Vector3.up * 0.2f;
            isRaised = true;
            pulseCoroutine = StartCoroutine(PulseUpDown());
        }
        else if (!raise && isRaised)
        {
            transform.position = originalPosition;
            isRaised = false;
        }
    }

    IEnumerator PulseUpDown()
    {
        float speed = 2f;
        float amplitude = 0.05f;

        while (true)
        {
            float newY = originalPosition.y + 0.2f + Mathf.Sin(Time.time * speed) * amplitude;
            transform.position = new Vector3(originalPosition.x, newY, originalPosition.z);
            yield return null;
        }
    }
}
