using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A small script for managing the win / lose screens.
/// </summary>
public class WinLoseManager : MonoBehaviour
{
    /// <summary>
    /// Show cursor.
    /// </summary>
    void Update()
    {
        if (!Cursor.visible)
            Cursor.visible = true;
    }

    /// <summary>
    /// Loads the title scene.
    /// </summary>
    public void BackToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}
