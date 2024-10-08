using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FuguColor
{
    Red,
    Green,
    Yellow,
    Purple,
    Cyan,
    Pink,
}

public enum FuguScale
{
    Small = 1,  // 1x1
    Medium, // 2x2
    Large,  // 3x3
}

public enum RelativePosition
{
    Up,
    Right,
    Down,
    Left,
}

public class FuguController : MonoBehaviour
{
    public bool isPrimary; // Indicates that this fugu is the active pivot point. This changes upon inflating the partner as the larger fugu.
    public int id;         // Unique ID.
    public int partnerId;  // Partner's unique ID. Only matters during free-fall.
    public Vector2Int bottomLeftCoordinate;

    public FuguColor color;
    public FuguScale scale;
    public RelativePosition relativePosition;
    public SpriteRenderer image;

    public Animator animator;

    /*
    public FuguController(bool isPrimary, int id, int partnerId, Vector2Int topLeftCoordinate, FuguColor color)
    {
        this.isPrimary = isPrimary;
        this.id = id;
        this.partnerId = partnerId;
        this.bottomLeftCoordinate = topLeftCoordinate + (Vector2Int.down * -2);
        this.color = color;
        this.scale = FuguScale.Medium;

        if (isPrimary)
        {
            relativePosition = RelativePosition.Down;
        }
        else
        {
            relativePosition = RelativePosition.Up;
        }
    }
    */

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetRotationVisuals()
    {
        transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, -90 * (int)relativePosition + 90);
    }

    public void SetScaleVisuals()
    {
        // ALWAYS
        //transform.localScale = new Vector3(0.5f, 0.5f);

        int scaleInt = (int)scale;
        animator.SetInteger("FuguScale", scaleInt);
        transform.GetChild(0).localPosition = new Vector2(0.5f, 0.5f) * scaleInt;
    }
    public void SetGridPosition(Vector2Int bottomLeftCoordinate)
    {
        this.bottomLeftCoordinate = bottomLeftCoordinate;
        transform.localPosition = (Vector2)bottomLeftCoordinate;
    }
    public void SetColorVisuals()
    {
        Color tint = Color.white;
        switch (color)
        {
            case FuguColor.Red:
                tint = Color.red;
                break;
            case FuguColor.Green:
                tint = Color.green;
                break;
            case FuguColor.Purple:
                tint = new Color(0.25f, 0.06f, 0.45f);
                break;
            case FuguColor.Pink:
                tint = new Color(0.77f, 0.27f, 0.67f);
                break;
            case FuguColor.Cyan:
                tint = Color.cyan;
                break;
            case FuguColor.Yellow:
                tint = Color.yellow;
                break;
        }
        transform.GetChild(0).GetComponent<SpriteRenderer>().color = tint;
    }

    public void Draw()
    { 
        SetRotationVisuals();
        SetScaleVisuals();
        SetColorVisuals();
        // SetGridPositionVisuals();
        transform.localPosition = (Vector2)bottomLeftCoordinate;
    }

    // Get all the cells this fugu is currently covering.
    public List<Vector2Int> GetAllCells()
    {
        List<Vector2Int> coords = new List<Vector2Int>();
        int dim = (int)scale;
        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                coords.Add(bottomLeftCoordinate + new Vector2Int(i, j));
            }
        }
        return coords;
    }

    // Get all the cells adjacent to this fugu.
    public List<Vector2Int> GetAllAdjacentCells()
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        int dim = (int)scale;
        for (int i = 0; i < dim; i++)
        {
            Vector2Int adjD = bottomLeftCoordinate + (Vector2Int.right * i) + Vector2Int.down;
            Vector2Int adjL = bottomLeftCoordinate + (Vector2Int.up * i) + Vector2Int.left;
            Vector2Int adjU = bottomLeftCoordinate + (Vector2Int.right * i) + (Vector2Int.up * dim);
            Vector2Int adjR = bottomLeftCoordinate + (Vector2Int.up * i) + (Vector2Int.right * dim);
            coords.Add(adjD);
            coords.Add(adjL);
            coords.Add(adjU);
            coords.Add(adjR);
        }

        return coords;
    }

    // Get all the cells below this fugu. Useful for detecting when we collide below.
    public List<Vector2Int> GetBelowCells()
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        int dim = (int)scale;
        for (int i = 0; i < dim; i++)
        {
            Vector2Int adjD = bottomLeftCoordinate + (Vector2Int.right * i) + Vector2Int.down;
            coords.Add(adjD);
        }
        return coords;

    }

    public List<Vector2Int> GetLeftCells()
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        int dim = (int)scale;
        for (int i = 0; i < dim; i++)
        {
            Vector2Int adjL = bottomLeftCoordinate + (Vector2Int.up * i) + Vector2Int.left;
            coords.Add(adjL);
        }
        return coords;

    }

    public List<Vector2Int> GetRightCells()
    {
        List<Vector2Int> coords = new List<Vector2Int>();

        int dim = (int)scale;
        for (int i = 0; i < dim; i++)
        {
            Vector2Int adjR = bottomLeftCoordinate + (Vector2Int.up * i) + (Vector2Int.right * dim);
            coords.Add(adjR);
        }
        return coords;

    }

    public void DetachFromPair()
    {
        animator.SetTrigger("Detach");
    }

    public void PreExplode()
    {
        animator.SetTrigger("PreExplode");
    }

    public void ExplodeSelf()
    {
        transform.GetChild(0).GetComponent<SpriteRenderer>().color = Color.white;
        animator.SetTrigger("Explode");
    }

    // For restoring an exploded fugu back into the grid.
    public void RestoreSelf()
    {
        animator.SetInteger("FuguScale", (int)scale);
        animator.SetTrigger("Restore");
    }

    public Vector2Int GetCenterCoord()
    {
        int x = (2 * bottomLeftCoordinate.x + (int)scale - 1) / 2;
        int y = (2 * bottomLeftCoordinate.y + (int)scale - 1) / 2;
        return new Vector2Int(x, y);
    }

    public void MoveToQueuePosition(Vector2 start, Vector2 end)
    {
        StartCoroutine(MoveTowardQueuePos(start, end));
    }

    float timeToMove = 0.5f;
    IEnumerator MoveTowardQueuePos(Vector2 start, Vector2 end)
    {
        float t = 0;
        while (t < 1)
        {
            transform.localPosition = Vector2.Lerp(start, end, t);
            t = t + Time.deltaTime / timeToMove;
            yield return new WaitForEndOfFrame();
        }
    }
}
