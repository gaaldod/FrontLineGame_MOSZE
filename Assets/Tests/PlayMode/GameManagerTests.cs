using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;
using TMPro;

public class GameManagerTests
{
    private GameObject gmObj;
    private GameManager gm;
    private GameObject tileObj;
    private HexTile tile;
    private GameObject wmObj;
    private WorldManager wm;

    [SetUp]
    public void Setup()
    {
        // WorldManager létrehozása és Instance beállítása
        wmObj = new GameObject("WorldManager");
        wm = wmObj.AddComponent<WorldManager>();
        WorldManager.Instance = wm;
        wm.SetGold(new int[] { 10, 10 }); // Arany beállítása Start előtt

        // GameManager létrehozása és Instance beállítása
        gmObj = new GameObject("GameManager");
        gm = gmObj.AddComponent<GameManager>();
        GameManager.Instance = gm;
        gm.unitPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gm.startingGold = 10;
        gm.unitCost = 5;

        // Dummy UI létrehozása
        var leftGO = new GameObject("LeftGold");
        gm.leftGoldText = leftGO.AddComponent<TextMeshProUGUI>();
        var rightGO = new GameObject("RightGold");
        gm.rightGoldText = rightGO.AddComponent<TextMeshProUGUI>();

        // Privát Start() meghívása, hogy a GameManager inicializálódjon
        var startMethod = typeof(GameManager).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
        startMethod.Invoke(gm, null);

        // Manuálisan állítsuk be a GameManager arany tömbjét a teszthez
        typeof(GameManager).GetField("gold", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gm, new int[] { 10, 10 });

        // Aktív játékos
        typeof(GameManager).GetField("activePlayer", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gm, 0);

        // Tile létrehozása
        tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile = tileObj.AddComponent<HexTile>();
        tile.gameObject.layer = LayerMask.NameToLayer("LeftZone");
        tile.isOccupied = false;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gmObj);
        Object.DestroyImmediate(tileObj);
        Object.DestroyImmediate(wmObj);
        WorldManager.Instance = null;
        GameManager.Instance = null;
    }

    [UnityTest]
    public IEnumerator EndGame_UpdatesGoldAndWorldManager()
    {
        int[] oldGold = (int[])WorldManager.Instance.GetGold().Clone();
        gm.EndGame(0);
        yield return null;

        int[] newGold = WorldManager.Instance.GetGold();
        Assert.AreEqual(oldGold[0] + 5, newGold[0], "Győztes arany nem frissült.");
    }

    [UnityTest]
    public IEnumerator TryPlaceUnit_UpdatesGoldAndOccupancy()
    {
        // Biztosítjuk, hogy legyen elég arany
        typeof(GameManager).GetField("gold", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gm, new int[] { 10, 10 });

        // Private StartPlacingUnit meghívása reflectionnel
        var method = typeof(GameManager).GetMethod("StartPlacingUnit", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(gm, new object[] { 0 });

        yield return null;

        // Unit lerakása
        gm.TryPlaceUnit(tile);

        // Arany ellenőrzése a GameManager privát gold tömbjén
        int[] gold = (int[])typeof(GameManager)
            .GetField("gold", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(gm);

        Assert.AreEqual(5, gold[0], "Arany nem csökkent a unit lerakás után.");
        Assert.IsTrue(tile.isOccupied, "Tile nem lett foglalt.");
    }

    [UnityTest]
    public IEnumerator StartPlacingUnit_NotEnoughGold_NoPlacement()
    {
        // 0 arany a bal játékosnak
        typeof(GameManager).GetField("gold", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(gm, new int[] { 0, 10 });

        var method = typeof(GameManager).GetMethod("StartPlacingUnit", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(gm, new object[] { 0 });

        yield return null;

        Assert.IsFalse(tile.isOccupied, "Unit placement elindult, miközben kevés az arany.");
    }
}
