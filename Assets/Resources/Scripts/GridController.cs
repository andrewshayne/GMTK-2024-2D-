using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine.Purchasing;
using Unity.VisualScripting;
using static GridState;



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

    public FuguPair(FuguColor primaryColor, FuguColor secondaryColor, Vector2Int position, GameObject FuguPrefab)
    {
        GameObject primaryObj = GameObject.Instantiate(FuguPrefab);
        GameObject secondaryObj = GameObject.Instantiate(FuguPrefab);
        primary = primaryObj.GetComponent<FuguController>();
        secondary = secondaryObj.GetComponent<FuguController>();

        SetUpFuguController(primary, true, position + (Vector2Int.down * 2), primaryColor);
        SetUpFuguController(secondary, false, position, secondaryColor);
    }

    public void SetUpFuguController(FuguController fugu, bool isPrimary, Vector2Int topLeftCoordinate, FuguColor color)
    {
        fugu.isPrimary = isPrimary;
        fugu.id = -2; // This should ALWAYS get overwritten when adding fugu to puzzle.
        fugu.bottomLeftCoordinate = topLeftCoordinate + (Vector2Int.down * 1); // Offset = 1 because 2x2 fugu's top left corner is 1 away from its bottom left corner.
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

        // make sure local scale is set back to 1
        this.primary.transform.localScale = Vector2.one;
        this.secondary.transform.localScale = Vector2.one;

        this.primary.transform.SetParent(null);
        this.secondary.transform.SetParent(null);
    }

    public void Disconnect()
    {
        // Trigger fugu animation to exit the kissy stage!
        primary.DetachFromPair();
        secondary.DetachFromPair();
        this.primary = null;
        this.secondary = null;
    }

    // Inflate the primary fugu, deflate the secondary.
    public bool Inflate(bool inflatePrimary)
    {
        FuguController inflateMe;
        FuguController deflateMe;
        if (inflatePrimary)
        {
            inflateMe = primary;
            deflateMe = secondary;
        }
        else
        {
            inflateMe = secondary;
            deflateMe = primary;
        }

        // Inflate if it's smaller than a large fugu.
        if (inflateMe.scale < FuguScale.Large)
        {
            inflateMe.scale++;
            deflateMe.scale--;
            inflateMe.SetScaleVisuals();
            deflateMe.SetScaleVisuals();

            if (inflateMe.scale == FuguScale.Medium)
            {
                switch (inflateMe.relativePosition)
                {
                    case RelativePosition.Up:
                        inflateMe.bottomLeftCoordinate += Vector2Int.left;
                        inflateMe.bottomLeftCoordinate += Vector2Int.down;
                        break;
                    case RelativePosition.Right:
                        inflateMe.bottomLeftCoordinate += Vector2Int.left;
                        inflateMe.bottomLeftCoordinate += Vector2Int.down;
                        break;
                    case RelativePosition.Down:
                        inflateMe.bottomLeftCoordinate += Vector2Int.left;
                        deflateMe.bottomLeftCoordinate += Vector2Int.up;
                        break;
                    case RelativePosition.Left:
                        inflateMe.bottomLeftCoordinate += Vector2Int.down;
                        deflateMe.bottomLeftCoordinate += Vector2Int.right;
                        break;
                }
            }
            else if (inflateMe.scale == FuguScale.Large)
            {
                switch (inflateMe.relativePosition)
                {
                    case RelativePosition.Up:
                        inflateMe.bottomLeftCoordinate += Vector2Int.down;
                        deflateMe.bottomLeftCoordinate += Vector2Int.right;
                        break;
                    case RelativePosition.Right:
                        inflateMe.bottomLeftCoordinate += Vector2Int.left;
                        deflateMe.bottomLeftCoordinate += Vector2Int.up;
                        break;
                    case RelativePosition.Down:
                        deflateMe.bottomLeftCoordinate += Vector2Int.right;
                        deflateMe.bottomLeftCoordinate += Vector2Int.up;
                        break;
                    case RelativePosition.Left:
                        deflateMe.bottomLeftCoordinate += Vector2Int.right;
                        deflateMe.bottomLeftCoordinate += Vector2Int.up;
                        break;
                }
            }
            return true;
        }
        else
        {
            // Play a sound indicating this action is unavailable.
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
            return RotateAroundBigFuguCenter(isClockwise);
        }
    }

    private KeyValuePair<Vector2Int, Vector2Int> RotateAroundBigFuguCenter(bool isClockwise)
    {
        if (primary.scale > secondary.scale)
        {
            Vector2Int primaryCenter = primary.GetCenterCoord();

            Vector2Int newSecondaryBottomLeftCoordinate = RotatePointAroundPoint(primaryCenter, secondary.bottomLeftCoordinate, isClockwise);

            return new KeyValuePair<Vector2Int, Vector2Int>(primary.bottomLeftCoordinate, newSecondaryBottomLeftCoordinate);
        } else
        {
            Vector2Int secondaryCenter = secondary.GetCenterCoord();

            Vector2Int newPrimaryBottomLeftCoordinate = RotatePointAroundPoint(secondaryCenter, primary.bottomLeftCoordinate, isClockwise);

            return new KeyValuePair<Vector2Int, Vector2Int>(newPrimaryBottomLeftCoordinate, secondary.bottomLeftCoordinate);
        }
    }

    private KeyValuePair<Vector2Int, Vector2Int> RotateAroundPairCenter(bool isClockwise)
    {
        Vector2Int primaryCenter = primary.GetCenterCoord();
        Vector2Int secondaryCenter = secondary.GetCenterCoord();
        Vector2Int rotationPoint = GetCenter(primaryCenter, secondaryCenter);
        Vector2Int newPrimaryBottomLeft = RotatePointAroundPoint(rotationPoint, primary.bottomLeftCoordinate, isClockwise);
        Vector2Int newSecondaryBottomLeft = RotatePointAroundPoint(rotationPoint, secondary.bottomLeftCoordinate, isClockwise);
        return new KeyValuePair<Vector2Int, Vector2Int>(newPrimaryBottomLeft, newSecondaryBottomLeft);
    }

    // Updates the relative positions of the primary and secondary Fugus based on rotations
    public void UpdateFuguRelativePositions(bool isClockwise)
    {
        secondary.relativePosition = RotateRelativePosition(secondary.relativePosition, isClockwise);
        primary.relativePosition = RotateRelativePosition(primary.relativePosition, isClockwise);
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

public struct  GridState
{
    public int[,] grid;

    public struct FuguState
    {
        public bool isPrimary;
        public int id;
        public FuguColor color;
        public FuguScale scale;
        public Vector2Int bottomLeftCoord;
        public RelativePosition relativePosition;
    }

    public FuguState PrimaryFugu;
    public FuguState SecondaryFugu;
    
    // Dictionary of all fugus in the grid with state
    public Dictionary<int, FuguState> fugusInGrid;

    public FuguState FuguToFuguState(FuguController fugu)
    {
        FuguState state = new FuguState();
        state.isPrimary = fugu.isPrimary;
        state.id = fugu.id;
        state.color = fugu.color;
        state.scale = fugu.scale;
        state.bottomLeftCoord = fugu.bottomLeftCoordinate;
        state.relativePosition = fugu.relativePosition;
        return state;
    }
}



public class GridController : MonoBehaviour
{
    Vector2Int GRID_SIZE = new Vector2Int(14, 16);

    private int currentID = 0;
    private Vector2Int defaultSpawnPosition = new Vector2Int(6, 15);
    private Vector2Int[] fuguQueuePositions = {
        new Vector2Int(0, 0), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(1, -1),
        new Vector2Int(0, -2), new Vector2Int(1, -2),
        new Vector2Int(0, -3), new Vector2Int(1, -3),
    };

    private float gravityTickTimer = 0.0f;
    public float freeFallTickPeriod = 2.0f;

    public FuguPair ActiveFreefallPair;

    // grid holds fugu ids
    public int[,] grid;

    public LinkedList<FuguPair> fuguQueue = new LinkedList<FuguPair>();
    public Dictionary<int, FuguController> fuguDict = new Dictionary<int, FuguController>();
    public Dictionary<int, FuguController> fuguMorgue = new Dictionary<int, FuguController>();

    public SFXManager sfxManager;

    public GameObject FuguPrefab;
    public GameObject DebuggingSquare;

    public GameObject RenderQueuePanel;
    public Camera camera;

    
    public Stack<GridState> History = new Stack<GridState>();

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
        fuguMorgue.Clear();
        // Clear history
        History.Clear();
    }

    // Start is called before the first frame update
    void Start()
    {
        camera.pixelRect = new Rect(0, 0, 1920, 1080);

        // load grid from file or something...
        InitGrid();
        InitQueue();

        // Save grid state to history

        // Debugging
        InitDebuggingGrid();

        FuguPair fuguPair = fuguQueue.First();
        fuguQueue.RemoveFirst();
        ActiveFreefallPair.SetFuguPair(fuguPair);
        SaveGridState(fuguPair);
        PlaceFuguInGrid(fuguPair.primary, fuguPair.primary.bottomLeftCoordinate);
        PlaceFuguInGrid(fuguPair.secondary, fuguPair.secondary.bottomLeftCoordinate);

        RenderFuguQueueAtStart();
    }

    void InitGrid()
    {
        grid = new int[GRID_SIZE.x, GRID_SIZE.y];
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                grid[i, j] = -1;
            }
        }
    }

    void InitQueue()
    {
        FuguPair a = new FuguPair(FuguColor.Red, FuguColor.Green, defaultSpawnPosition, FuguPrefab);
        FuguPair b = new FuguPair(FuguColor.Red, FuguColor.Cyan, defaultSpawnPosition, FuguPrefab);
        FuguPair c = new FuguPair(FuguColor.Red, FuguColor.Pink, defaultSpawnPosition, FuguPrefab);
        FuguPair d = new FuguPair(FuguColor.Red, FuguColor.Purple, defaultSpawnPosition, FuguPrefab);
        FuguPair e = new FuguPair(FuguColor.Yellow, FuguColor.Cyan, defaultSpawnPosition, FuguPrefab);

        AddFuguPairToPuzzle(a);
        AddFuguPairToPuzzle(b);
        AddFuguPairToPuzzle(c);
        AddFuguPairToPuzzle(d);
        AddFuguPairToPuzzle(e);
    }

    void AddFuguPairToPuzzle(FuguPair fuguPair)
    {
        // Set the id
        fuguPair.primary.id = GetUniqueID();
        fuguPair.secondary.id = GetUniqueID();

        // Set queue panel as parent
        fuguPair.primary.transform.SetParent(RenderQueuePanel.transform);
        fuguPair.secondary.transform.SetParent(RenderQueuePanel.transform);

        AddPairToDict(fuguPair);
        fuguQueue.AddLast(fuguPair);
    }

    public List<FuguController> GetAllFugusInGrid()
    {
        List<FuguController> fugus = new List<FuguController>();
        HashSet<int> ids = new HashSet<int>();
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                if (grid[i, j] == -1)
                {
                    continue;
                }
                else
                {
                    ids.Add(grid[i, j]);
                }
            }
        }
        foreach (int id in ids)
        {
            fugus.Add(fuguDict[id]);
        }
        return fugus;
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
        Undo,             // Undo move
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
        if (Input.GetKeyDown(KeyCode.Z))
        {
            return ActionInput.Undo;
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
                    isValid = ActiveFreefallPair.Inflate(true);
                }
                else if (relS == RelativePosition.Up)
                {
                    isValid = ActiveFreefallPair.Inflate(false);
                }
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (relP == RelativePosition.Left)
                {
                    isValid = ActiveFreefallPair.Inflate(true);
                }
                else if (relS == RelativePosition.Left)
                {
                    isValid = ActiveFreefallPair.Inflate(false);
                }
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (relP == RelativePosition.Down)
                {
                    isValid = ActiveFreefallPair.Inflate(true);
                }
                else if (relS == RelativePosition.Down)
                {
                    isValid = ActiveFreefallPair.Inflate(false);
                }
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (relP == RelativePosition.Right)
                {
                    isValid = ActiveFreefallPair.Inflate(true);
                }
                else if (relS == RelativePosition.Right)
                {
                    isValid = ActiveFreefallPair.Inflate(false);
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

    public void SaveGridState(FuguPair fuguPair)
    {
        GridState state = new GridState();

        // Copy grid
        int[,] copyGrid = new int[GRID_SIZE.x,GRID_SIZE.y];
        Dictionary<int, FuguState> fugusInGrid = new Dictionary<int, FuguState>();
        
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                int fuguId = grid[i,j];
                if (fuguId != -1 && !fugusInGrid.ContainsKey(fuguId))
                {
                    fugusInGrid.Add(fuguId, state.FuguToFuguState(fuguDict[fuguId]));
                }
                copyGrid[i, j] = fuguId;
            }
        }
        state.grid = copyGrid;

        // Fugu states of the ones in the grid
        state.fugusInGrid = fugusInGrid;

        // Save active fugu pair
        state.PrimaryFugu = state.FuguToFuguState(fuguPair.primary);
        state.SecondaryFugu = state.FuguToFuguState(fuguPair.secondary);

        // Push to the history stack
        History.Push(state);
    }

    public void PopGridState()
    {
        GridState state = History.Pop();
        HashSet<int> previousActiveFugus = new HashSet<int>();

        // Copy over the grid
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                grid[i,j] = state.grid[i,j];
                if (grid[i,j] != -1)
                {
                    previousActiveFugus.Add(grid[i, j]);
                }
            }
        }

        // Restore all fugus in morgue
        List<int> idsToActivate = new List<int>();
        foreach (int id in fuguMorgue.Keys)
        {
            if (state.fugusInGrid.ContainsKey(id))
            {
                FuguState fuguState = state.fugusInGrid[id];
                FuguController fugu = fuguMorgue[fuguState.id];

                fugu.scale = fuguState.scale;
                fugu.relativePosition = fuguState.relativePosition;
                fugu.bottomLeftCoordinate = fuguState.bottomLeftCoord;
                idsToActivate.Add(id);
            }
        }
        foreach (int id in idsToActivate)
        {
            fuguMorgue[id].gameObject.SetActive(true);
            fuguDict.Add(id, fuguMorgue[id]);
            fuguMorgue.Remove(id);
            fuguDict[id].Draw();
        }

        // Restore all fugus in grid
        List<int> idsToDeactivate = new List<int>();
        foreach (int id in fuguDict.Keys)
        {
            if (state.fugusInGrid.ContainsKey(id))
            {
                FuguState fuguState = state.fugusInGrid[id];
                FuguController fugu = fuguDict[fuguState.id];
                fugu.scale = fuguState.scale;
                fugu.relativePosition = fuguState.relativePosition;
                fugu.bottomLeftCoordinate = fuguState.bottomLeftCoord;
                fugu.Draw();
            }
            else
            {
                // WIP
                //idsToDeactivate.Add(id);
            }
        }

        foreach (int id in idsToDeactivate)
        {
            if (id != state.PrimaryFugu.id && id != state.SecondaryFugu.id)
            {
                fuguDict[id].gameObject.SetActive(false);
                fuguMorgue.Add(id, fuguDict[id]);
                fuguDict.Remove(id);
            }
        }

        FuguPair fuguPair = new FuguPair();
        
        // Restore primary fugu
        if (!state.PrimaryFugu.IsUnityNull())
        {
            fuguPair.primary = fuguDict[state.PrimaryFugu.id];
            fuguPair.primary.scale = state.PrimaryFugu.scale;
            fuguPair.primary.relativePosition = state.PrimaryFugu.relativePosition;
            fuguPair.primary.bottomLeftCoordinate = state.PrimaryFugu.bottomLeftCoord;
            fuguPair.primary.gameObject.SetActive(true);

            fuguPair.primary.Draw();
        }

        // Restore secondary fugu
        if (!state.SecondaryFugu.IsUnityNull())
        {
            fuguPair.secondary = fuguDict[state.SecondaryFugu.id];
            fuguPair.secondary.scale = state.SecondaryFugu.scale;
            fuguPair.secondary.relativePosition = state.SecondaryFugu.relativePosition;
            fuguPair.secondary.bottomLeftCoordinate = state.SecondaryFugu.bottomLeftCoord;
            fuguPair.secondary.gameObject.SetActive(true);

            fuguPair.secondary.Draw();
        }

        // Add active fugu pair to front of fuguQueue
        fuguQueue.AddFirst(fuguPair);
    }

    // Call this function if you just need to wipe the grid of a specific fugu id.
    // It scans the whole grid but who cares. I just want to clear this id.
    void DesperateFuguGridCleanup(int id)
    {
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                if (grid[i,j] == id)
                {
                    grid[i,j] = -1;
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
                KeyValuePair<Vector2Int, Vector2Int> newCWCoords = ActiveFreefallPair.Rotate(isClockwise: true);
                if (CanMoveFuguPairToCoords(primaryBottomLeft: newCWCoords.Key, secondaryBottomLeft: newCWCoords.Value))
                {
                    ActiveFreefallPair.UpdateFuguRelativePositions(isClockwise: true);
                    PlaceFuguInGrid(ActiveFreefallPair.primary, newCWCoords.Key);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, newCWCoords.Value);
                    sfxManager.PlayRotateSFX();
                }
                break;
            case ActionInput.RotateCCW:
                KeyValuePair<Vector2Int, Vector2Int> newCCWCoords = ActiveFreefallPair.Rotate(isClockwise: false);
                if (CanMoveFuguPairToCoords(primaryBottomLeft: newCCWCoords.Key, secondaryBottomLeft: newCCWCoords.Value))
                {
                    ActiveFreefallPair.UpdateFuguRelativePositions(isClockwise: false);
                    PlaceFuguInGrid(ActiveFreefallPair.primary, newCCWCoords.Key);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, newCCWCoords.Value);
                    sfxManager.PlayRotateSFX();
                }
                break;
            case ActionInput.Undo:
                if (History.Count() > 0)
                {
                    PopGridState();
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
                    sfxManager.PlayMoveSFX();
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
                    sfxManager.PlayMoveSFX();
                }
                else
                {
                    // play a bump into wall sound?
                }
            }
            else if (dir == Vector2Int.down)
            {
                if (CanMoveFuguPairDown())
                {
                    PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate + dir);
                    PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate + dir);
                    sfxManager.PlayMoveSFX();
                }
            }
        }
    }

    private bool CanMoveFuguPairToCoords(Vector2Int primaryBottomLeft, Vector2Int secondaryBottomLeft) 
    {        
        int primaryScale = (int)ActiveFreefallPair.primary.scale;
        int secondaryScale = (int)ActiveFreefallPair.secondary.scale;
        
        Vector2Int primaryTopRight = new Vector2Int(primaryBottomLeft.x + primaryScale - 1, primaryBottomLeft.y + primaryScale - 1);
        Vector2Int secondaryTopRight = new Vector2Int(secondaryBottomLeft.x + secondaryScale - 1, secondaryBottomLeft.y + secondaryScale - 1);
        
        // Check if new primary and secondary bottom left and top right are within the grid
        if (isOOB(primaryBottomLeft) || isOOB(primaryTopRight) || isOOB(secondaryBottomLeft) || isOOB(secondaryTopRight))
        {
            Debug.Log($"FuguPair new coords out of bounds, primaryBottomLeft: {primaryBottomLeft}, primaryTopRight: {primaryTopRight}, secondaryBottomLeft: {secondaryBottomLeft}, secondaryTopRight: {secondaryTopRight}");
            return false;
        }
        
        // Check if any of the new primary coords are already occupied
        for (int i = primaryBottomLeft.x; i <= primaryTopRight.x; i++)
        {
            for (int j = primaryBottomLeft.y; j <= primaryTopRight.y; j++)
            {
                if (grid[i,j] != -1 && grid[i,j] != ActiveFreefallPair.primary.id && grid[i,j] != ActiveFreefallPair.secondary.id)
                {
                    Debug.Log($"FuguPair primary (id: {ActiveFreefallPair.primary.id} new coords collision with id {grid[i,j]}");
                    return false;
                }
            }
        }

        // Check if any of the new secondary coords are already occupied
        for (int i = secondaryBottomLeft.x; i <= secondaryTopRight.x; i++)
        {
            for (int j = secondaryBottomLeft.y; j <= secondaryTopRight.y; j++)
            {
                if (grid[i,j] != -1 && grid[i,j] != ActiveFreefallPair.secondary.id && grid[i,j] != ActiveFreefallPair.primary.id)
                {
                    Debug.Log($"FuguPair secondary (id: {ActiveFreefallPair.secondary.id} new coords collision with id {grid[i,j]}");
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
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.secondary.id)
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
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.primary.id)
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
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.secondary.id)
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
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.primary.id)
            {
                return false;
            }
        }

        return true;

    }

    bool CanMoveFuguPairDown()
    {
        List<Vector2Int> belowCells = ActiveFreefallPair.primary.GetBelowCells();
        foreach (Vector2Int pos in belowCells)
        {
            // below cells are OOB
            if (pos.y < 0)
            {
                return false;
            }
            // below cell is something other than partner fugu or -1
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.secondary.id)
            {
                return false;
            }
        }

        belowCells = ActiveFreefallPair.secondary.GetBelowCells();
        foreach (Vector2Int pos in belowCells)
        {
            // below cells are OOB
            if (pos.y < 0)
            {
                return false;
            }
            // below cell is something other than partner fugu
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.primary.id)
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
        // Lerp each fugu pair to their position

        DebuggingRenderGrid();

        if (isPlayerLocked)
        {
            return;
        }
        HandlePlayerInput();

        // Decide how to reconcile player input that happens on the SAME frame as the tick!


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
            if (grid[pos.x,pos.y] != -1 && grid[pos.x,pos.y] != ActiveFreefallPair.secondary.id)
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
            if (grid[pos.x,pos.y] != -1 &&grid[pos.x,pos.y] != ActiveFreefallPair.primary.id)
            {
                return true;
            }
        }

        return false;
    }

    private void MakeFuguPairFallOneCell()
    {
        if (CanMoveFuguPairDown())
        {
            PlaceFuguInGrid(ActiveFreefallPair.primary, ActiveFreefallPair.primary.bottomLeftCoordinate + Vector2Int.down);
            PlaceFuguInGrid(ActiveFreefallPair.secondary, ActiveFreefallPair.secondary.bottomLeftCoordinate + Vector2Int.down);
        }
    }

    private void ClearPreviousFuguPosition(FuguController fugu, Vector2Int bottomLeftCoordinate)
    {
        // Place the fugu's id in the current cells.
        List<Vector2Int> previousCells = fugu.GetAllCells();
        for (int i = 0; i < previousCells.Count; i++)
        {
            // Checking if there actually previously was this fugu's id here allows us to safely
            // write other fugus to this previously occupied cell, without overriding it.
            if (grid[previousCells[i].x,previousCells[i].y] == fugu.id)
            {
                grid[previousCells[i].x, previousCells[i].y] = -1;
            }
        }
    }

    private void PlaceFuguInGrid(FuguController fugu, Vector2Int bottomLeftCoordinate)
    {
        // If the fugu was previously in the grid, clear its id from previous cells.
        ClearPreviousFuguPosition(fugu, bottomLeftCoordinate);

        // Set the fugu's new position and scale.
        fugu.SetGridPosition(bottomLeftCoordinate);
        SetFuguVisuals(fugu);

        List<Vector2Int> currentCells = fugu.GetAllCells();
        for (int i = 0; i < currentCells.Count; i++)
        {
            grid[currentCells[i].x,currentCells[i].y] = fugu.id;
        }
    }

    private void SetFuguVisuals(FuguController fugu)
    {
        fugu.SetScaleVisuals();
        fugu.SetColorVisuals();
        fugu.SetRotationVisuals();
    }

    public float FuguLandingDelay = 0.3f;
    IEnumerator FuguLandingDelayStep()
    {
        // Give final buffer for input
        float temp = freeFallTickPeriod;
        freeFallTickPeriod = 99999f;

        yield return new WaitForSeconds(FuguLandingDelay);
        sfxManager.PlayMoveSFX();

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
        List<FuguController> allFugus = GetAllFugusInGrid();

        // Sort all active fugus by height. Lowest first.
        allFugus.Sort(delegate (FuguController f1, FuguController f2) {
            return f1.bottomLeftCoordinate.y.CompareTo(f1.bottomLeftCoordinate.y);
        });

        yield return new WaitForSeconds(FugusQuickFallStepDelay);

        bool atLeastOneFell = false;

        foreach (FuguController fugu in allFugus)
        {
            List<Vector2Int> belowCells = fugu.GetBelowCells();
            bool canFuguFall = true;
            foreach (Vector2Int belowCell in belowCells)
            {
                // OOB, noop
                if (isOOB(belowCell))
                {
                    canFuguFall = false;
                    break;
                }
                // something blocking, noop
                if (grid[belowCell.x, belowCell.y] != -1)
                {
                    canFuguFall = false;
                    break;
                }
            }
            if (canFuguFall)
            {
                PlaceFuguInGrid(fugu, fugu.bottomLeftCoordinate + Vector2Int.down);
                atLeastOneFell = true;
            }
        }

        // if not all fugus are at rest, do it again
        if (atLeastOneFell)
        {
            sfxManager.PlayQuickFallSFX();
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
            // skip if visited already or if the gameObject is inactive
            if (visitedFugus.Contains(p.Key) || !p.Value.gameObject.activeInHierarchy)
            {
                continue;
            }
            FuguController fugu = p.Value;
            visitedFugus.Add(fugu.id);

            // Explore all color matching fugus from here once.
            HashSet<int> connectedFugus = new HashSet<int>();
            ExploreConnectedFugus(fugu, visitedFugus, connectedFugus, new HashSet<int>(), fugu.color);
            connectedSets.Add(connectedFugus);
        }

        yield return new WaitForSeconds(ConnectFugusStepDelay);

        // Set visual connections between fugus here...
        sfxManager.PlayMoveSFX();

        yield return ExplodeFugusStep(connectedSets);
    }

    // TODO: Finish this/get it working.
    void DrawConnectedFuguPerimeters(List<HashSet<int>> connectedSets)
    {
        List<HashSet<Vector2Int>> cellCoordSets = new List<HashSet<Vector2Int>>();

        // Iterate over each fugu.
        foreach (HashSet<int> fuguIDs in connectedSets)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();

            // Iterate over all cells of each fugu and add to collective set for these adjacent fugus.
            foreach (int id in fuguIDs)
            {
                FuguController fugu = fuguDict[id];
                foreach (Vector2Int pos in fugu.GetAllCells())
                {
                    cells.Add(pos);
                }
            }
            cellCoordSets.Add(cells);
        }

        // Using the groups of adjacent cells. get the 


        // .... using shader method, get all adjacent cells and put a cylinder between them.

        // iterate over sets...
        foreach (HashSet<Vector2Int> cell in cellCoordSets)
        {

        }

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
        // Mark as visited for future iterations.
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
            nextToVisit.Add(grid[pos.x,pos.y]);
        }

        foreach (int id in nextToVisit)
        {
            if (visitedFugus.Contains(id) || (fuguDict.ContainsKey(id) && !fuguDict[id].gameObject.activeInHierarchy))
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
        bool isOneOrMoreExplosions = false;
        List<IEnumerator> fuguExplosions = new List<IEnumerator>();

        foreach(HashSet<int> fuguIDs in connectedSets)
        {
            if (fuguIDs.Count < 4)
            {
                continue;
            }
            isOneOrMoreExplosions = true;

            foreach (int id in fuguIDs)
            {
                // Check if it's in the dict and is active, because it may have just been removed by a previous iteration.
                if (fuguDict.ContainsKey(id) && fuguDict[id].gameObject.activeInHierarchy)
                {
                    FuguController fugu = fuguDict[id];
                    RemoveFuguFromGrid(fugu);
                    fuguExplosions.Add(ExplodeIndividualFugu(fugu));
                }
            }
        }

        // Run all fugu explosion coroutines.
        for (int i = 0; i < fuguExplosions.Count; i++)
        {
            StartCoroutine(fuguExplosions[i]);
        }
        // Continue when they are finished.
        for (int i = 0; i < fuguExplosions.Count; i++)
        {
            yield return fuguExplosions[i];
        }


        // And the cycle restarts...
        // Here we need to make everything QUICKLY fall on loop until all is settled.
        if (isOneOrMoreExplosions)
        {
            // do quickfall
            yield return FugusQuickFallStep();
        }
        else
        {
            if (fuguQueue.Count > 0)
            {
                FuguPair nextFuguPair = fuguQueue.First();
                fuguQueue.RemoveFirst();
                ActiveFreefallPair.SetFuguPair(nextFuguPair);
                SaveGridState(nextFuguPair);
                isPlayerLocked = false;

                sfxManager.PlayPreExplodeSFX();

                // Update and render the queue!
                RenderFuguQueue();
            }
            else
            {
                // Either the player is about to win, or the level is lost!
            }
        }
    }

    private void RenderFuguQueueAtStart()
    {
        // start by directly setting position...

        int i = 0;
        foreach (FuguPair fuguPair in fuguQueue)
        {
            SetPairQueuePosition(fuguPair, fuguQueuePositions[i], true);
            i++;
        }
    }
    private void RenderFuguQueue()
    {
        // start by directly setting position...

        int i = 0;
        foreach (FuguPair fuguPair in fuguQueue)
        {
            SetPairQueuePosition(fuguPair, fuguQueuePositions[i], false);
            i++;
        }
    }

    public float leftMargin = 0f;
    public float topMargin = 0f;
    public float xGap = 0f;
    public float yGap = 0f;

    void SetPairQueuePosition(FuguPair fuguPair, Vector2 pos, bool isStart)
    {

        float scalar = 3f;
        Vector2 topLeftAnchorPosition = new Vector2(-8f, 13f);

        SetFuguVisuals(fuguPair.primary);
        SetFuguVisuals(fuguPair.secondary);

        //Vector2 endPos = (pos * scalar) + topLeftAnchorPosition;
        //fuguPair.primary.MoveToQueuePosition(fuguPair.primary.transform.localPosition, endPos + (Vector2.down * 2));
        //fuguPair.secondary.MoveToQueuePosition(fuguPair.primary.transform.localPosition, endPos);


        // Also shrink them a little to better fit in the UI
        // fuguPair.primary.transform.localScale = Vector2.one * 0.8f;
        // fuguPair.secondary.transform.localScale = Vector2.one * 0.8f;

        float fuguScalar = 0.8f;
        Vector2 upperPos = GetCalculatedQueuePosition(pos);
        Vector2 lowerPos = upperPos + Vector2.down * 2f * fuguScalar; // 0.8 is the queue fugu scale

        if (isStart)
        {
            fuguPair.primary.transform.localPosition = lowerPos;
            fuguPair.secondary.transform.localPosition = upperPos;
        }
        else
        {
            fuguPair.primary.MoveToQueuePosition(fuguPair.primary.transform.localPosition, lowerPos);
            fuguPair.secondary.MoveToQueuePosition(fuguPair.secondary.transform.localPosition, upperPos);
        }

        fuguPair.primary.transform.localScale = Vector2.one * fuguScalar;
        fuguPair.secondary.transform.localScale = Vector2.one * fuguScalar;
    }

    Vector2 GetCalculatedQueuePosition(Vector2 pos)
    {
        Vector2 calcPos = (Vector2.right * leftMargin) + (Vector2.down * topMargin) + pos + (Vector2.right * xGap * pos.x) + (Vector2.up * yGap * pos.y);
        return calcPos;
    }

    private float BaseIndividualExplodeDelay = 2.0f;
    IEnumerator ExplodeIndividualFugu(FuguController fugu)
    {
        float d = UnityEngine.Random.Range(-0.1f, 0.1f);

        yield return new WaitForSeconds(BaseIndividualExplodeDelay + d);
        // particles...
        sfxManager.PlayExplodeSFX();
        fugu.ExplodeSelf();

        // wait however long the animation needs...
        yield return new WaitForSeconds(0.1f);
        fugu.gameObject.SetActive(false);
        //Destroy(fugu.gameObject);
    }

    void RemoveFuguFromGrid(FuguController fugu)
    {
        fuguMorgue.Add(fugu.id, fugu);
        fuguDict.Remove(fugu.id);
        fugu.PreExplode();
        ////fugu.ExplodeSelf(); // animation
        sfxManager.PlayPreExplodeSFX();
        ClearPreviousFuguPosition(fugu, fugu.bottomLeftCoordinate);
    }


    // Debugging fugu / cell tool.
    private bool enableDebuggingGrid = false;
    private GameObject[,] debuggingGrid;
    void InitDebuggingGrid()
    {
        if (!enableDebuggingGrid)
        {
            return;
        }
        debuggingGrid = new GameObject[GRID_SIZE.x, GRID_SIZE.y];
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                debuggingGrid[i,j] = GameObject.Instantiate(DebuggingSquare);
                debuggingGrid[i,j].transform.localPosition = new Vector2(i, j) + (Vector2.one * 0.5f);
            }
        }
    }

    void DebuggingRenderGrid ()
    {
        if (!enableDebuggingGrid)
        {
            return;
        }
        for (int i = 0; i < GRID_SIZE.x; i++)
        {
            for (int j = 0; j < GRID_SIZE.y; j++)
            {
                bool isActive = grid[i, j] != -1 && fuguDict[grid[i, j]].gameObject.activeInHierarchy;
                debuggingGrid[i, j].SetActive(isActive);
            }
        }
    }
}
