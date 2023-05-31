using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;

public class Pathfinding : MonoBehaviour
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();

    public PathfindingTileGrid debugTileGrid;
    public bool findingPath;
    PathRequest currentRequest;

    int2[] neighbourOffsetArry = new int2[8];
    //NativeArray<int2> neighbourOffsetArry;
    private void Awake()
    {
        //neighbourOffsetArry = new NativeArray<int2>(8, Allocator.Temp);
        neighbourOffsetArry[0] = new int2(-1, 0);//left
        neighbourOffsetArry[1] = new int2(+1, 0); //right
        neighbourOffsetArry[2] = new int2(0, +1);//up
        neighbourOffsetArry[3] = new int2(0, -1); //down
        neighbourOffsetArry[4] = new int2(-1, -1); // left down
        neighbourOffsetArry[5] = new int2(-1, +1);// left up
        neighbourOffsetArry[6] = new int2(+1, -1);// right down
        neighbourOffsetArry[7] = new int2(+1, +1); // right up
    }

    private void Update()
    {
        //Debug.Log(pathRequestQueue.Count);
        if(pathRequestQueue.Count>0 && !findingPath)
        {
            findingPath = true;
            currentRequest = pathRequestQueue.Dequeue();
            if (currentRequest.unit == null) return; // if unit is deleted in the meantime, dont execute calculations

            StartCoroutine(FindPath());
        }
    }

    void FindPath(int2 startPosition,int2 endPosition)
    {

    }

    IEnumerator FindPath()
    {
        //Debug Solve Time
        float startTime = Time.realtimeSinceStartup;

        // Create a contaner to the path we will provide
        List<int2> pathData = new List<int2>();
        //Start and end from the request
        int2 startPosition = currentRequest.start;
        int2 endPosition = currentRequest.end;
        //Get the grid and create locally used Array with local struct - Pathnode
        //Later put it in the request!!!!
        List<PathNode> pathNodeArray = CreatePathNodeGrid(debugTileGrid);

        //Create Two lists for A* pathfinding
        List<int> openList = new List<int>();
        List<int> closedList = new List<int>();

        /* Job compatible list
        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);*/

        //Setup the end node
        int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, debugTileGrid.GetWidth());
        if (!pathNodeArray[endNodeIndex].isWalkable) //target is over an obstacle - dont try finding a path
        {
            yield break;
        }

        //Setup the start node 
        PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, debugTileGrid.GetWidth())];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        openList.Add(startNode.index);

        //Reset all pathnodes - came from node and heuristic cost
        for (int i = 0; i < pathNodeArray.Count; i++)
        {
            PathNode pathNode = pathNodeArray[i];
            pathNode.cameFromNodeIndex = -1;
            pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.y), endPosition);

            pathNodeArray[i] = pathNode;
        }
        //All the nodes have the heuristc cost
        //Every checked node is in the closed list
        //Execute the algorithm
        
        while (openList.Count > 0)
        {
           
            //We get the best node form the open list
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
            PathNode currentNode = pathNodeArray[currentNodeIndex];
            if (currentNodeIndex == endNodeIndex)
            {
                //reached destination
                break;
            }
            
            //Take out the node we are checking from the open list and put it to the closed
            for (int i = 0; i < openList.Count; i++)
            {
                if (openList[i] == currentNodeIndex)
                {
                    openList.RemoveAt(i);
                    break;
                }
            }
            closedList.Add(currentNodeIndex);
            
            //Check around the selected node
            for (int i = 0; i < neighbourOffsetArry.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArry[i];
                int2 neighbourPos = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                if (!IsPositionInsideGrid(neighbourPos, debugTileGrid.GetWidth()))
                {
                    //Neighbour not vailid
                    continue;
                }
                int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y, debugTileGrid.GetWidth());
                if (closedList.Contains(neighbourNodeIndex))
                {
                    //already searched this node
                    continue;
                }

                PathNode neighbourNode = pathNodeArray[neighbourNodeIndex];
                if (!neighbourNode.isWalkable)
                {
                    //not walkable
                    continue;
                }

                int2 currentNodePos = new int2(currentNode.x, currentNode.y);

                int tentativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePos, neighbourPos);
                if (tentativeGCost < neighbourNode.gCost)
                {
                    neighbourNode.cameFromNodeIndex = currentNodeIndex;
                    neighbourNode.gCost = tentativeGCost;
                    neighbourNode.CalculateFCost();
                    pathNodeArray[neighbourNodeIndex] = neighbourNode;

                    if (!openList.Contains(neighbourNode.index))
                    {
                        openList.Add(neighbourNode.index);
                    }
                }
            }
        }
        //we should have every node we need in the closed path, it leads to the end node

        PathNode endNode = pathNodeArray[endNodeIndex];
        if (endNode.cameFromNodeIndex == -1)
        {
            Debug.Log("Didn't find a path");
        }
        else
        {
            //found path
            pathData.AddRange(CalculatePath(pathNodeArray, endNode));
            currentRequest.unit.SetPath(pathData);
        }


        //Debug solve time
        Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        findingPath = false;
        yield break;

        //DEBUG - run one iteration of the while cycle
        //yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space)); yielding and waiting for space
    }
    private static int GetLowestCostFNodeIndex(List<int> openList, List<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Count; i++)
        {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }
    private static int CalculateDistanceCost(int2 aPos, int2 bPos)
    {
        int xDst = math.abs(aPos.x - bPos.x);
        int yDst = math.abs(aPos.y - bPos.y);
        int remaining = math.abs(xDst - yDst);
        return MOVE_DIAGONAL_COST * math.min(xDst, yDst) + MOVE_STRAIGHT_COST * remaining;
    }
    private static bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
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
    private static List<int2> CalculatePath(List<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            //couldtn find a path
            return null;
        }
        else
        {
            //found paht
            //NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
            List<int2> path = new List<int2>();
            path.Add(new int2(endNode.x, endNode.y));

            PathNode currentNode = endNode;
            while (currentNode.cameFromNodeIndex != -1)
            {
                PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                path.Add(new int2(cameFromNode.x, cameFromNode.y));
                currentNode = cameFromNode;
            }
            return path;
        }
    }
    private List<PathNode> CreatePathNodeGrid(PathfindingTileGrid _grid)
    {
        //We create a grid out of the objects in the game
        //This helps by holding the heruistic values and references of tiles
        //All done with structs in mind to later implement job system
        PathfindingTile[,] grid = _grid.GetTileGrid();
        int width = _grid.GetWidth();
        int height = _grid.GetHeight();

        int2 gridSize = new int2(width, height);
        PathNode[] pathNodeArray = new PathNode[gridSize.x * gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                PathNode pathNode = new PathNode();
                pathNode.x = x;
                pathNode.y = y;
                pathNode.index = CalculateIndex(x, y, gridSize.x);

                pathNode.gCost = int.MaxValue;


                pathNode.isWalkable = grid[x, y].IsWalkable();
                pathNode.cameFromNodeIndex = -1;
                pathNodeArray[pathNode.index] = pathNode;
            }

        }
        List<PathNode> pathList = new List<PathNode>();
        pathList.AddRange(pathNodeArray);
        return pathList;
    }
    private static int CalculateIndex(int x, int y, int gridWith)
    {
        return x + y * gridWith;
    }


    private struct PathRequest
    {
        public int2 start;
        public int2 end;

        public PathfindingUnit unit;

        public NativeList<int2> path;
    }
    private struct PathNode
    {
        public int x;
        public int y;

        public int index;

        public int gCost;
        public int hCost;
        public int fCost;

        public bool isWalkable;
        public int cameFromNodeIndex;

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
        public void SetIsWalkable(bool _isWalkable)
        {
            this.isWalkable = _isWalkable;
        }
    }
    
}
