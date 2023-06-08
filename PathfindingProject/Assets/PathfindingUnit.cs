using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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

    Pathfinding pathfinding;
    public List<int2> path = new List<int2>();
    private void Awake()
    {
        pathfinding = GameObject.Find("Pathfinding").GetComponent<Pathfinding>();
    }
    public PathfindingTileGrid GetGrid()
    {
        return currentTileGrid;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            //This Creates Coroutines in paralel as many times the button is pressed, one is created
            //StartCoroutine(FindPath());
            pathfinding.RequestPath(transform.position, pathfindingTarget.position, this);
        }
    }

    
    public void SetPath(List<int2> _path)
    {
        path = _path;
    }
    public void SetPath(int2[] _path)
    {
        path.Clear();
        path.AddRange(_path);
    }
    private void OnDrawGizmos()
    {
        if(DisplayGizmos && pathfindingTarget != null && currentTileGrid != null && currentTileGrid.GetTileCount() != 0)
        {
            if(path != null && path.Count > 0)
            {
                for (int i = 1; i < path.Count - 1; i++)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawCube(currentTileGrid.WorldPointFromTile(path[i]), Vector3.one * (currentTileGrid.GetTileRadius() * TileGizmoSize));
                }
            }
            
            Gizmos.color = unitColor;
            Gizmos.DrawCube(currentTileGrid.TileFromWorldPoint(transform.position).wPos, Vector3.one * (currentTileGrid.GetTileRadius() * TileGizmoSize));
            Gizmos.color = targetColor;
            Gizmos.DrawCube(currentTileGrid.TileFromWorldPoint(pathfindingTarget.position).wPos, Vector3.one * (currentTileGrid.GetTileRadius() * TileGizmoSize));
        }
    }
}
