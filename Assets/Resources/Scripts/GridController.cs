using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;



/* Inflate left   | Inflate right
 *    A A B B     |     A A B B 
 *    A X B B     |     A A X B
 *                |
 *    A A A       |       B B B
 *    A A A B     |     A B B B
 *    A X A       |       B X B 
 * 
 * ~ ~ ~ ~ ~ ~ ~ ~ ~
 *   Inflate top
 *    A A    A A A
 *    A X    A X A
 *    B B    A A A
 *    B B      B
 * 
 * ~ ~ ~ ~ ~ ~ ~ ~ ~
 *   Inflate bottom
 *    A A      A   
 *    A A    B B B 
 *    B X    B X B
 *    B B    B B B
 * 
 */

public struct FuguPair
{
    public FuguController primary;
    public FuguController secondary;

    public FuguPair(FuguColor primaryColor, FuguColor secondaryColor, int id1, int id2, Vector2Int position, GameObject FuguPrefab)
    {
        GameObject primaryObj = GameObject.Instantiate(FuguPrefab);
        GameObject secondaryObj = GameObject.Instantiate(FuguPrefab);
        primary = primaryObj.GetComponent<FuguController>();
        secondary = secondaryObj.GetComponent<FuguController>();

        SetUpFuguController(primary, true, id1, id2, position + (Vector2Int.down * 2), primaryColor);
        SetUpFuguController(secondary, true, id2, id1, position, secondaryColor);
    }

    public void SetUpFuguController(FuguController fugu, bool isPrimary, int id, int partnerId, Vector2Int topLeftCoordinate, FuguColor color)
    {
        fugu.isPrimary = isPrimary;
        fugu.id = id;
        fugu.partnerId = partnerId;
        fugu.bottomLeftCoordinate = topLeftCoordinate + (Vector2Int.down * 2);
        fugu.color = color;
        fugu.scale = FuguScale.Medium;

        if (isPrimary)
        {
            fugu.relativePosition = RelativePosition.Down;
        }
        else
        {
            fugu.relativePosition = RelativePosition.Up;
        }
    }

    public bool IsEmpty()
    {
        return primary == null && secondary == null;
    }

    public void SetFuguPair(FuguPair fuguPair)
    {
        this.primary = fuguPair.primary;
        this.secondary = fuguPair.secondary;
    }

    public void Disconnect()
    {
        this.primary = null;
        this.secondary = null;
    }

    // Inflate the primary fugu, deflate the secondary.
    public void InflatePrimary()
    {
        // Inflate if it's smaller than a large fugu.
        if (primary.scale < FuguScale.Large)
        {
            primary.scale++;
            secondary.scale--;

            primary.SetScale();
            secondary.SetScale();
        }
        else
        {
            // Play a sound indicating this action is unavailable.
        }
    }

    // Inflate the secondary fugu, deflate the secondary.
    // If the secondary becomes larger than the primary, promote it to primary and update the pair's coordinate to match the new primary.
    public void InflateSecondary()
    {
        // ALWAYS inflate. Secondary should always be equal size or smaller than the primary.
        // Small -> Medium: Do nothing else.
        // Medium -> Large: Set as new pivot point.

        primary.scale--;
        secondary.scale++;

        primary.SetScale();
        secondary.SetScale();

        // Promote secondary to primary if it's larger now.
        if (secondary.scale > primary.scale)
        {
            FuguController temp = primary;
            primary = secondary;
            secondary = temp;
        }
    }

    public void Rotate(bool isClockwise)
    {
        //Debug.Log($"Rotate CW: {isClockwise}");
        RotateAroundPrimaryCenter(isClockwise);
    }

    private void RotateAroundPrimaryCenter(bool isClockwise)
    {
        //Debug.Log($"RotateAroundPrimaryCenter");
        UpdateFuguRelativePositions(isClockwise);

    }

    private void RotateAroundPairCenter(bool isClockwise)
    {
    }

    // Updates the relative positions of the primary and secondary Fugus based on rotations
    private void UpdateFuguRelativePositions(bool isClockwise)
    {
        Debug.Log($"UpdateFuguRelativePositions primary: {primary.relativePosition} secondary: {secondary.relativePosition}");
        secondary.relativePosition = RotateRelativePosition(secondary.relativePosition, isClockwise);
        primary.relativePosition = RotateRelativePosition(primary.relativePosition, isClockwise);
        Debug.Log($"new primary: {primary.relativePosition} secondary: {secondary.relativePosition}");
    }

    // Returns the relative position after the rotation
    private RelativePosition RotateRelativePosition(RelativePosition relativePosition, bool isClockwise)
    {
        if (isClockwise)
        {
            return (RelativePosition)((((int)relativePosition + 1) % 4));
        } else
        {
            return (RelativePosition)((((int)relativePosition - 1) % 4));
        }
    }
}



public class GridController : MonoBehaviour
{

    const int GRID_SIZE = 12;

    private int currentID = 0;
    private Vector2Int defaultSpawnPosition = new Vector2Int(5, 11);

    private float gravityTickTimer = 0.0f;
    public float freeFallTickPeriod = 2.0f;

    public FuguPair ActiveFreefallPair;

    // grid holds fugu ids
    public int[][] grid = new int[GRID_SIZE][];

    public Queue<FuguPair> fuguQueue = new Queue<FuguPair>();
    public Dictionary<int, FuguController> fuguDict = new Dictionary<int, FuguController>();
    public GameObject FuguPrefab;

    int GetUniqueID()
    {
        return currentID++;
    }
    void ResetIDGenerator()
    {
        currentID = 0;
    }

    // Completely wipe the current puzzle to make way for the next one.
    void ClearPuzzle()
    {
        // Reset the ID generator.
        ResetIDGenerator();
        // Clear the ActiveFreefallPair.
        // Clear the grid.
        // Clear the queue.
        fuguQueue.Clear();
        // Clear the dictionary.
        fuguDict.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        // load grid from file or something...
        InitGrid();
        InitQueue();
        
    }

    void InitGrid()
    {
        for (int i = 0; i < GRID_SIZE; i++)
        {
            grid[i] = new int[GRID_SIZE];
            for (int j = 0; j < GRID_SIZE; j++)
            {
                grid[i][j] = -1;
            }
        }
    }

    void InitQueue()
    {
        int id1 = GetUniqueID();
        int id2 = GetUniqueID();
        int id3 = GetUniqueID();
        int id4 = GetUniqueID();

        FuguPair a = new FuguPair(FuguColor.Red, FuguColor.Green, id1, id2, defaultSpawnPosition, FuguPrefab);
        FuguPair b = new FuguPair(FuguColor.Red, FuguColor.Cyan, id3, id4, defaultSpawnPosition, FuguPrefab);

        AddPairToDict(a);
        AddPairToDict(b);

        fuguQueue.Enqueue(a);
        fuguQueue.Enqueue(b);
    }

    public void AddPairToDict(FuguPair fuguPair)
    {
        fuguDict[fuguPair.primary.id] = fuguPair.primary;
        fuguDict[fuguPair.secondary.id] = fuguPair.secondary;
    }

    Vector2Int GetPlayerMovementInput()
    {
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            dir = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            dir = Vector2Int.right;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            dir = Vector2Int.down;
        }
        return dir;
    }

    enum ActionInput
    {
        NoAction,         // Do nothing.
        RotateCW,         // Rotate pieces clockwise.
        RotateCCW,        // Rotate pieces counter-clockwise.
        InflatePrimary,   // Inflate the fugu that was initially the upper piece. Deflate the other.
        InflateSecondary, // Inflate the fugu that was initially the lower piece. Deflate the other.
    }

    ActionInput GetPlayerActionInput()
    {
        if (Input.GetKey(KeyCode.E))
        {
            return ActionInput.RotateCW;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            return ActionInput.RotateCCW;
        }
        if (Input.GetKey(KeyCode.Z))
        {
            return ActionInput.InflatePrimary;
        }
        if (Input.GetKey(KeyCode.C))
        {
            return ActionInput.InflateSecondary;
        }
        return ActionInput.NoAction;
    }

    void HandlePlayerInput()
    {
        ActionInput actionInput = GetPlayerActionInput();

        switch (actionInput)
        {
            case ActionInput.RotateCW:
                ActiveFreefallPair.Rotate(isClockwise: true);
                break;
            case ActionInput.RotateCCW:
                ActiveFreefallPair.Rotate(isClockwise: false);
                break;
            case ActionInput.InflatePrimary:
                break;
            case ActionInput.InflateSecondary:
                break;
            case ActionInput.NoAction:
                break;
        }

        // No action. Check for movement.
        if (actionInput == ActionInput.NoAction)
        {
            Vector2Int dir = GetPlayerMovementInput();

            if (dir == Vector2Int.left)
            {
                if (CanMoveFuguPairLeft())
                {
                    PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate + dir);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate + dir);
                }
                else
                {
                    // play a bump into wall sound?
                }
            }
            else if (dir == Vector2Int.right)
            {
                if (CanMoveFuguPairRight())
                {
                    PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate + dir);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate + dir);
                }
                else
                {
                    // play a bump into wall sound?
                }
            }
            else if (dir == Vector2Int.down)
            {
                MakeFuguPairFallOneCell();
            }
        }
    }

    // Basically this function tells up if we're currently up against a wall of any kind
    bool CanMoveFuguPairLeft()
    {
        List<Vector2Int> leftCells = ActiveFreefallPair.primary.GetLeftCells();
        foreach (Vector2Int pos in leftCells)
        {
            // left cells are OOB
            if (pos.x < 0)
            {
                return false;
            }
            // left cell is something other than partner fugu
            if (grid[pos.x][pos.y] != -1 && grid[pos.x][pos.y] != ActiveFreefallPair.secondary.id)
            {
                return false;
            }
        }

        leftCells = ActiveFreefallPair.secondary.GetLeftCells();
        foreach (Vector2Int pos in leftCells)
        {
            // left cells are OOB
            if (pos.x < 0)
            {
                return false;
            }
            // left cell is something other than partner fugu
            if (grid[pos.x][pos.y] != -1 && grid[pos.x][pos.y] != ActiveFreefallPair.primary.id)
            {
                return false;
            }
        }

        return true;
    }
    bool CanMoveFuguPairRight()
    {
        List<Vector2Int> rightCells = ActiveFreefallPair.primary.GetRightCells();
        foreach (Vector2Int pos in rightCells)
        {
            // right cells are OOB
            if (pos.x >= GRID_SIZE)
            {
                return false;
            }
            // right cell is something other than partner fugu or -1
            if (grid[pos.x][pos.y] != -1 && grid[pos.x][pos.y] != ActiveFreefallPair.secondary.id)
            {
                return false;
            }
        }

        rightCells = ActiveFreefallPair.secondary.GetRightCells();
        foreach (Vector2Int pos in rightCells)
        {
            // right cells are OOB
            if (pos.x >= GRID_SIZE)
            {
                return false;
            }
            // right cell is something other than partner fugu
            if (grid[pos.x][pos.y] != -1 && grid[pos.x][pos.y] != ActiveFreefallPair.primary.id)
            {
                return false;
            }
        }

        return true;

    }

    // Update is called once per frame
    void Update()
    {
        HandlePlayerInput();

        // Decide how to reconcile player input that happens on the SAME frame as the tick!


        if (ActiveFreefallPair.IsEmpty())
        {
            if (fuguQueue.Count > 0)
            {
                FuguPair fuguPair = fuguQueue.Dequeue();
                ActiveFreefallPair.SetFuguPair(fuguPair);
                PlaceFuguInGrid(fuguPair.primary, fuguPair.primary.bottomLeftCoordinate);
                PlaceFuguInGrid(fuguPair.secondary, fuguPair.secondary.bottomLeftCoordinate);
            }
            else
            {
                // WIN / LOSE CONDITION!!!!
                Debug.LogError("YOU LOSE YOU LOSE YOU LOSE");
            }
        }


        // Do gravity tick check.
        gravityTickTimer += Time.deltaTime;
        if (gravityTickTimer > freeFallTickPeriod)
        {
            // 1 tick has occurred! Move the active free-fall pair. 
            MakeFuguPairFallOneCell();
            ///////MakeFuguPairFallOneCell();

            // if the lowest point of the current active pair is resting upon something else, do a few things:
            // 1 - make dangling pieces fall
            // 2 - once all pieces have landed, visually connect everything that needs connecting
            // 3 - evaluate, disintegrating if needed. If disintegrated, then return to step 1 and repeat!


            // Check for landing... (also give a grace period if the player drags off onto another gap, for example

            if (IsFuguPairLanded())
            {
                ////////StartCoroutine(FuguLandingDelayStep());
            }

            gravityTickTimer = 0.0f;
        }


        ////StartCoroutine();

    }

    private bool IsFuguPairLanded()
    {
        // check if primary landed
        List<Vector2Int> belowCells = ActiveFreefallPair.primary.GetBelowCells();
        foreach (Vector2Int pos in belowCells)
        {
            // below cells are OOB
            if (pos.y < 0)
            {
                return true;
            }
            // below cell is something other than partner fugu
            if (grid[pos.x][pos.y] != -1 && grid[pos.x][pos.y] != ActiveFreefallPair.secondary.id)
            {
                return true;
            }
        }

        // check if secondary landed
        belowCells = ActiveFreefallPair.secondary.GetBelowCells();
        foreach (Vector2Int pos in belowCells)
        {
            // below cells are OOB
            if (pos.y < 0)
            {
                return true;
            }
            // below cell is something other than partner fugu
            if (grid[pos.x][pos.y] != -1 &&grid[pos.x][pos.y] != ActiveFreefallPair.primary.id)
            {
                return true;
            }
        }

        return false;
    }

    private void MakeFuguPairFallOneCell()
    {
        PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate + Vector2Int.down);
        PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate + Vector2Int.down);
    }

    private void PlaceFuguInGrid(FuguController fugu, Vector2Int bottomLeftCoordinate)
    {
        // If the fugu was previously in the grid, clear its id from previous cells.

        // Place the fugu's id in the current cells.
        List<Vector2Int> previousCells = fugu.GetAllCells();
        for (int i = 0; i < previousCells.Count; i++)
        {
            // Checking if there actually previously was this fugu's id here allows us to safely
            // write other fugus to this previously occupied cell, without overriding it.
            if (grid[previousCells[i].x][previousCells[i].y] == fugu.id)
            {
                grid[previousCells[i].x][previousCells[i].y] = -1;
            }
        }

        // Set the fugu's new position and scale.
        fugu.SetGridPosition(bottomLeftCoordinate);
        fugu.SetScale();
        fugu.SetColor();
        List<Vector2Int> currentCells = fugu.GetAllCells();
        for (int i = 0; i < currentCells.Count; i++)
        {
            grid[currentCells[i].x][currentCells[i].y] = fugu.id;
        }
    }

    public float FuguLandingDelay = 0.3f;
    IEnumerator FuguLandingDelayStep()
    {
        // Give final buffer for input
        float temp = freeFallTickPeriod;
        freeFallTickPeriod = 99999f;

        yield return new WaitForSeconds(FuguLandingDelay);
        // Reset the tick period back.
        freeFallTickPeriod = temp;

        // Remove the player's active pair
        ActiveFreefallPair.Disconnect();

        yield return FugusFallStep();
    }

    public float FugusFallStepDelay = 0.1f;
    // Make all fugus fall downward 1 cell per iteration.
    IEnumerator FugusFallStep()
    {
        yield return new WaitForSeconds(FugusFallStepDelay);

        // if not all fugus are at rest, do it again
        if (false)
        {
            yield return FugusFallStep();
        }
        else
        {
            yield return ConnectFugusStep();
        }
    }
    public float ConnectFugusStepDelay = 0.1f;
    // Visually connect adjacent fugus with the same color.

    private Queue<FuguController> fugusMarkedForExploding = new Queue<FuguController>();

    // TODO: finish this! Work in progress (like many many other places)
    IEnumerator ConnectFugusStep()
    {
        // Run code to visually connect all fugus that need connecting...
        // Also mark them for exploding!

        // fugu flood fill...
        // 1. iterate over all fugus in grid and add to a unique set at each starting point
        //    a. if visited, continue...
        //    b. otherwise, check all adjacent cells
        //       1. if adacent matches color, add to the set

        // For marking fugus as visited specifically when it's the iteration step, or we explore the matching color.
        HashSet<int> visitedFugus = new HashSet<int>();
        List<HashSet<int>> connectedSets = new List<HashSet<int>>();


        foreach (KeyValuePair<int, FuguController> p in fuguDict)
        {
            // skip if visited already
            if (visitedFugus.Contains(p.Key))
            {
                continue;
            }
            FuguController fugu = p.Value;
            visitedFugus.Add(fugu.id);

            // Explore all color matching fugus from here once.
            HashSet<int> connectedFugus = new HashSet<int>();
            ExploreConnectedFugus(fugu, visitedFugus, connectedFugus, new HashSet<int>(), fugu.color);
            if (connectedFugus.Count >= 4)
            {
                connectedSets.Add(connectedFugus);
            }
        }

        

        yield return new WaitForSeconds(ConnectFugusStepDelay);

        // Set visuall connections between fugus here...

    }

    private void ExploreConnectedFugus(FuguController fugu, HashSet<int> visitedFugus, HashSet<int> connectedFugus, HashSet<int> localVisited, FuguColor color)
    {
        // Mark a visited for future iterations.
        visitedFugus.Add(fugu.id);
        // If we enter here, we add it to the current connected-by-color set.
        connectedFugus.Add(fugu.id);

        // get unique set of fugus to explore from adjacent cells (could be multiple cells of same fugu)
        HashSet<int> nextToVisit = new HashSet<int>();
        List<Vector2Int> adjacentCells = fugu.GetAllAdjacentCells();
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            Vector2Int pos = adjacentCells[i];
            nextToVisit.Add(grid[pos.x][pos.y]);
        }

        foreach (int id in nextToVisit)
        {
            if (visitedFugus.Contains(id))
            {
                continue;
            }
            FuguController nextFugu = fuguDict[id];
            // skip if wrong color, or already visited
            if (nextFugu.color != color)
            {
                continue;
            }
            ExploreConnectedFugus(nextFugu, visitedFugus, connectedFugus, localVisited, color);
        }
    }

    // Explode every individual fugu. The original game seems to have no particular order for this! Perhaps slighly vary the scheduling
    // of each animation and keep the explosion timer the same no matter how many?
    public float ExplodeFugusDelay = 0.8f;
    IEnumerator ExplodeFugus()
    {
        bool isOneOrMoreExplosions = fugusMarkedForExploding.Count > 0;
        while (fugusMarkedForExploding.Count > 0)
        {
            FuguController fugu = fugusMarkedForExploding.Dequeue();
            RemoveFuguFromGrid(fugu);
        }

        yield return new WaitForSeconds(ExplodeFugusDelay);

        // And the cycle restarts...
        // Here we need to make everything QUICKLY fall on loop until all is settled.
        if (isOneOrMoreExplosions)
        {
            // do quickfall
        }
    }


    void RemoveFuguFromGrid(FuguController fugu)
    {
        fuguDict.Remove(fugu.id);
        fugu.ExplodeSelf();
    }

}
