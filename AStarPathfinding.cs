using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class AStarPathfinding : MonoBehaviour
{
    // Grid variables
    // [S,0,1,0,0]
    // [0,0,1,0,0]
    // [0,0,1,0,0]
    // [0,0,1,0,0]
    // [0,0,0,0,T]
    private int[,] grid;
    public int gridSizeX = 5;
    public int gridSizeY = 5;
    public List<Vector2Int> obstacles = new List<Vector2Int>();

    // Grid display string for debugging
    private string gridString;

    // DELETE THIS REGION AFTER TESTING
    #region Display Grid onGUI and Print Pathfinding Results

    private void Start()
    {
        FindPathAndUpdateGrid(gridSizeX, gridSizeY, obstacles, startPosition, targetPosition);
    }

    // Displaying grid onGUI for debugging
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.TextArea(gridString, GUILayout.Width(200), GUILayout.Height(200));
        if (GUILayout.Button("Find Path"))
        {
            FindPathAndUpdateGrid(5, 5, obstacles, startPosition, targetPosition);
            PrintResult();
        }
        UpdateGridString();
        GUILayout.EndHorizontal();
    }

    // Grid display string for debugging
    private void UpdateGridString()
    {
        // Loop through the grid and update based on pathfinding results
        gridString = "";
        for (int y = grid.GetLength(1)-1; y >= 0; y--)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                if (grid[x, y] == 1)
                {
                    gridString += "1 ";
                }
                else if (startPosition == new Vector2Int(x, y))
                {
                    gridString += "S ";
                }
                else if (targetPosition == new Vector2Int(x, y))
                {
                    gridString += "T ";
                }
                else if(pathList.Contains(new Vector2Int(x, y)))
                {
                    gridString += "x ";
                }
                else
                {
                    gridString += "0 ";
                }
            }
            gridString += "\n";
        }
    }

    void PrintResult()
    {
        string result = "Pathfinding Result: ";
        if (pathList.Count > 0)
        {
            result += "Path found! Length: " + pathList.Count + " | Next Move is " + pathList[0];
        }
        else
        {
            result += "No path found!";
        }
        Debug.Log(result);
    }
    #endregion

    #region Pathfinding Variables
    // Pathfinding variables
    public Vector2Int startPosition = new Vector2Int(0, 0);
    public Vector2Int targetPosition = new Vector2Int(4, 4);
    public bool allowDiagonals = true; // Toggle for omnidirectional movement (8 directions)

    // Pathfinding result
    public List<Vector2Int> pathList = new List<Vector2Int>();

    // Setup variables for direction of pathfinding
    private Vector2Int[] directions;
    void UpdateDirection()
    {
        // Initialize the directions based on whether diagonal movement is allowed
        if (allowDiagonals)
        {
            directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),    // Up
                new Vector2Int(0, -1),   // Down
                new Vector2Int(-1, 0),   // Left
                new Vector2Int(1, 0),    // Right
                new Vector2Int(-1, 1),   // Up-Left (Diagonal)
                new Vector2Int(1, 1),    // Up-Right (Diagonal)
                new Vector2Int(-1, -1),  // Down-Left (Diagonal)
                new Vector2Int(1, -1)    // Down-Right (Diagonal)
            };
        }
        else
        {
            directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),    // Up
                new Vector2Int(0, -1),   // Down
                new Vector2Int(-1, 0),   // Left
                new Vector2Int(1, 0)     // Right
            };
        }
    }
    #endregion

    #region A* Pathfinding Algorithm
    // Returns a list of Vector2Int for the quickest path to take
    // If returns empty list, no path is found
    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int targetPos)
    {
        // setup pathfinding variables
        startPosition = startPos;
        targetPosition = targetPos;
        pathList.Clear();
        UpdateDirection();

        // A* algorithm implementation
        List<Vector2Int> openList = new List<Vector2Int>();   // Nodes to be evaluated
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();  // Explored nodes
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();  // For path reconstruction
        Dictionary<Vector2Int, int> gScore = new Dictionary<Vector2Int, int>();  // Cost from start
        Dictionary<Vector2Int, int> fScore = new Dictionary<Vector2Int, int>();  // Estimated total cost (g + h)

        // Initialize starting node
        openList.Add(startPosition);
        gScore[startPosition] = 0;
        fScore[startPosition] = Heuristic(startPosition, targetPosition);

        while (openList.Count > 0)
        {
            // Get the node with the lowest fScore
            Vector2Int current = GetLowestFScoreNode(openList, fScore);

            if (current == targetPosition)
            {
                return ReconstructPath(cameFrom, current);
            }

            openList.Remove(current);
            closedList.Add(current);

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;
                if (!IsValidTile(neighbor) || closedList.Contains(neighbor))
                {
                    continue;  // Skip invalid tiles or already explored nodes
                }

                int tentativeGScore = gScore[current] + 1;  // Assume each move has a cost of 1

                if (!openList.Contains(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, targetPosition);

                    if (!openList.Contains(neighbor))
                    {
                        openList.Add(neighbor);
                    }
                }
            }
        }

        return new List<Vector2Int>();  // Return an empty list if no path found
    }

    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    private bool IsValidTile(Vector2Int tile)
    {
        return tile.x >= 0 && tile.x < grid.GetLength(0) && tile.y >= 0 && tile.y < grid.GetLength(1) && grid[tile.x, tile.y] == 0;
    }

    private Vector2Int GetLowestFScoreNode(List<Vector2Int> openList, Dictionary<Vector2Int, int> fScore)
    {
        Vector2Int lowest = openList[0];
        foreach (Vector2Int node in openList)
        {
            if (fScore.ContainsKey(node) && fScore[node] < fScore[lowest])
            {
                lowest = node;
            }
        }
        return lowest;
    }

    // Heuristic function (Diagonal Distance for omnidirectional movement)
    private int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    private void UpdateGridObstacles()
    {
        // Loop through the grid and update based on obstacles[]
        for (int y = 0; y < grid.GetLength(1); y++)
        {
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                if (obstacles.Contains(new Vector2Int(x, y)))
                {
                    grid[x, y] = 1;
                }
                else
                {
                    grid[x, y] = 0;
                }
            }
        }
    }
    #endregion

    #region Function to Modify Grid and Obstacles
    public List<Vector2Int> FindPathAndUpdateGrid(int width, int height, List<Vector2Int> newObstacles, Vector2Int startPos, Vector2Int targetPos)
    {
        UpdateGrid(width, height, newObstacles);
        pathList = FindPath(startPos, targetPos);
        return pathList;
    }

    public void UpdateGrid(int width, int height, List<Vector2Int> newObstacles)
    {
        obstacles = new List<Vector2Int>(newObstacles);
        grid = new int[gridSizeX, gridSizeY];
        UpdateGridObstacles();
    }
    #endregion
}
