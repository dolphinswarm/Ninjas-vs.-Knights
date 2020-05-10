using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AimIK;
using UnityEngine.UI;

/// <summary>
/// The script for contolling a player.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ============================================= Variables
    [Header("Controls and Animations")]
    public Transform behindPosition;            // The behind position of the camera.
    public Transform shoulderPosition;          // The shoulder position of the camera
    private CharacterController controller;     // The script for controlling a character.
    private Animator animator;                  // The animator for the controller.
    private AimIKBehaviour aimer;               // The script for controlling aim.
    private AimerMovement aimerMovement;
    private Camera mainCamera;                  // The main camera.
    private Camera overheadCamera;              // The overhead camera.
    private Canvas reticle;                     // The canvas containing the aiming reticle.

    private float speed;
    public float turn;
    private Vector3 jump = Vector3.zero;

    [Header("Shooting")]
    public bool hasGun = false;                // Does the player currently have a gun?
    public GunScript gun;                       // The gun of this character.
    private bool adjustOnce = false;

    [Header("Body Parts")]
    public GameObject spine;
    public GameObject rightHand;

    [Header("Health")]
    public float health = 100.0f;
    public GameObject deathRagdoll;             // The ragdoll to create upon death.
    private bool onlySpawnOne = false;          // Only spawn one ragdoll!

    [Header("Scoring")]
    public bool isHoldingFlag = false;
    public GameManager gameManager;

    [Header("Audio")]
    public AudioClip jumpSound;
    public AudioClip death;

    // ============================================= Methods
    /// <summary>
    /// On game start, set this object's properties.
    /// </summary>
    void Start()
    {
        // Set the game manager, if not set
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // Set the animator and character controller
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Set the aimer and disable it
        aimer = GetComponentInChildren<AimIKBehaviour>();
        aimer.enabled = false;
        aimerMovement = GetComponentInChildren<AimerMovement>();

        // Set the main camera
        mainCamera = Camera.main;

        // Set the overhead camera
        overheadCamera = GameObject.FindGameObjectWithTag("OverheadCamera").GetComponent<Camera>();

        // Set the reticle
        reticle = mainCamera.GetComponentInChildren<Canvas>();
        reticle.enabled = false;

        // Adjust health bar
        gameManager.healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(health * 3.0f, 0), 15.0f);
    }

    // Update is called once per frame
    void Update()
    {
        // Set initial float
        float modifier = 1.0f;

        if (controller.isGrounded)
        {
            // Reset jump vector
            jump = Vector3.zero;

            // If we should reload...
            if (Input.GetKeyDown(KeyCode.Z) && !gun.currentlyReloading)
            {
                gun.Reload();
            }

            // If we should aim, aim
            if (Input.GetMouseButton(1) && hasGun && !adjustOnce && !gun.currentlyReloading)
            {
                // Turn on aiming
                ToggleAiming(true);
                modifier = 0.5f;

                // Readjust transforms
                mainCamera.transform.position = shoulderPosition.position;
                mainCamera.transform.rotation = shoulderPosition.rotation;
                mainCamera.transform.parent = shoulderPosition;

                adjustOnce = true;
            }
            // Else, if let go, unaim
            else if ((!Input.GetMouseButton(1) || !hasGun || gun.currentlyReloading) && adjustOnce)
            {
                // Turn off aiming
                ToggleAiming(false);

                // Readjust transforms
                mainCamera.transform.position = behindPosition.position;
                mainCamera.transform.rotation = behindPosition.rotation;
                mainCamera.transform.parent = behindPosition;

                adjustOnce = false;
            }

            // If we should jump...
            if (Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(1))
            {
                animator.SetBool("Jump", true);
                jump.y = 15.0f;
                AudioSource.PlayClipAtPoint(jumpSound, transform.position);
            }
            else
                animator.SetBool("Jump", false);

            // Get horizontal and vertical movement
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            // Set movement variables
            speed = v * 2.0f * modifier;
            turn = h;
            animator.SetFloat("Speed", speed);
            animator.SetFloat("Turn", turn);
        }

        // Apply jump movement
        else {
            // Set movement variables
            float tmp = jump.y;
            jump = transform.forward * speed * 2.0f;
            jump.y = tmp;
            jump.y -= 9.8f * Time.deltaTime;
        }

        controller.Move(jump * Time.deltaTime);
    }

    /// <summary>
    /// Tell the gun if we are currently aiming.
    /// </summary>
    /// <param name="aiming"></param>
    private void ToggleAiming(bool aiming)
    {
        // Adjust the animator and crosshair
        animator.SetBool("Aim", aiming);
        aimer.enabled = aiming;
        reticle.enabled = aiming;
        overheadCamera.enabled = !aiming;
        gameManager.ammoCounter.SetActive(aiming);

        // Toggle the bool in the gun
        if (gun != null)
            gun.currentlyAiming = aiming;
    }

    /// <summary>
    /// Damages this player.
    /// </summary>
    /// <param name="amount">The amount to damage by.</param>
    public void Damage(float amount)
    {
        // Subtract health from this knight
        health -= amount;

        // Adjust health bar
        gameManager.healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(health * 3.0f, 0), 15.0f);

        // If health is below 0, die
        if (health <= 0.0f && !onlySpawnOne)
        {
            // Create ragdoll
            onlySpawnOne = true;
            GameObject ragdoll = Instantiate(deathRagdoll, transform.position, transform.rotation);
            Destroy(ragdoll, 7.0f);

            // If holding flag, drop it
            // If holding flag, drop it
            if (isHoldingFlag)
            {
                gameManager.enemyFlag.GetComponent<FlagScript>().Drop();
                // Show message
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died and dropped the enemy's flag!";
            }
            else
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died!";

            

            // Turn off aiming
            ToggleAiming(false);

            // Remove from players list
            gameManager.players.Remove(gameObject);

            // Play death scream
            AudioSource.PlayClipAtPoint(death, transform.position);

            // Change camera to follow ragdoll
            Camera.main.transform.parent = ragdoll.transform;

            // Destroy self
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Toggle's this animator's reload.
    /// </summary>
    public float ToggleReload()
    {
        // Change the bool
        if (animator.GetBool("Reload"))
            // Set bool
            animator.SetBool("Reload", false);
        else
        {
            // Set bool
            animator.SetBool("Reload", true);

            // Return the duration of the animation clip
            foreach (AnimationClip animation in animator.runtimeAnimatorController.animationClips)
                if (animation.name == "reloading")
                    return animation.length;
        }

        // Return nothing
        return 0.0f;
    }
}
