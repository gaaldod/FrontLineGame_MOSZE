using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake() => Instance = this;

    [Header("Unit Settings")]
    public GameObject unitPrefab;

    [Header("UI Elements")]
    public Button buyLeftButton;
    public Button buyRightButton;

    private bool isPlacingUnit = false;
    private GameObject ghostUnit;
    private int activePlayer = 0;

    public int ActivePlayer => activePlayer; // hozzáférés a HexTile-nek

    public Color leftPlayerColor = Color.cyan;
    public Color rightPlayerColor = Color.red;

    void Start()
    {
        buyLeftButton.onClick.AddListener(() => StartPlacingUnit(0));
        buyRightButton.onClick.AddListener(() => StartPlacingUnit(1));
    }

    void Update()
    {
        if (isPlacingUnit && ghostUnit != null)
            FollowMouse();
    }

    void StartPlacingUnit(int player)
    {
        if (isPlacingUnit) return;

        isPlacingUnit = true;
        activePlayer = player;

        ghostUnit = Instantiate(unitPrefab);
        SetTransparency(ghostUnit, 0.5f);

        // Ghost külön layerre
        ghostUnit.layer = LayerMask.NameToLayer("Ghost");
        foreach (Transform child in ghostUnit.transform)
            child.gameObject.layer = LayerMask.NameToLayer("Ghost");
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("LeftZone", "RightZone");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            Vector3 offset = Camera.main.transform.right * 0.5f + Vector3.up * 0.1f;
            Vector3 targetPos = hit.point + offset;
            ghostUnit.transform.position = Vector3.Lerp(ghostUnit.transform.position, targetPos, Time.deltaTime * 20f);
        }
    }

    public void TryPlaceUnit(HexTile tile)
    {
        if (!isPlacingUnit || tile.isOccupied) return;

        // 🏰 Ne lehessen a castle-ra rakni
        if (tile.CompareTag("Castle"))
        {
            Debug.Log("❌ A kastélyra nem lehet unitot rakni!");
            return;
        }

        // Zónaellenőrzés
        if (activePlayer == 0 && tile.gameObject.layer != LayerMask.NameToLayer("LeftZone"))
        {
            Debug.Log("❌ A bal játékos csak a bal zónába helyezhet unitot!");
            return;
        }

        if (activePlayer == 1 && tile.gameObject.layer != LayerMask.NameToLayer("RightZone"))
        {
            Debug.Log("❌ A jobb játékos csak a jobb zónába helyezhet unitot!");
            return;
        }

        tile.isOccupied = true;
        GameObject unit = Instantiate(unitPrefab, tile.transform.position + Vector3.up * 0.1f, Quaternion.identity);

        Destroy(ghostUnit);
        ghostUnit = null;
        isPlacingUnit = false;

        Debug.Log($"✅ Játékos {activePlayer + 1} unitot helyezett le a Tile-ra!");
    }

    void SetTransparency(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material m in r.materials)
            {
                Color c = m.color;
                c.a = alpha;
                m.color = c;
                m.SetFloat("_Mode", 3);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = 3000;
            }
        }
    }
}
