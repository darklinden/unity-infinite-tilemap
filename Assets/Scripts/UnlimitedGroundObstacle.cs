using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UnlimitedGroundObstacle : MonoBehaviour
{
    [Serializable]
    public struct IntRange
    {
        public int Min;
        public int Max;
    }

    public struct DecoData
    {
        public Vector2Int RelativePosition;
        public bool IsEmpty;
        public int TileIndex;
    }

    public Vector2Int ViewSizeRadius = new Vector2Int(10, 10);
    public Vector2Int ViewSizeRadiusMax = new Vector2Int(100, 100);
    public Vector2Int ViewSizeDiameterMax { get; set; }

    public Tile[] Tiles;
    public int[] TilesWeights;

    public IntRange DecoCountRange = new IntRange { Min = 10, Max = 20 };
    public Dictionary<Vector2Int, DecoData> DecoTiles { get; set; }
    protected bool _initialized = false;

    protected void Awake()
    {
        ViewSizeDiameterMax = new Vector2Int { x = ViewSizeRadiusMax.x * 2 + 1, y = ViewSizeRadiusMax.y * 2 + 1 };
        DecoTiles = new Dictionary<Vector2Int, DecoData>(64);
        var decoCount = UnityEngine.Random.Range(DecoCountRange.Min, DecoCountRange.Max);
        for (int i = 0; i < decoCount; i++)
        {
            var x = UnityEngine.Random.Range(0, ViewSizeDiameterMax.x - 1);
            var y = UnityEngine.Random.Range(0, ViewSizeDiameterMax.y - 1);

            var pos = new Vector2Int(x, y);
            if (DecoTiles.ContainsKey(pos))
            {
                i--;
                continue;
            }
            var tileIndex = RandomTileByWeight();
            DecoTiles.Add(pos, new DecoData { RelativePosition = pos, IsEmpty = false, TileIndex = tileIndex });
        }

        _initialized = true;
    }

    protected int _totalWeight = -1;
    public int RandomTileByWeight()
    {
        if (_totalWeight == -1)
        {
            _totalWeight = 0;
            foreach (var weight in TilesWeights)
            {
                _totalWeight += weight;
            }
        }

        var rnd = UnityEngine.Random.Range(0, _totalWeight);
        var sum = 0;
        for (var i = 0; i < TilesWeights.Length; i++)
        {
            sum += TilesWeights[i];
            if (rnd < sum)
            {
                return i;
            }
        }

        return 0;
    }

    protected Vector3Int? _lastTilePos = null;
    protected void Update()
    {
        if (_initialized == false)
        {
            return;
        }
        if (CameraTest.Instance == null)
        {
            return;
        }

        var vec3 = CameraTest.Instance.transform.position;
        var tilePos = new Vector3Int((int)vec3.x, 0, (int)vec3.z);
        if (!_lastTilePos.HasValue || Mathf.Abs(tilePos.x - _lastTilePos.Value.x) + Mathf.Abs(tilePos.z - _lastTilePos.Value.z) >= 4)
        {
            RefreshTiles(_lastTilePos, tilePos);
            _lastTilePos = tilePos;
        }
    }

    protected Tilemap m_Tilemap;
    protected Tilemap Tilemap { get => m_Tilemap ?? (m_Tilemap = GetComponentInChildren<Tilemap>()); }

    protected void ToMapOffset(ref int x, ref int z)
    {
        x = x + ViewSizeRadiusMax.x;
        z = z + ViewSizeRadiusMax.y;

        while (x < 0)
        {
            x += ViewSizeDiameterMax.x;
        }

        while (z < 0)
        {
            z += ViewSizeDiameterMax.y;
        }

        x = x % ViewSizeDiameterMax.x;
        z = z % ViewSizeDiameterMax.y;
    }

    protected void RefreshViewTiles(Vector3Int viewPos)
    {
        for (int x = viewPos.x - ViewSizeRadius.x; x < viewPos.x + ViewSizeRadius.x; x++)
        {
            for (int z = viewPos.z - ViewSizeRadius.y; z < viewPos.z + ViewSizeRadius.y; z++)
            {
                var tx = x;
                var tz = z;
                ToMapOffset(ref tx, ref tz);

                var key = new Vector2Int(tx, tz);
                if (!DecoTiles.TryGetValue(key, out var decoData))
                {
                    Tilemap.SetTile(new Vector3Int(x, z, 0), null);
                }
                else
                {

                    Tilemap.SetTile(new Vector3Int(x, z, 0), Tiles[decoData.TileIndex]);
                }
            }
        }

        // Tilemap.CompressBounds();
    }

    protected void RefreshTiles(Vector3Int? prePosV, Vector3Int curPos)
    {
        if (!prePosV.HasValue)
        {
            RefreshViewTiles(curPos);
            return;
        }

        var prePos = prePosV.Value;

        // clear delta x
        var dx = curPos.x - prePos.x;
        int fromX = 0;
        int toX = 0;

        if (dx > 0)
        {
            fromX = prePos.x - ViewSizeRadius.x;
            toX = curPos.x - ViewSizeRadius.x;
        }
        else
        {
            fromX = curPos.x + ViewSizeRadius.x;
            toX = prePos.x + ViewSizeRadius.x;
        }

        for (int x = fromX; x < toX; x++)
        {
            for (int z = Mathf.Min(prePos.z, curPos.z) - ViewSizeRadius.y; z < Mathf.Max(prePos.z, curPos.z) + ViewSizeRadius.y; z++)
            {
                Tilemap.SetTile(new Vector3Int(x, z, 0), null);
            }
        }

        // clear delta z
        var dz = curPos.z - prePos.z;
        var fromZ = 0;
        var toZ = 0;

        if (dz > 0)
        {
            fromZ = prePos.z - ViewSizeRadius.y;
            toZ = curPos.z - ViewSizeRadius.y;
        }
        else
        {
            fromZ = curPos.z + ViewSizeRadius.y;
            toZ = prePos.z + ViewSizeRadius.y;
        }

        for (int z = fromZ; z < toZ; z++)
        {
            for (int x = Mathf.Min(prePos.x, curPos.x) - ViewSizeRadius.x; x < Mathf.Max(prePos.x, curPos.x) + ViewSizeRadius.x; x++)
            {
                Tilemap.SetTile(new Vector3Int(x, z, 0), null);
            }
        }

        // set delta x
        if (dx > 0)
        {
            fromX = prePos.x + ViewSizeRadius.x;
            toX = curPos.x + ViewSizeRadius.x;
        }
        else
        {
            fromX = curPos.x - ViewSizeRadius.x;
            toX = prePos.x - ViewSizeRadius.x;
        }

        for (int x = fromX; x < toX; x++)
        {
            for (int z = curPos.z - ViewSizeRadius.y; z < curPos.z + ViewSizeRadius.y; z++)
            {
                var tx = x;
                var tz = z;
                ToMapOffset(ref tx, ref tz);

                if (DecoTiles.TryGetValue(new Vector2Int(tx, tz), out var decoData))
                {
                    Tilemap.SetTile(new Vector3Int(x, z, 0), Tiles[decoData.TileIndex]);
                }
                else
                {
                    Tilemap.SetTile(new Vector3Int(x, z, 0), null);
                }
            }
        }

        // set delta z
        if (dz > 0)
        {
            fromZ = prePos.z + ViewSizeRadius.y;
            toZ = curPos.z + ViewSizeRadius.y;
        }
        else
        {
            fromZ = curPos.z - ViewSizeRadius.y;
            toZ = prePos.z - ViewSizeRadius.y;
        }

        for (int z = fromZ; z < toZ; z++)
        {
            for (int x = curPos.x - ViewSizeRadius.x; x < curPos.x + ViewSizeRadius.x; x++)
            {
                var tx = x;
                var tz = z;
                ToMapOffset(ref tx, ref tz);

                if (DecoTiles.TryGetValue(new Vector2Int(tx, tz), out var decoData))
                {
                    Tilemap.SetTile(new Vector3Int(x, z, 0), Tiles[decoData.TileIndex]);
                }
                else
                {
                    Tilemap.SetTile(new Vector3Int(x, z, 0), null);
                }
            }
        }

        Tilemap.CompressBounds();
    }
}