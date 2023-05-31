using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PathfindingTileGrid : MonoBehaviour
{
    [SerializeField]
    [Range(0.5f, 1f)] float TileGizmoSize = 1;
    [SerializeField] Color defaultTileColor = Color.white;
    public bool displayGridArea;
    public bool displayGridGizmos;

    public Vector2 gridWorldSize;
    public float tileRadius = 0.5f;

    float tileDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    [SerializeField]
    public LayerMask obstacleMask;

    PathfindingTile[,] tileGrid;

    
    private void Awake()
    {
        tileDiameter = tileRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / tileDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / tileDiameter);
        CreateGrid();
    }

    private void OnDrawGizmos()
    {
        //Grid Area Display
        if(displayGridArea)
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));
        }
        //Grid Tile Display
        if (tileGrid != null && displayGridGizmos)
        {
            foreach (PathfindingTile t in tileGrid)
            {
                //Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, t.movementPenalty));

                Gizmos.color = (t.IsWalkable()) ? defaultTileColor : Color.red;

                Gizmos.DrawCube(t.GetWorldPos(), Vector3.one * (tileDiameter * TileGizmoSize));
            }
        }

    }
    
    public PathfindingTile TileFromWorldPoint(Vector3 worldPos)
    {
        if (tileGrid == null || tileGrid.Length == 0) return null;
        //Calculate the actual size with gridTile Diamater
        float actualGridSizeX = gridSizeX*tileDiameter;
        float actualGridSizeY = gridSizeY *tileDiameter;
        //Get the percentage where we are on the grid
        float percentX = ((worldPos.x-transform.position.x) + actualGridSizeX * .5f) / actualGridSizeX;
        float percentY = (worldPos.y - transform.position.y + actualGridSizeY * .5f) / actualGridSizeY;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        float TileDiameterXAsPercent = tileDiameter / gridSizeX;
        float TileDiameterYAsPercent = tileDiameter / gridSizeY;
        //Calculate the x,y coordiantes with one tile as percentage and the current percentage
        int x = Mathf.Clamp(Mathf.FloorToInt(percentX / TileDiameterXAsPercent), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(percentY / TileDiameterYAsPercent), 0, gridSizeY - 1);

        return tileGrid[x, y];
    }
    public Vector3 WorldPointFromTile(int2 tileCoord)
    {
        if (tileGrid == null || tileGrid.Length == 0) return Vector3.zero;
        return tileGrid[tileCoord.x, tileCoord.y].wPos;
    }
    void CreateGrid()
    {
        tileGrid = new PathfindingTile[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * (gridWorldSize.x / 2) - Vector3.up * (gridWorldSize.y / 2);
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * tileDiameter + tileRadius) + Vector3.up * (y * tileDiameter + tileRadius);
                bool isObstructed = Physics2D.BoxCast(worldPoint, new Vector2(tileDiameter, tileDiameter), 0, Vector2.zero, 1f, obstacleMask);
                tileGrid[x, y] = new PathfindingTile(this, x, y, worldPoint, !isObstructed);
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // Debug.Log(tileGrid[x, y].obstacleProximity); 
            }
        }
    }
    public float GetTileRadius()
    {
        return tileRadius;
    }
    public int GetTileCount()
    {
        if (tileGrid == null) return 0;
        return tileGrid.Length;
    }
    public PathfindingTile[,] GetTileGrid()
    {
        return tileGrid;
    }
    public int GetHeight()
    {
        return gridSizeY;
    }
    public int GetWidth()
    {
        return gridSizeX;
    }
}
