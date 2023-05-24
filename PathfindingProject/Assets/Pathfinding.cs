using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();

    private void Awake()
    {
        
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            //This Creates Coroutines in paralel as many times the button is pressed, one is created
            StartCoroutine(FindPath());
        }
    }


    IEnumerator FindPath()
    {
        /* Debug for testing coroutine
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitForSeconds(5);
        Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        Debug.Log("FIND");
        yield break;*/


        yield break;
    }
    public void RequestPath(Vector3 from, Vector3 to, PathfindingUnit _unit) //Units Create the request
    {
        PathfindingTileGrid TileGrid = _unit.GetGrid();
        PathfindingTile start = TileGrid.TileFromWorldPoint(from);
        PathfindingTile end = TileGrid.TileFromWorldPoint(to);

        //Create and set the request parameters
        PathRequest request = new PathRequest();
        request.start = start.GetGridPos();
        request.end = end.GetGridPos();
        request.path = new NativeList<int2>(Allocator.TempJob);
        request.unit = _unit;
        pathRequestQueue.Enqueue(request);
    }
    private struct PathRequest
    {
        public int2 start;
        public int2 end;

        public PathfindingUnit unit;

        public NativeList<int2> path;
    }
}
