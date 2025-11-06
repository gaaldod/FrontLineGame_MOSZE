using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake() => Instance = this;

    [Header("Prefabs & UI")]
    public GameObject unitPrefab;
    public Button buyLeftButton;
    public Button buyRightButton;
    public TMP_Text leftGoldText;
    public TMP_Text rightGoldText;

    [Header("Economy Settings")]
    public int startingGold = 15;
    public int unitCost = 5;

    [Header("Player Colors")]
    public Color leftPlayerColor = Color.cyan;
    public Color rightPlayerColor = Color.red;

    private int[] gold = new int[2]; // [0] = bal, [1] = jobb
    private bool isPlacingUnit = false;
    private GameObject ghostUnit;
    private int activePlayer = 0;

    public int ActivePlayer => activePlayer;

    void Start()
    {
        // Kezdő arany
        gold[0] = startingGold;
        gold[1] = startingGold;
        UpdateGoldUI();

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
        if (gold[player] < unitCost)
        {
            Debug.Log($"Játékos {player + 1} nem engedheti meg magának a unitot!");
            return;
        }

        isPlacingUnit = true;
        activePlayer = player;
        ghostUnit = Instantiate(unitPrefab);
        SetTransparency(ghostUnit, 0.5f);
    }

    void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // kicsit az egér mellé toljuk
            Vector3 offset = Camera.main.transform.right * 0.5f + Vector3.up * 0.1f;
            ghostUnit.transform.position = hit.point + offset;
        }
    }

    public void TryPlaceUnit(HexTile tile)
    {
        if (!isPlacingUnit || tile.isOccupied) return;
        if (tile.CompareTag("Castle")) return; // ne lehessen kastélyra rakni

        // Zóna ellenőrzés
        if (activePlayer == 0 && tile.gameObject.layer != LayerMask.NameToLayer("LeftZone"))
        {
            Debug.Log("A bal játékos csak a bal oldalon rakhat le unitot!");
            return;
        }

        if (activePlayer == 1 && tile.gameObject.layer != LayerMask.NameToLayer("RightZone"))
        {
            Debug.Log("A jobb játékos csak a jobb oldalon rakhat le unitot!");
            return;
        }

        // Gold levonás
        gold[activePlayer] -= unitCost;
        UpdateGoldUI();

        tile.isOccupied = true;

        Instantiate(unitPrefab, tile.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        Destroy(ghostUnit);
        ghostUnit = null;
        isPlacingUnit = false;

        Debug.Log($"Játékos {activePlayer + 1} unitot helyezett le! ({gold[activePlayer]} gold maradt)");
    }

    void UpdateGoldUI()
    {
        leftGoldText.text = $"Arany: {gold[0]}";
        rightGoldText.text = $"Arany: {gold[1]}";
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
