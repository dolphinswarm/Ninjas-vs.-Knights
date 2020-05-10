using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Billboards this object to the main camera.
/// </summary>
public class Billboard180 : MonoBehaviour
{
    /// <summary>
    /// On frame update, look at main camera
    /// </summary>
    void Update()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(Vector3.up, 180.0f);
    }
}
