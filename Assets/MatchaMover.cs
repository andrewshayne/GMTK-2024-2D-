using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchaMover : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float moveSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        Vector3 moveVector = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            moveVector += new Vector3(0, 1);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveVector += new Vector3(1, 0);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            moveVector += new Vector3(1, -1);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveVector += new Vector3(-1, 0);
        }
        transform.position += moveVector * moveSpeed;
    }
}
