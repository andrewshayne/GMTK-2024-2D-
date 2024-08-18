using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;



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
        SetUpFuguController(secondary, false, id2, id1, position, secondaryColor);
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
    public bool InflatePrimary()
    {
        // Inflate if it's smaller than a large fugu.
        if (primary.scale < FuguScale.Large)
        {
            primary.scale++;
            secondary.scale--;

            primary.SetScale();
            secondary.SetScale();

            switch (primary.relativePosition)
            {
                case RelativePosition.Up:
                    // Move the secondary right 1
                    secondary.bottomLeftCoordinate += Vector2Int.up;
                    break;
                case RelativePosition.Right:
                    // Move the secondary up 1
                    secondary.bottomLeftCoordinate += Vector2Int.up;
                    break;
                case RelativePosition.Down:
                    // Move the secondary up 1, right 1
                    secondary.bottomLeftCoordinate += Vector2Int.up + Vector2Int.right;
                    break;
                case RelativePosition.Left:
                    // Move the secondary up 1, right 1
                    secondary.bottomLeftCoordinate += Vector2Int.up + Vector2Int.right;
                    break;
            }
            return true;
        }
        else
        {
            // Play a sound indicating this action is unavailable.
            return false;
        }
    }

    // Inflate the secondary fugu, deflate the secondary.
    public bool InflateSecondary()
    {
        // Inflate if it's smaller than a large fugu.
        if (secondary.scale < FuguScale.Large)
        {
            primary.scale--;
            secondary.scale++;

            primary.SetScale();
            secondary.SetScale();

            switch (secondary.relativePosition)
            {
                case RelativePosition.Up:
                    // Move the secondary left 1, down 1.
                    secondary.bottomLeftCoordinate += Vector2Int.left + Vector2Int.down;
                    break;
                case RelativePosition.Right:
                    // Move the secondary left 1, down 1.
                    secondary.bottomLeftCoordinate += Vector2Int.left + Vector2Int.down;
                    break;
                case RelativePosition.Down:
                    // Move the secondary left 1. Primary up 1.
                    secondary.bottomLeftCoordinate += Vector2Int.left;
                    primary.bottomLeftCoordinate += Vector2Int.up;
                    break;
                case RelativePosition.Left:
                    // Move the secondary down 1. Primary right 1.
                    secondary.bottomLeftCoordinate += Vector2Int.down;
                    primary.bottomLeftCoordinate += Vector2Int.right;
                    break;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    // Rotates the fugu pair and returns (primary.bottomLeftCoordinate, secondary.bottomLeftCoordinate)
    public KeyValuePair<Vector2Int, Vector2Int> Rotate(bool isClockwise)
    {
        if (primary.scale == secondary.scale)
        {
            return RotateAroundPairCenter(isClockwise);
        } else
        {
            return RotateAroundPrimaryCenter(isClockwise);
        }
    }

    private KeyValuePair<Vector2Int, Vector2Int> RotateAroundPrimaryCenter(bool isClockwise)
    {
        Vector2Int primaryCenter = primary.GetCenterCoord();

        Vector2Int newSecondaryBottomLeftCoordinate = RotatePointAroundPoint(primaryCenter, secondary.bottomLeftCoordinate, isClockwise);

        return new KeyValuePair<Vector2Int, Vector2Int>(primary.bottomLeftCoordinate, newSecondaryBottomLeftCoordinate);
    }

    private KeyValuePair<Vector2Int, Vector2Int> RotateAroundPairCenter(bool isClockwise)
    {
        //Debug.Log($"primary Scale: {primary.scale}, secondary Scale: {secondary.scale}");
        Vector2Int primaryCenter = primary.GetCenterCoord();
        Vector2Int secondaryCenter = secondary.GetCenterCoord();
        Vector2Int rotationPoint = GetCenter(primaryCenter, secondaryCenter);
        Vector2Int newPrimaryBottomLeft = RotatePointAroundPoint(rotationPoint, primary.bottomLeftCoordinate, isClockwise);
        Vector2Int newSecondaryBottomLeft = RotatePointAroundPoint(rotationPoint, secondary.bottomLeftCoordinate, isClockwise);
        //Debug.Log($"isClockwise: {isClockwise}, primaryCenter: {primaryCenter}, secondaryCenter: {secondaryCenter}");
        //Debug.Log($"rotationPoint: {rotationPoint}, primaryBLC: {primary.bottomLeftCoordinate}, secondaryBLC: {secondary.bottomLeftCoordinate}");
        //Debug.Log($"newPrimaryBLC: {newPrimaryBottomLeft}, newSecondaryBLC: {newSecondaryBottomLeft}");
        return new KeyValuePair<Vector2Int, Vector2Int>(newPrimaryBottomLeft, newSecondaryBottomLeft);
    }

    // Updates the relative positions of the primary and secondary Fugus based on rotations
    public void UpdateFuguRelativePositions(bool isClockwise)
    {
        //Debug.Log($"UpdateFuguRelativePositions primary: {primary.relativePosition} secondary: {secondary.relativePosition}");
        secondary.relativePosition = RotateRelativePosition(secondary.relativePosition, isClockwise);
        primary.relativePosition = RotateRelativePosition(primary.relativePosition, isClockwise);
        //Debug.Log($"new primary: {primary.relativePosition} secondary: {secondary.relativePosition}");
    }

    // Returns the relative position after the rotation
    private RelativePosition RotateRelativePosition(RelativePosition relativePosition, bool isClockwise)
    {
        if (isClockwise)
        {
            return (RelativePosition)((((int)relativePosition + 1) % 4 + 4) %4);
        } else
        {
            return (RelativePosition)((((int)relativePosition - 1) % 4 + 4) %4);
        }
    }

    private Vector2Int GetCenter(Vector2Int pointA, Vector2Int pointB)
    {
        return new Vector2Int((pointA.x + pointB.x) / 2, (pointA.y + pointB.y) / 2);
    }

    private Vector2Int RotatePointAroundPoint(Vector2Int rotationPoint, Vector2Int point, bool isClockwise)
    {
        if (!isClockwise)
        {
            // Calculation: x = (point.x - rotationPoint.x)*cos(90) - (point.y - rotationPoint.y)*sin(90) + rotationPoint.x
            // Simplifies to --> x = rotationPoint.y - point.y + rotationPoint.x
            int x = rotationPoint.y - point.y + rotationPoint.x;
            // Calculation: y = (point.y - rotationPoint.y)*cos(90) + (point.x - rotationPoint.x)*sin(90) + rotationPoint.y
            // Simplifies to --> y = point.x - rotationPoint.x + rotationPoint.y
            int y = point.x - rotationPoint.x + rotationPoint.y;
            return new Vector2Int(x, y);
        }
        else
        {
            // Calculation: x = (point.x - rotationPoint.x)*cos(-90) - (point.y - rotationPoint.y)*sin(-90) + rotationPoint.x
            // Simplifies to --> x = point.y - rotationPoint.y + rotationPoint.x
            int x = point.y - rotationPoint.y + rotationPoint.x;
            // Calculation: y = (point.y - rotationPoint.y)*cos(-90) + (point.x - rotationPoint.x)*sin(-90) + rotationPoint.y
            // Simplifies to --> x = rotationPoint.x - point.x + rotationPoint.y
            int y = rotationPoint.x - point.x + rotationPoint.y;
            return new Vector2Int(x, y);
        }
    }
}



public class GridController : MonoBehaviour
{
    Vector2Int GRID_SIZE = new Vector2Int(14, 16);

    private int currentID = 0;
    private Vector2Int defaultSpawnPosition = new Vector2Int(6, 15);

    private float gravityTickTimer = 0.0f;
    public float freeFallTickPeriod = 2.0f;

    public FuguPair ActiveFreefallPair;

    // grid holds fugu ids
    public int[][] grid;

    public Queue<FuguPair> fuguQueue = new Queue<FuguPair>();
    public Dictionary<int, FuguController> fuguDict = new Dictionary<int, FuguController>();
    public GameObject FuguPrefab;
    public GameObject DebuggingSquare;

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

        // Debugging
        InitDebuggingGrid();
    }

    void InitGrid()
    {
        grid = new int[GRID_SIZE.x][];
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            grid[i] = new int[GRID_SIZE.y];
            for (int j = 0; j < GRID_SIZE.y; j++)
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

        int id5 = GetUniqueID();
        int id6 = GetUniqueID();
        int id7 = GetUniqueID();
        int id8 = GetUniqueID();

        int id9 = GetUniqueID();
        int id10 = GetUniqueID();

        FuguPair a = new FuguPair(FuguColor.Red, FuguColor.Green, id1, id2, defaultSpawnPosition, FuguPrefab);
        FuguPair b = new FuguPair(FuguColor.Red, FuguColor.Cyan, id3, id4, defaultSpawnPosition, FuguPrefab);
        FuguPair c = new FuguPair(FuguColor.Red, FuguColor.Pink, id5, id6, defaultSpawnPosition, FuguPrefab);
        FuguPair d = new FuguPair(FuguColor.Red, FuguColor.Purple, id7, id8, defaultSpawnPosition, FuguPrefab);
        FuguPair e = new FuguPair(FuguColor.Yellow, FuguColor.Cyan, id9, id10, defaultSpawnPosition, FuguPrefab);

        AddPairToDict(a);
        AddPairToDict(b);
        AddPairToDict(c);
        AddPairToDict(d);
        AddPairToDict(e);

        fuguQueue.Enqueue(a);
        fuguQueue.Enqueue(b);
        fuguQueue.Enqueue(c);
        fuguQueue.Enqueue(d);
        fuguQueue.Enqueue(e);
    }

    public void AddPairToDict(FuguPair fuguPair)
    {
        fuguDict[fuguPair.primary.id] = fuguPair.primary;
        fuguDict[fuguPair.secondary.id] = fuguPair.secondary;
    }

    Vector2Int GetPlayerMovementInput()
    {
        Vector2Int dir = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.LeftArrow))// || Input.GetKeyDown(KeyCode.A))
        {
            dir = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))// || Input.GetKeyDown(KeyCode.D))
        {
            dir = Vector2Int.right;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))// || Input.GetKeyDown(KeyCode.S))
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
        if (Input.GetKeyDown(KeyCode.E))
        {
            return ActionInput.RotateCW;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            return ActionInput.RotateCCW;
        }

        // I know this is horrible. But trust.
        if (!ActiveFreefallPair.IsEmpty())
        {
            // inflation via WASD
            RelativePosition relP = ActiveFreefallPair.primary.relativePosition;
            RelativePosition relS = ActiveFreefallPair.secondary.relativePosition;

            bool isValid = false;
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (relP == RelativePosition.Up)
                {
                    isValid = ActiveFreefallPair.InflatePrimary();
                }
                else if (relS == RelativePosition.Up)
                {
                    isValid = ActiveFreefallPair.InflateSecondary();
                }
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (relP == RelativePosition.Left)
                {
                    isValid = ActiveFreefallPair.InflatePrimary();
                }
                else if (relS == RelativePosition.Left)
                {
                    isValid = ActiveFreefallPair.InflateSecondary();
                }
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (relP == RelativePosition.Down)
                {
                    isValid = ActiveFreefallPair.InflatePrimary();
                }
                else if (relS == RelativePosition.Down)
                {
                    isValid = ActiveFreefallPair.InflateSecondary();
                }
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (relP == RelativePosition.Right)
                {
                    isValid = ActiveFreefallPair.InflatePrimary();
                }
                else if (relS == RelativePosition.Right)
                {
                    isValid = ActiveFreefallPair.InflateSecondary();
                }
            }
            if (isValid)
            {
                DesperateFuguGridCleanup(ActiveFreefallPair.primary.id);
                DesperateFuguGridCleanup(ActiveFreefallPair.secondary.id);

                PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate);
                PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate);
            }
        }

        return ActionInput.NoAction;
    }

    // Call this function if you just need to wipe the grid of a specific fugu id.
    // It scans the whole grid but who cares. I just want to clear this id.
    void DesperateFuguGridCleanup(int id)
    {
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                if (grid[i][j] == id)
                {
                    grid[i][j] = -1;
                }
            }
        }
    }

    void HandlePlayerInput()
    {
        ActionInput actionInput = GetPlayerActionInput();
        bool isValid = false;

        switch (actionInput)
        {
            case ActionInput.RotateCW:
                //Debug.Log($"CW primary coord: {ActiveFreefallPair.primary} secondary coord: {ActiveFreefallPair.secondary}");
                KeyValuePair<Vector2Int, Vector2Int> newCWCoords = ActiveFreefallPair.Rotate(isClockwise: true);
                //Debug.Log($"CW primary coord: {newCWCoords.Key} secondary coord: {newCWCoords.Value}");
                if (CanMoveFuguPairToCoords(primaryBottomLeft: newCWCoords.Key, secondaryBottomLeft: newCWCoords.Value))
                {
                    ActiveFreefallPair.UpdateFuguRelativePositions(isClockwise: true);
                    PlaceFuguInGrid(ActiveFreefallPair.primary, newCWCoords.Key);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, newCWCoords.Value);
                }
                break;
            case ActionInput.RotateCCW:
                //Debug.Log($"CCW primary coord: {ActiveFreefallPair.primary} secondary coord: {ActiveFreefallPair.secondary}");
                KeyValuePair<Vector2Int, Vector2Int> newCCWCoords = ActiveFreefallPair.Rotate(isClockwise: false);
                //Debug.Log($"CCW after primary coord: {newCCWCoords.Key} secondary coord: {newCCWCoords.Value}");
                if (CanMoveFuguPairToCoords(primaryBottomLeft: newCCWCoords.Key, secondaryBottomLeft: newCCWCoords.Value))
                {
                    ActiveFreefallPair.UpdateFuguRelativePositions(isClockwise: false);
                    PlaceFuguInGrid(ActiveFreefallPair.primary, newCCWCoords.Key);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, newCCWCoords.Value);
                }
                break;
            case ActionInput.InflatePrimary:
                // Handled when getting the action. Empty case.
                break;
            case ActionInput.InflateSecondary:
                // Handled when getting the action. Empty case.
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
                // Do fast drop!!! Call the quick drop with 0 delay.
                // But do it without the existing quick drop function (which leads to other events)
            }
        }
    }

    private bool CanMoveFuguPairToCoords(Vector2Int primaryBottomLeft, Vector2Int secondaryBottomLeft) 
    {        
        int primaryScale = (int)ActiveFreefallPair.primary.scale;
        int secondaryScale = (int)ActiveFreefallPair.secondary.scale;
        
        Vector2Int primaryTopRight = new Vector2Int(primaryBottomLeft.x + primaryScale, primaryBottomLeft.y + primaryScale);
        Vector2Int secondaryTopRight = new Vector2Int(secondaryBottomLeft.x + secondaryScale, secondaryBottomLeft.y + secondaryScale);
        
        // Check if new primary and secondary bottom left and top right are within the grid
        if (isOOB(primaryBottomLeft) || isOOB(primaryTopRight) || isOOB(secondaryBottomLeft) || isOOB(secondaryTopRight))
        {
            Debug.Log($"FuguPair new coords out of bounds, primaryBottomLeft: {primaryBottomLeft}, primaryTopRight: {primaryTopRight}, secondaryBottomLeft: {secondaryBottomLeft}, secondaryTopRight: {secondaryTopRight}");
            return false;
        }
        
        // Check if any of the new primary coords are already occupied
        for (int i = primaryBottomLeft.x; i < primaryTopRight.x; i++)
        {
            for (int j = primaryBottomLeft.y; j < primaryTopRight.y; j++)
            {
                if (grid[i][j] != -1 && grid[i][j] != ActiveFreefallPair.primary.id && grid[i][j] != ActiveFreefallPair.secondary.id)
                {
                    Debug.Log($"FuguPair primary (id: {ActiveFreefallPair.primary.id} new coords collision with id {grid[i][j]}");
                    return false;
                }
            }
        }

        // Check if any of the new secondary coords are already occupied
        for (int i = secondaryBottomLeft.x; i < secondaryTopRight.x; i++)
        {
            for (int j = secondaryBottomLeft.y; j < secondaryTopRight.y; j++)
            {
                if (grid[i][j] != -1 && grid[i][j] != ActiveFreefallPair.secondary.id && grid[i][j] != ActiveFreefallPair.primary.id)
                {
                    Debug.Log($"FuguPair secondary (id: {ActiveFreefallPair.secondary.id} new coords collision with id {grid[i][j]}");
                    return false;
                }
            }
        }
        return true;
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
            if (pos.x >= GRID_SIZE.x)
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
            if (pos.x >= GRID_SIZE.x)
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

    bool isPlayerLocked = false;

    // Update is called once per frame
    void Update()
    {
        DebuggingRenderGrid();

        if (isPlayerLocked)
        {
            return;
        }
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

            // if the lowest point of the current active pair is resting upon something else, do a few things:
            // 1 - make dangling pieces fall
            // 2 - once all pieces have landed, visually connect everything that needs connecting
            // 3 - evaluate, disintegrating if needed. If disintegrated, then return to step 1 and repeat!


            // Check for landing... (also give a grace period if the player drags off onto another gap, for example

            if (IsFuguPairLanded())
            {
                StartCoroutine(FuguLandingDelayStep());
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

    private void ClearPreviousFuguPosition(FuguController fugu, Vector2Int bottomLeftCoordinate)
    {
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
    }

    private void PlaceFuguInGrid(FuguController fugu, Vector2Int bottomLeftCoordinate)
    {
        // If the fugu was previously in the grid, clear its id from previous cells.
        ClearPreviousFuguPosition(fugu, bottomLeftCoordinate);

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

        // Lock the player's input
        isPlayerLocked = true;

        yield return FugusQuickFallStep();
    }

    // FugusQuickfallStep should repeatedly call itself until all fugus are at rest.
    // Start from the lowest fugus, moving up to the highest.
    public float FugusQuickFallStepDelay = 0.1f;
    IEnumerator FugusQuickFallStep()
    {
        List<FuguController> allFugus = new List<FuguController>();
        foreach(var f in fuguDict)
        {
            allFugus.Add(f.Value);
        }

        // Sort all the fugus by height. Lowest first.
        allFugus.Sort(delegate (FuguController f1, FuguController f2) {
            return f1.bottomLeftCoordinate.y.CompareTo(f1.bottomLeftCoordinate.y);
        });

        yield return new WaitForSeconds(FugusQuickFallStepDelay);

        bool atLeastOneFell = false;

        foreach (FuguController fugu in allFugus)
        {
            List<Vector2Int> belowCells = new List<Vector2Int>();
            foreach (Vector2Int belowCell in belowCells)
            {
                // OOB, noop
                if (isOOB(belowCell))
                {
                    continue;
                }
                // something blocking, noop
                if (grid[belowCell.x][belowCell.y] != -1)
                {
                    continue;
                }
                // otherwise move down 1 cell
                PlaceFuguInGrid(fugu, fugu.bottomLeftCoordinate + Vector2Int.down);
                atLeastOneFell = true;
            }
        }

        // if not all fugus are at rest, do it again
        if (atLeastOneFell)
        {
            yield return FugusQuickFallStep();
        }
        else
        {
            yield return ConnectFugusStep();
        }
    }
    public float ConnectFugusStepDelay = 0.1f;
    // Visually connect adjacent fugus with the same color.

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

        yield return ExplodeFugusStep(connectedSets);

        // Set visual connections between fugus here...

    }

    private bool isOOB(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= GRID_SIZE.x || pos.y >= GRID_SIZE.y)
        {
            return true;
        }
        return false;
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
            // skip oob cells
            Vector2Int pos = adjacentCells[i];
            if (isOOB(pos))
            {
                continue;
            }
            nextToVisit.Add(grid[pos.x][pos.y]);
        }

        foreach (int id in nextToVisit)
        {
            if (visitedFugus.Contains(id))
            {
                continue;
            }
            if (id == -1)
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
    IEnumerator ExplodeFugusStep(List<HashSet<int>> connectedSets)
    {
        bool isOneOrMoreExplosions = connectedSets.Count > 0;
        foreach(HashSet<int> fuguIDs in connectedSets)
        {
            foreach (int id in fuguIDs)
            {
                // Check if it's in the dict, because it may have just been removed by a previous iteration.
                if (fuguDict.ContainsKey(id))
                {
                    FuguController fugu = fuguDict[id];
                    RemoveFuguFromGrid(fugu);
                }
            }
        }

        yield return new WaitForSeconds(ExplodeFugusDelay);

        // And the cycle restarts...
        // Here we need to make everything QUICKLY fall on loop until all is settled.
        if (isOneOrMoreExplosions)
        {
            // do quickfall
            yield return FugusQuickFallStep();
        }
        else
        {
            FuguPair nextFuguPair = fuguQueue.Dequeue();
            ActiveFreefallPair.SetFuguPair(nextFuguPair);

            isPlayerLocked = false;
        }
    }

    void RemoveFuguFromGrid(FuguController fugu)
    {
        fuguDict.Remove(fugu.id);
        fugu.ExplodeSelf();
    }


    // Debugging fugu / cell tool.
    private bool enableDebuggingGrid = true;
    private List<List<GameObject>> debuggingGrid;
    void InitDebuggingGrid()
    {
        if (!enableDebuggingGrid)
        {
            return;
        }
        debuggingGrid = new List<List<GameObject>>();
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            debuggingGrid.Add(new List<GameObject>());
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                debuggingGrid[i].Add(GameObject.Instantiate(DebuggingSquare));
                debuggingGrid[i][j].transform.localPosition = new Vector2(i, j) + (Vector2.one * 0.5f);
            }
        }
    }

    void DebuggingRenderGrid ()
    {
        if (!enableDebuggingGrid)
        {
            return;
        }
        if (debuggingGrid.Count == GRID_SIZE.x && debuggingGrid[0].Count == GRID_SIZE.y)
        {
            for (int i = 0; i < GRID_SIZE.x; i++)
            {
                for (int j = 0; j < GRID_SIZE.y; j++)
                {
                    bool isActive = grid[i][j] != -1;
                    debuggingGrid[i][j].SetActive(isActive);
                }
            }
        }
    }
}
