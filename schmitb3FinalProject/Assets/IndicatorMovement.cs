using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the indicator dynamically.
/// </summary>
public class IndicatorMovement : MonoBehaviour
{
    // ============================================= Variables
    private float num = 0.0f;

    // ============================================= Methods
    /// <summary>
    /// On frame update...
    /// </summary>
    void Update()
    {
        // Move up
        transform.position = transform.parent.position + (Vector3.up * 1.5f) + new Vector3(0.0f, Mathf.Sin(num) / 10.0f, 0.0f);

        // Increment number
        num += 0.1f;
    }
}
