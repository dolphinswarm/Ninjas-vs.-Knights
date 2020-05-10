using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script for managing the goals.
/// </summary>
public class GoalScript : MonoBehaviour
{
    // ============================================= Variables
    [Header("Collision")]
    public string checkTag;             // The tag we should be checking for
    public bool hasFlag = true;         // Does this goal have its flag?
    
    [Header("Visuals")]
    public ParticleSystem particles;    // The particle system for this object.

    [Header("Scoring")]
    public GameManager gameManager;

    // ============================================= Methods
    // Start is called before the first frame update
    void Start()
    {
        // Set the game manager, if not set
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// On collision with another trigger...
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // Debug other
        Debug.Log(other.name);

        // Check if we match tags...
        if (other.gameObject.CompareTag(checkTag))
        {
            // If a player...
            if (checkTag == "Player")
            {
                // Get the player controller
                if (other.gameObject.TryGetComponent<PlayerController>(out PlayerController controller))
                {
                    if (controller.isHoldingFlag) gameManager.Win();
                }
                else
                {
                    if (other.gameObject.GetComponent<PlayerAI>().isHoldingFlag) gameManager.Win();
                }
            }
            // If an enemy...
            if (checkTag == "Enemy")
            {
                // Get the enemy AI
                EnemyAI enemy = other.gameObject.GetComponent<EnemyAI>();

                // If the enemy AI has flag, lose...
                if (enemy.isHoldingFlag) gameManager.Lose();
            }
        }
    }
}
