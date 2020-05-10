using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTeammateRagdollScript : MonoBehaviour
{
    // ============================================= Variables
    public GameManager gameManager;             // The game's game manager.

    // ============================================= Methods
    /// <summary>
    /// Gets the game manager.
    /// </summary>
    void Start()
    {
        // Set the game manager, if not set
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    /// <summary>
    /// On destory, summons a new knight.
    /// </summary>
    void OnDestroy()
    {
        if (!gameManager.gameOver) gameManager.SpawnPlayerAI();
    }
}
