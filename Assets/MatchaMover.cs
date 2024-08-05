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
    public AudioSource keySound;

    // Update is called once per frame
    void Update()
    {
        Vector3 moveVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            keySound.Play();
        }
        transform.position += moveVector * moveSpeed;
    }
}
