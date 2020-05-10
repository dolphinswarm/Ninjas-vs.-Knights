using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The script for controlling flag collection.
/// </summary>
public class FlagScript : MonoBehaviour
{
    // ============================================= Variables
    [Header("Scoring")]
    public GameManager gameManager;
    public Vector3 startPosition;

    [Header("Collision")]
    public string myTeamCheckTag;             // The tag we should be checking for
    public string otherTeamCheckTag;             // The tag we should be checking for
    public GameObject flagToSpawn;      // The flag prefab we should spawn.
    public bool currentlyCollected = false;
    public bool hasBeenStolen = false;

    [Header("Audio")]
    public AudioClip collect;
    public AudioClip warHorn;

    // ============================================= Methods
    /// <summary>
    /// On game start...
    /// </summary>
    void Start()
    {
        // Set the game manager, if not set
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // Set default tag, if none found
        if (myTeamCheckTag == "")
            myTeamCheckTag = "Null";

        // Set default tag, if none found
        if (otherTeamCheckTag == "")
            otherTeamCheckTag = "Null";

        // Set the start position
        startPosition = transform.position;
    }

    /// <summary>
    /// On entry with a collider
    /// </summary>
    /// <param name="other"></param>
    void OnTriggerEnter(Collider other)
    {
        // Check other tags. If they match...
        if (other.gameObject.CompareTag(otherTeamCheckTag) && !currentlyCollected)
        {
            // Set the parent transform of this object to the other object
            GameObject spawnedFlag = new GameObject();

            // If player...
            if (otherTeamCheckTag == "Player")
            {
                // Get spine of ninja
                Transform ninjaSpine;
                if (other.gameObject.TryGetComponent<PlayerController>(out PlayerController controller))
                    ninjaSpine = controller.spine.transform;
                else
                    ninjaSpine = other.gameObject.GetComponent<PlayerAI>().spine.transform;

                // Set spawned flag to be inside ninja
                spawnedFlag = Instantiate(flagToSpawn, ninjaSpine);
                spawnedFlag.transform.localPosition = new Vector3(-0.1f, 0.0f, -0.05f);

                // Toggle that the flag is acquired
                if (controller != null)
                    controller.isHoldingFlag = true;
                else
                    other.gameObject.GetComponent<PlayerAI>().isHoldingFlag = true;

                // Toggle on the player's particle system
                gameManager.enemyGoal.hasFlag = false;
                gameManager.playerGoal.particles.Play();

                // Play collect sound
                AudioSource.PlayClipAtPoint(collect, transform.position);

                // Show message
                gameManager.eventText.text = other.name.Replace("(Clone)", "") + " captured the enemy flag!";
            } 
            // If enemy
            else if (otherTeamCheckTag == "Enemy")
            {
                // Get spine of ninja
                Transform enemySpine = other.gameObject.GetComponent<EnemyAI>().spine.transform;

                // Set spawned flag to be inside ninja
                spawnedFlag = Instantiate(flagToSpawn, enemySpine);
                //spawnedFlag.transform.localPosition = new Vector3(-0.1f, 0.0f, -0.25f);

                // Toggle that the flag is acquired
                other.gameObject.GetComponent<EnemyAI>().isHoldingFlag = true;

                // Toggle on the player's particle system
                gameManager.playerGoal.hasFlag = false;
                gameManager.enemyGoal.particles.Play();

                // Play collect sound
                AudioSource.PlayClipAtPoint(warHorn, Camera.main.transform.position);

                // Show message
                gameManager.eventText.text = other.name.Replace("(Clone)", "") + " captured your flag!";
            }

            // Hide this object
            transform.parent = other.gameObject.transform;
            gameObject.SetActive(false);
            currentlyCollected = true;

            // Set stolen status
            hasBeenStolen = true;
        }
        // Else, being re-collected, so check the friendly team flag
        else if (other.gameObject.CompareTag(myTeamCheckTag) && !currentlyCollected && transform.position != startPosition)
        {
            // If player OR enemy
            if (myTeamCheckTag == "Player" || myTeamCheckTag == "Enemy")
            {
                // Play collect sound
                AudioSource.PlayClipAtPoint(collect, transform.position);

                // Go back to original position
                transform.position = startPosition;

                // Reset stolen status
                hasBeenStolen = false;

                // Show message
                gameManager.eventText.text = other.name.Replace("(Clone)", "") + " retreived their team's flag!";
            }

            // Toggle correct particle system
            if (myTeamCheckTag == "Player")
            {
                // Toggle on the player's particle system
                gameManager.playerGoal.hasFlag = true;
                gameManager.enemyGoal.particles.Stop();
            }
            else if (myTeamCheckTag == "Enemy")
            {
                // Toggle on the player's particle system
                gameManager.enemyGoal.hasFlag = true;
                gameManager.playerGoal.particles.Stop();
            }
        }
    }


    /// <summary>
    /// Drops this flag.
    /// </summary>
    public void Drop()
    {
        // Hide this object
        gameObject.SetActive(true);
        gameObject.transform.parent = null;

        // Turn off currently collected
        currentlyCollected = false;
    }
}
