using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PathfindingTile
{
    //Grid this tile is in
    private PathfindingTileGrid grid;
    private int x;
    private int y;

    //World position
    public Vector3 wPos;

    //Flag to check obstacles - for now
    private bool isWalkable;

    public PathfindingTile(PathfindingTileGrid grid, int x, int y, Vector3 pos, bool _isWalkable)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
        isWalkable = _isWalkable;
        wPos = pos;
    }
    public bool IsWalkable()
    {
        return isWalkable;
    }

    public int2 GetGridPos()
    {
        return new int2(x, y);
    }
    public void SetIsWalkable(bool _isWalkable)
    {
        this.isWalkable = _isWalkable;
    }
    public Vector3 GetWorldPos()
    {
        return wPos;
    }
}
