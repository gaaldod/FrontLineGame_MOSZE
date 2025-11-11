using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class WorldHexMap3DTests
{
    private GameObject mapObj;
    private WorldHexMap3D map;

    [SetUp]
    public void Setup()
    {
        mapObj = new GameObject("WorldHexMap3D");
        map = mapObj.AddComponent<WorldHexMap3D>();

        // Dummy prefabs létrehozása
        map.groundHexPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        map.castleHexPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Map paraméterek beállítása a teszthez
        map.width = 4;
        map.height = 4;
        map.hexSize = 1f;
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(mapObj);
        Object.DestroyImmediate(map.groundHexPrefab);
        Object.DestroyImmediate(map.castleHexPrefab);
    }

    [Test]
    public void GenerateMap_CreatesCorrectNumberOfTiles()
    {
        map.SendMessage("GenerateMap", null, SendMessageOptions.DontRequireReceiver);

        int expectedTileCount = (map.width * 2) * (map.height / 2); // a Start() logikája szerint
        var tiles = mapObj.GetComponentsInChildren<Transform>();
        int actualTileCount = 0;
        foreach (var t in tiles)
        {
            if (t != mapObj.transform) actualTileCount++;
        }

        Assert.AreEqual(expectedTileCount, actualTileCount);
    }

    [Test]
    public void CastleTile_IsCorrectlyTaggedAndFlagged()
    {
        map.SendMessage("GenerateMap", null, SendMessageOptions.DontRequireReceiver);

        WorldHexTile castleTile = null;
        foreach (Transform child in mapObj.transform)
        {
            var tileComp = child.GetComponent<WorldHexTile>();
            if (tileComp != null && tileComp.isCastleTile)
            {
                castleTile = tileComp;
                break;
            }
        }

        Assert.IsNotNull(castleTile, "Castle tile nem található.");
        Assert.AreEqual("Castle", castleTile.gameObject.tag);
        Assert.IsTrue(castleTile.isCastleTile);
    }

    [Test]
    public void TilesHaveCorrectLayers()
    {
        map.SendMessage("GenerateMap", null, SendMessageOptions.DontRequireReceiver);

        int midX = map.width; // a Start() logikájában width*2
        foreach (Transform child in mapObj.transform)
        {
            var tileComp = child.GetComponent<WorldHexTile>();
            if (tileComp == null) continue;

            if (tileComp.hexX < midX / 2)
                Assert.AreEqual(LayerMask.NameToLayer("LeftZone"), tileComp.gameObject.layer);
            else
                Assert.AreEqual(LayerMask.NameToLayer("RightZone"), tileComp.gameObject.layer);
        }
    }
}
