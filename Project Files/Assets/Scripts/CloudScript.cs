using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// / The script fort moving the clouds.
/// </summary>
public class CloudScript : MonoBehaviour
{
    // ============================================= Variables
    private float speed;

    // ============================================= Methods
    /// <summary>
    /// On game start...
    /// </summary>
    void Start()
    {
        speed = Random.Range(0.01f, 0.2f);
    }

    /// <summary>
    /// Move clouds on frame update.
    /// </summary>
    void Update()
    {
        // Move by 1 unit
        transform.position += Vector3.right * speed;

        // If we're far to the left, move back
        if (transform.position.x > 500.0f)
            transform.position = new Vector3(-300.0f, transform.position.y, transform.position.z);
    }
}
