using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Reflection;

public class HexTileTests
{
    private GameObject tileObj;
    private HexTile tile;
    private Renderer rend;
    private GameObject gmObj;
    private GameManager gm;
    private GameObject camObj;

    [SetUp]
    public void Setup()
    {
        // GameManager létrehozása
        gmObj = new GameObject("GameManager");
        gm = gmObj.AddComponent<GameManager>();
        gm.unitPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gm.startingGold = 10;
        gm.unitCost = 5;
        GameManager.Instance = gm;

        // Dummy WorldManager létrehozása
        var wmObj = new GameObject("WorldManager");
        var wm = wmObj.AddComponent<WorldManager>();
        wm.SetGold(new int[] { 10, 10 });
        WorldManager.Instance = wm;

        // MainCamera létrehozása (különben FollowMouse() NRE-t dob)
        camObj = new GameObject("MainCamera");
        camObj.AddComponent<Camera>();
        camObj.tag = "MainCamera";

        // HexTile létrehozása
        tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile = tileObj.AddComponent<HexTile>();
        tile.gameObject.layer = LayerMask.NameToLayer("LeftZone");
        tile.isOccupied = false;
        rend = tileObj.GetComponent<Renderer>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(tileObj);
        Object.DestroyImmediate(gmObj);
        Object.DestroyImmediate(camObj);
        if (WorldManager.Instance != null)
            Object.DestroyImmediate(WorldManager.Instance.gameObject);
        WorldManager.Instance = null;
        GameManager.Instance = null;
    }

    [UnityTest]
    public IEnumerator MouseEnter_HighlightsCorrectColor()
    {
        // StartPlacingUnit(0) hívása reflectionnel
        var method = typeof(GameManager).GetMethod("StartPlacingUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(gm, new object[] { 0 });

        yield return null;

        tileObj.SendMessage("OnMouseEnter");
        yield return null;

        Assert.AreEqual(gm.leftPlayerColor, rend.material.color,
            "Aktív játékos színe nem lett beállítva helyesen.");
    }

    [UnityTest]
    public IEnumerator MouseDown_CallsTryPlaceUnit()
    {
        // StartPlacingUnit(0) reflectionnel
        var method = typeof(GameManager).GetMethod("StartPlacingUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(gm, new object[] { 0 });

        yield return null;

        // Tile kattintás
        tile.isOccupied = false;
        tileObj.SendMessage("OnMouseDown");
        yield return new WaitForSeconds(0.1f);

        // Foglaltság ellenőrzése
        Assert.IsTrue(tile.isOccupied,
            "TryPlaceUnit nem frissítette a tile foglaltságot.");
    }
}
