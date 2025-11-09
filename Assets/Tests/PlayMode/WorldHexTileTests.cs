using NUnit.Framework;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;

public class WorldHexTileTests
{
    private GameObject tileObj;
    private WorldHexTile tile;
    private Renderer rend;

    [SetUp]
    public void Setup()
    {
        tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile = tileObj.AddComponent<WorldHexTile>();
        rend = tileObj.GetComponent<Renderer>();

        var wmObj = new GameObject("WorldManager");
        var wm = wmObj.AddComponent<WorldManager>();
        WorldManager.Instance = wm;

        // Meghívjuk a Start()-ot a WorldManageren
        wm.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(wm, null);

        // Meghívjuk a Start()-ot a WorldHexTile-en is, hogy originalColor inicializálva legyen
        tile.GetType().GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(tile, null);
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(tileObj);
        if (WorldManager.Instance != null)
            Object.DestroyImmediate(WorldManager.Instance.gameObject);
        WorldManager.Instance = null;
    }

    [Test]
    public void Tile_StoresHexCoordinatesCorrectly()
    {
        tile.hexX = 4;
        tile.hexZ = 5;
        Assert.AreEqual(4, tile.hexX);
        Assert.AreEqual(5, tile.hexZ);
    }

    [UnityTest]
    public IEnumerator RaiseTile_RaisesAndResetsPosition()
    {
        Vector3 startPos = tile.transform.position;

        var method = tile.GetType().GetMethod("RaiseTile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(tile, new object[] { true });

        yield return new WaitForSeconds(0.1f);
        Assert.Greater(tile.transform.position.y, startPos.y);

        method.Invoke(tile, new object[] { false });
        Assert.AreEqual(startPos.y, tile.transform.position.y, 0.01f);
    }

    [Test]
    public void ResetColor_RestoresOriginalColor()
    {
        Color original = rend.material.color;
        rend.material.color = Color.red;

        tile.ResetColor();

        Assert.AreEqual(original, rend.material.color, "ResetColor nem állította vissza az eredeti színt.");
    }

    [UnityTest]
    public IEnumerator OnMouseEnter_ChangesColorIfClickable()
    {
        tile.gameObject.layer = LayerMask.NameToLayer("RightZone");

        var neighborObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var neighborTile = neighborObj.AddComponent<WorldHexTile>();
        neighborTile.hexX = tile.hexX - 1;
        neighborTile.hexZ = tile.hexZ;
        neighborObj.layer = LayerMask.NameToLayer("LeftZone");

        var allTilesField = typeof(WorldManager).GetField("allTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        allTilesField.SetValue(WorldManager.Instance, new[] { tile, neighborTile });

        tile.SendMessage("OnMouseEnter");
        yield return null;

        Assert.AreEqual(Color.yellow, rend.material.color);

        Object.DestroyImmediate(neighborObj);
    }

    [UnityTest]
    public IEnumerator OnMouseExit_ResetsColorAfterHover()
    {
        tile.SendMessage("OnMouseEnter");
        yield return null;
        tile.SendMessage("OnMouseExit");
        yield return null;
        Assert.AreNotEqual(Color.yellow, rend.material.color);
    }
}
