using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using AimIK;
using UnityEngine.AI;
/// <summary>
/// An enumeration of enemy states.
/// </summary>
public enum PlayerState
{
    WalkingTowardsMiddle, WalkingTowardsFlag,
    WalkingTowardsBase, RetrievingEnemyFlag,
    RetrievingPlayerFlag, ShootingAtEnemy, Reloading, NULL
}

/// <summary>
/// The script for controlling AI through NavMeshes.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class PlayerAI : MonoBehaviour
{
    // ============================================= Variables
    [Header("Controls and Animations")]
    private Animator animator;                  // The animator for the controller.
    private AimIKBehaviour aimer;               // The script for controlling aim.
    //private Stack<KeyValuePair<Transform, EnemyState>> previousStateStack;      // A stack of previous command calls
    private PlayerState previousState = PlayerState.NULL;
    private Transform previousNavMeshTarget = null;
    private Transform preReloadTarget = null;
    public PlayerState currentState;             // The current state of this enemy.

    public float speed;

    [Header("Movement and AI")]
    public Transform navMeshTarget;             // The target of the nav mesh agent.
    private NavMeshAgent navMeshAgent;          // The nav mesh agent of this object.
    private Transform player;                   // The transform of the player
    public int behavior = 0;                    // Sets random behavior for the AI.
    [Range(2.0f, 20.0f)]
    public float enemyShootDistance = 10.0f;   // The distance at which we should shoot the player from.
    public GunScript gun;                       // The enemy's gun.
    public bool currentlyAiming;                // Is this enemy currently aiming?
    public GameObject nearestEnemy;

    private FlagScript myTeamFlag;
    private FlagScript otherTeamFlag;

    [Header("Body Parts")]
    public GameObject spine;

    [Header("Health")]
    public GameObject healthBar;                // This knight's health bar.
    public float health = 100.0f;               // This knight's current health.
    public GameObject deathRagdoll;             // The ragdoll to create upon death.
    private bool onlySpawnOne = false;          // Only spawn one ragdoll!

    [Header("Scoring")]
    public bool isHoldingFlag = false;
    public GameManager gameManager;

    [Header("Audio")]
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

        // Set the animator and nav mesh agent
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Get the player transform
        //player = FindObjectOfType<PlayerController>().gameObject.transform;

        // Set the default state and target
        currentState = PlayerState.WalkingTowardsMiddle;
        SetTarget();

        //// Add to previous call stack
        //previousStateStack = new Stack<KeyValuePair<Transform, PlayerState>>();
        //previousStateStack.Push(
        //    new KeyValuePair<Transform, PlayerState>(navMeshTarget, currentState));

        // Update the behavior
        behavior = Random.Range(0, 2);

        // Add flags
        myTeamFlag = gameManager.playerFlag.GetComponent<FlagScript>();
        otherTeamFlag = gameManager.enemyFlag.GetComponent<FlagScript>();

    }

    // Update is called once per frame
    void Update()
    {
        // Get the nearest player
        nearestEnemy = GetNearestEnemy();

        // PRIORITY #1 - IF PLAYER NEARBY, SHOOT AT
        if (nearestEnemy != null && (transform.position - nearestEnemy.transform.position).magnitude < enemyShootDistance &&
                  currentState != PlayerState.ShootingAtEnemy && currentState != PlayerState.Reloading)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == PlayerState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to shooting
            currentState = PlayerState.ShootingAtEnemy;

            // Set new target to be running away from player
            Vector3 newPosition = transform.position + (transform.position - nearestEnemy.transform.position);
            navMeshTarget = nearestEnemy.transform;
            navMeshAgent.SetDestination(newPosition);
        }

        // PRIORITY #2 - ELSE, IF MY TEAM FLAG IS NEARBY UNCOLLECTED, COLLECT IT
        else if ((transform.position - myTeamFlag.transform.position).magnitude < 25.0f &&
                  currentState != PlayerState.ShootingAtEnemy && currentState != PlayerState.Reloading &&
                  !myTeamFlag.currentlyCollected && myTeamFlag.hasBeenStolen)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == PlayerState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to retrieving
            currentState = PlayerState.RetrievingPlayerFlag;

            // Set new target to be enemy flag
            navMeshTarget = myTeamFlag.transform;
            navMeshAgent.SetDestination(navMeshTarget.position);
        }

        // PRIORITY #3 - ELSE, IF OTHER TEAM FLAG IS NEARBY UNCOLLECTED, COLLECT IT
        else if ((transform.position - otherTeamFlag.transform.position).magnitude < 25.0f &&
          currentState != PlayerState.ShootingAtEnemy && currentState != PlayerState.Reloading &&
          currentState != PlayerState.RetrievingPlayerFlag && !otherTeamFlag.currentlyCollected && otherTeamFlag.hasBeenStolen)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == PlayerState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to retrieving
            currentState = PlayerState.RetrievingPlayerFlag;

            // Set new target to be player flag
            navMeshTarget = otherTeamFlag.transform;
            navMeshAgent.SetDestination(navMeshTarget.position);
        }

        // PRIORITY #4 (NORMAL) - NONE OF THE ABOVE, SO BUSINESS AS NORMAL
        else
        {
            // IF NO PLAYER NEARBY AFTER SHOOTING, REVERT TO PREVIOUS STATE
            if (currentState == PlayerState.ShootingAtEnemy && (nearestEnemy == null ||
                (nearestEnemy.transform.position - nearestEnemy.transform.position).magnitude >= enemyShootDistance))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = PlayerState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, IF NO ENEMY FLAG NEARBY, REVERT TO PREVIOUS STATE
            else if (currentState == PlayerState.RetrievingPlayerFlag &&
                ((transform.position - myTeamFlag.transform.position).magnitude >= 25.0f ||
                myTeamFlag.currentlyCollected))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = PlayerState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, IF NO ENEMY FLAG NEARBY, REVERT TO PREVIOUS STATE
            else if (currentState == PlayerState.RetrievingPlayerFlag &&
                ((transform.position - otherTeamFlag.transform.position).magnitude >= 25.0f ||
                  otherTeamFlag.currentlyCollected))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = PlayerState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, DO NORMAL TRANSITIONS
            // Check the distance remaining. If none, change the state then find a new target
            else if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance &&
            navMeshAgent.velocity.sqrMagnitude == 0.0f && currentState != PlayerState.Reloading)
            {
                // If at midpoint...
                if (currentState == PlayerState.WalkingTowardsMiddle)
                {
                    // If we don't have flag, then run towards flag
                    if (!isHoldingFlag)
                        currentState = PlayerState.WalkingTowardsFlag;

                    // Else, we do, so run towards base
                    else
                        currentState = PlayerState.WalkingTowardsBase;
                }
                // Else, if at base for some reason and not holding flag, go back towards middle
                else if (currentState == PlayerState.WalkingTowardsBase && !isHoldingFlag)
                {
                    currentState = PlayerState.WalkingTowardsMiddle;
                }

                // Set the target
                SetTarget();
            }
            // If currently holding flag or a teammate is...
            else if (isHoldingFlag || otherTeamFlag.currentlyCollected)
            {
                // Update the state
                if (currentState == PlayerState.WalkingTowardsFlag)
                {
                    // If our behavior should be to run to the furthest midpoint first, do that
                    if (behavior == 0)
                    {
                        currentState = PlayerState.WalkingTowardsMiddle;
                        navMeshTarget = FindFurthestMidpoint();
                        navMeshAgent.SetDestination(navMeshTarget.position);
                    }
                    // Else, go directly to base
                    else
                    {
                        currentState = PlayerState.WalkingTowardsBase;
                        SetTarget();
                    }
                }
            }
        }

        // Apply animations
        if (Physics.Raycast(new Ray(transform.position, Vector3.down), 1.0f))
        {
            // Get the current velocity
            speed = navMeshAgent.velocity.magnitude;

            // If shooting at player, negatize it
            if (currentState == PlayerState.ShootingAtEnemy && gun != null)
            {
                speed = -speed * 0.5f;
                transform.LookAt(nearestEnemy.transform);
                if (!gun.currentlyAiming)
                    gun.currentlyAiming = true;
                gun.PlayerAIShoot();
            }
            else
            {
                if (gun.currentlyAiming)
                    gun.currentlyAiming = false;
            }

            // Set the speed
            animator.SetFloat("Speed", speed);

            // Set jump to false
            animator.SetBool("Jump", false);
        }

        // Apply jump movement
        else
        {
            // Set jump to true
            animator.SetBool("Jump", true);
        }
    }

    /// <summary>
    /// Chooses a target from a list of states
    /// </summary>
    void SetTarget()
    {
        // Re-check the nearest player
        nearestEnemy = GetNearestEnemy();

        // If walking towards the middle...
        if (currentState == PlayerState.WalkingTowardsMiddle)
            navMeshTarget = FindClosestMidpoint();

        // Else, if walking towards flag...
        else if (currentState == PlayerState.WalkingTowardsFlag)
            navMeshTarget = gameManager.enemyFlag.transform;

        // Else, if walking towards base...
        else if (currentState == PlayerState.WalkingTowardsBase)
            navMeshTarget = gameManager.playerGoal.transform;

        // Else, if shooting at player...
        else if (nearestEnemy != null && currentState == PlayerState.ShootingAtEnemy &&
                (transform.position - nearestEnemy.transform.position).magnitude < enemyShootDistance)
        {
            // Get a new far position
            Vector3 newPosition = transform.position + (transform.position - nearestEnemy.transform.position);
            navMeshTarget = nearestEnemy.transform;
            navMeshAgent.SetDestination(newPosition);

            // Return to prevent an error
            return;
        }

        // Set NavMeshAgent destination
        if (navMeshTarget == null)
        {
            if (isHoldingFlag)
            {
                currentState = PlayerState.WalkingTowardsBase;
                navMeshTarget = gameManager.enemyGoal.transform;
            }
            else
            {
                currentState = PlayerState.WalkingTowardsFlag;
                navMeshTarget = gameManager.playerFlag.transform;
            }
        }
        navMeshAgent.SetDestination(navMeshTarget.position);
    }

    /// <summary>
    /// Damages this enemy.
    /// </summary>
    /// <param name="amount">The amount to damage by.</param>
    public void Damage(float amount)
    {
        // Subtract health from this knight
        health -= amount;

        // Adjust health bar
        healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(Mathf.Max(health / 100.0f, 0), 0.05f);

        // If health is below 0, die
        if (health <= 0.0f && !onlySpawnOne)
        {
            // Create ragdoll
            onlySpawnOne = true;

            // If holding flag, drop it
            if (isHoldingFlag)
            {
                otherTeamFlag.Drop();
                // Show message
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died and dropped the enemy's flag!";
            }
            else
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died!";

            // Play death scream
            AudioSource.PlayClipAtPoint(death, transform.position);

            // Remove from players list
            gameManager.players.Remove(gameObject);

            // Drop gun
            gun.DropGun();

            // Create ragdoll
            GameObject ragdoll = Instantiate(deathRagdoll, transform.position, transform.rotation);
            Destroy(ragdoll, 7.0f);

            // Destroy self
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Finds the closest midpoint.
    /// </summary>
    /// <returns></returns>
    private Transform FindClosestMidpoint()
    {
        // Create basics
        float distance = float.MaxValue;
        string name = "";

        // For each midpoint...
        foreach (GameObject midpoint in GameObject.FindGameObjectsWithTag("Midpoint"))
        {
            // Tmp distance
            float tmpDist = (midpoint.transform.position - transform.position).magnitude;
            if (tmpDist < distance)
            {
                name = midpoint.name;
                distance = tmpDist;
            }
        }

        // Debug the closest midpoint
        Debug.Log("Closest midpoint to " + gameObject.name + ": " + name);

        // Return closest based on name
        return GameObject.Find(name).transform;
    }

    /// <summary>
    /// Finds the furthest midpoint.
    /// </summary>
    /// <returns></returns>
    private Transform FindFurthestMidpoint()
    {
        // Create basics
        float distance = float.MinValue;
        string name = "";

        // For each midpoint...
        foreach (GameObject midpoint in GameObject.FindGameObjectsWithTag("Midpoint"))
        {
            // Tmp distance
            float tmpDist = (midpoint.transform.position - transform.position).magnitude;
            if (tmpDist > distance)
            {
                name = midpoint.name;
                distance = tmpDist;
            }
        }

        // Debug the closest midpoint
        Debug.Log("Furthest midpoint from " + gameObject.name + ": " + name);

        // Return closest based on name
        return GameObject.Find(name).transform;
    }

    /// <summary>
    /// Toggle's this animator's reload.
    /// </summary>
    public float ToggleReload()
    {
        // Change the bool
        if (animator.GetBool("Reload"))
        {
            // Set boolean
            animator.SetBool("Reload", false);

            // If our target is still alive, go back to shooting them
            currentState = PlayerState.ShootingAtEnemy;
            if (preReloadTarget != null)
            {                
                navMeshTarget = preReloadTarget;
                navMeshAgent.SetDestination(navMeshTarget.transform.position);
            }
        }
        else
        {
            // Set boolean
            animator.SetBool("Reload", true);

            // Set state to reloading
            currentState = PlayerState.Reloading;
            preReloadTarget = navMeshTarget;

            // Reset navmesh
            navMeshAgent.SetDestination(transform.position);

            // Return the duration of the animation clip
            foreach (AnimationClip animation in animator.runtimeAnimatorController.animationClips)
                if (animation.name == "reloading")
                    return animation.length;
        }

        // Return nothing
        return 0.0f;
    }

    /// <summary>
    /// Sets the player for this object.
    /// </summary>
    /// <param name="player"></param>
    public void SetPlayer(Transform player)
    {
        this.player = player;
    }

    /// <summary>
    /// Gets the nearest enemy.
    /// </summary>
    /// <returns></returns>
    public GameObject GetNearestEnemy()
    {
        // For each enemy in enemies
        float dist = float.PositiveInfinity;
        GameObject closestEnemy = null;
        foreach (GameObject enemy in gameManager.enemies)
        {
            if ((enemy.transform.position - transform.position).magnitude < dist)
            {
                closestEnemy = enemy;
                dist = (enemy.transform.position - transform.position).magnitude;
            }
        }

        // Return the nearest enemy
        return closestEnemy;
    }
}
