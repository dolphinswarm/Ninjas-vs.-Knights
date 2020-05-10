using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Billboards this object to the main camera.
/// </summary>
public class Billboard : MonoBehaviour
{
    // ============================================= Variables

    // ============================================= Methods
    /// <summary>
    /// On frame update, look at main camera
    /// </summary>
    void Update()
    {
        transform.LookAt(Camera.main.transform);
    }
}
