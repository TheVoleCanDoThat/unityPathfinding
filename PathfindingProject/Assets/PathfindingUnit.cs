using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathfindingUnit : MonoBehaviour
{
    [SerializeField]
    //For test purposes, this is assigned by hand
    //later untis can register onto tilegrids to use them in pathfinding calculations
    PathfindingTileGrid currentTileGrid;

    [SerializeField]
    [Range(0.5f, 1f)] float TileGizmoSize = 1;
    [SerializeField] bool DisplayGizmos = true;
    [SerializeField] Color unitColor = Color.cyan,targetColor = Color.green;
    [SerializeField] Transform pathfindingTarget;

    public PathfindingTileGrid GetGrid()
    {
        return currentTileGrid;
    }

    private void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if(DisplayGizmos && pathfindingTarget != null && currentTileGrid != null && currentTileGrid.GetTileCount() != 0)
        {
            
            Gizmos.color = unitColor;
            Gizmos.DrawCube(currentTileGrid.TileFromWorldPoint(transform.position).wPos, Vector3.one * (currentTileGrid.GetTileRadius() * TileGizmoSize));
            Gizmos.color = targetColor;
            Gizmos.DrawCube(currentTileGrid.TileFromWorldPoint(pathfindingTarget.position).wPos, Vector3.one * (currentTileGrid.GetTileRadius() * TileGizmoSize));
        }
    }
}
