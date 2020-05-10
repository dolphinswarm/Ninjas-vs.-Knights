using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimerMovement : MonoBehaviour
{
    // ============================================= Variables
    public Transform player;        // The player's transform
    private Vector3 prevPos;
    private Vector3 curPos;
    private Vector3 originalPos;
    private bool canAim = false;

    // ============================================= Methods
    // Start is called before the first frame update
    void Start()
    {
        // Set player
        if (player == null)
            player = FindObjectOfType<PlayerController>().transform;

        // Get mouse pos
        curPos = Input.mousePosition;
        Cursor.lockState = CursorLockMode.Confined;

        // Set the original pos
        originalPos = transform.localPosition;
    }


    // Update is called once per frame
    void Update()
    {
        // Look at player
        transform.LookAt(player);

        // Hide cursor
        if (Cursor.visible)
            Cursor.visible = false;

        // Turn on aim coroutine on mouse down
        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(WaitToAim());
        }

        // Turn on aim coroutine on mouse down
        if (Input.GetMouseButtonUp(1))
        {
            canAim = false;
        }


        // Get the mouse movement
        if (canAim)
        {
            // Get movement vector
            prevPos = curPos;
            curPos = Input.mousePosition;
            Vector3 delta = curPos - prevPos;

            // Modify x and y comoponents
            delta.x *= 0.05f;
            delta.y *= 0.1f;

            // Move
            transform.localPosition += delta;
        }
        // Else, reset
        else
        {
            // Reset cursor and positon
            transform.localPosition = originalPos;
        }
    }

    IEnumerator WaitToAim()
    {
        yield return new WaitForSeconds(0.1f);

        canAim = true;
    }
}
