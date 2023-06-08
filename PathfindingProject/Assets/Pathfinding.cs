using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class Pathfinding : MonoBehaviour
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();

    PathRequest currentRequest;
    [SerializeField] int numberOfRequestToSolveAtOnce = 5;//We solve 5 at once (for now?)
    int2[] neighbourOffsetArray = new int2[8];

    float startTime;

    public bool useJobs;
    private void Awake()
    {
        //for later use when checking neighbours in pathfinding
        neighbourOffsetArray[0] = new int2(-1, 0);//left
        neighbourOffsetArray[1] = new int2(+1, 0); //right
        neighbourOffsetArray[2] = new int2(0, +1);//up
        neighbourOffsetArray[3] = new int2(0, -1); //down
        neighbourOffsetArray[4] = new int2(-1, -1); // left down
        neighbourOffsetArray[5] = new int2(-1, +1);// left up
        neighbourOffsetArray[6] = new int2(+1, -1);// right down
        neighbourOffsetArray[7] = new int2(+1, +1); // right up
    }

    private void Update()
    {
        ManageQueue();
        
    }
    void ManageQueue()
    {
        if(pathRequestQueue.Count<=0)
        {
            return;
        }
        if (useJobs)
        {
            //Debug Solve Time
            startTime = Time.realtimeSinceStartup;

            PathRequest[] requestsToSolve = new PathRequest[numberOfRequestToSolveAtOnce< pathRequestQueue.Count ? numberOfRequestToSolveAtOnce : pathRequestQueue.Count];

            NativeList<JobHandle> jobHandleList = new NativeList<JobHandle>(Allocator.TempJob);
            for (int i = 0; i < numberOfRequestToSolveAtOnce; i++)
            {
                try
                {
                    currentRequest = pathRequestQueue.Dequeue();
                    if (currentRequest.unit != null)// if unit is deleted in the meantime, dont execute calculations
                    {
                        requestsToSolve[i] = currentRequest;
                    }
                    
                }
                catch (System.Exception e) { }
                
            }

            //We gathered all the request, created the structs from them
            for (int i = 0; i < requestsToSolve.Length; i++)
            {
                //Out of the structs, we create the handles by scheduling them to exectuion
                if(requestsToSolve[i].unit != null)
                {
                    jobHandleList.Add(CreateFindPathJobHandle(requestsToSolve[i]));
                }
                
            }
            //Running complete will call all exectuces on the collection
            JobHandle.CompleteAll(jobHandleList);
            // here all the calculations are already completed, give units their path from the requests
            for (int i = 0; i < requestsToSolve.Length; i++)
            {
                if(requestsToSolve[i].unit != null)
                {
                    requestsToSolve[i].unit.SetPath(requestsToSolve[i].path.ToArray());
                    requestsToSolve[i].path.Dispose();
                }
            }
            jobHandleList.Dispose();
            Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        }
        else
        {
            //Debug Solve Time
            startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < numberOfRequestToSolveAtOnce; i++)
            {
                try
                {
                    currentRequest = pathRequestQueue.Dequeue();
                    if (currentRequest.unit == null) continue; // if unit is deleted in the meantime, dont execute calculations
                    FindPath();

                }
                catch (System.Exception e) { }
                
            }
            //Debug solve time
            Debug.Log(((Time.realtimeSinceStartup - startTime) * 1000f) + "ms");
        }
    }
    void FindPath()
    {

        // Create a contaner to the path we will provide
        List<int2> pathData = new List<int2>();
        //Start and end from the request
        int2 startPosition = currentRequest.start;
        int2 endPosition = currentRequest.end;
        //Get the grid and create locally used Array with local struct - Pathnode
        //Later put it in the request!!!!
        List<PathNode> pathNodeArray = new List<PathNode>(CreatePathNodeGridArray(currentRequest.unit.GetGrid()));

        //Create Two lists for A* pathfinding
        List<int> openList = new List<int>();
        List<int> closedList = new List<int>();

        //Setup the end node
        int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, currentRequest.unit.GetGrid().GetWidth());
        if (!pathNodeArray[endNodeIndex].isWalkable) //target is over an obstacle - dont try finding a path
        {
            return;
        }

        //Setup the start node 
        PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, currentRequest.unit.GetGrid().GetWidth())];
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
            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPos = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                if (!IsPositionInsideGrid(neighbourPos, currentRequest.unit.GetGrid().GetWidth()))
                {
                    //Neighbour not vailid
                    continue;
                }
                int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y, currentRequest.unit.GetGrid().GetWidth());
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
    private static int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++)
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
        if(useJobs)
        {
            request.path = new NativeList<int2>(Allocator.TempJob);
        }else
        {

        }
        
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
    private static NativeList<int2> CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode)
    {
        if (endNode.cameFromNodeIndex == -1)
        {
            //couldtn find a path
            return new NativeList<int2>(Allocator.Temp);
        }
        else
        {
            //found paht
            NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
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
    private PathNode[] CreatePathNodeGridArray(PathfindingTileGrid _grid)
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
        
        return pathNodeArray;
    }
    
    private static int CalculateIndex(int x, int y, int gridWith)
    {
        return x + y * gridWith;
    }


   

    //We create a jobhandle out of the job we declared
    private JobHandle CreateFindPathJobHandle(PathRequest pathRequest)
    {
        findPathJob findPathJob = new findPathJob
        {
            //Requests hold the result path, it can be assigned to the unit through it
            pathData = pathRequest.path,
            neighboursArray = new NativeArray<int2>(neighbourOffsetArray, Allocator.TempJob),
            pathNodeArray = new NativeArray<PathNode>(CreatePathNodeGridArray(pathRequest.unit.GetGrid()), Allocator.TempJob),
            startPosition = pathRequest.start,
            endPosition = pathRequest.end,
            gridSize = pathRequest.unit.GetGrid().GetWidth()
        };
 
        
        
        return findPathJob.Schedule();
    }

    
    struct PathRequest
    {
        public int2 start;
        public int2 end;

        public PathfindingUnit unit;
        public NativeList<int2> path;
    }
    struct PathNode
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

    [BurstCompile]
    struct findPathJob : IJob
    {
        //Struct to contain the pathfinding logic

        //Path we need to read when pathfinding completed
        public NativeList<int2> pathData;

        public int2 gridSize;

        //The array we use the search trees to find a path
        [DeallocateOnJobCompletion]
        public NativeArray<PathNode> pathNodeArray;
        [DeallocateOnJobCompletion]
        public NativeArray<int2> neighboursArray;
        public int2 startPosition;
        public int2 endPosition;
        public void Execute()
        {
            //Create Two lists for A* pathfinding
            /* Job compatible lists*/
            NativeList<int> openList= new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            //Setup the end node
            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);
            if (!pathNodeArray[endNodeIndex].isWalkable) //target is over an obstacle - dont try finding a path
            {
                return;
            }

            //Setup the start node 
            PathNode startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            openList.Add(startNode.index);

            //Reset all pathnodes - came from node and heuristic cost
            for (int i = 0; i < pathNodeArray.Length; i++)
            {
                PathNode pathNode = pathNodeArray[i];
                pathNode.cameFromNodeIndex = -1;
                pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.y), endPosition);

                pathNodeArray[i] = pathNode;
            }
            //All the nodes have the heuristc cost
            //Every checked node is in the closed list
            //Execute the algorithm

            while (openList.Length > 0)
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
                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAt(i);
                        break;
                    }
                }
                closedList.Add(currentNodeIndex);

                //Check around the selected node
                for (int i = 0; i < neighboursArray.Length; i++)
                {
                    int2 neighbourOffset = neighboursArray[i];
                    int2 neighbourPos = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPos, gridSize))
                    {
                        //Neighbour not vailid
                        continue;
                    }
                    int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y, gridSize.x);
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
                pathData.Add(startPosition);
                Debug.Log("Didn't find a path");
            }
            else
            {
                //found path
                pathData.AddRange(CalculatePath(pathNodeArray, endNode));
            }

            
            openList.Dispose();
            closedList.Dispose();
        }
    }
}






