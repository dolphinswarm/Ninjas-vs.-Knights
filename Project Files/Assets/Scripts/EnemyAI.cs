using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using AimIK;
using UnityEngine.AI;

/// <summary>
/// An enumeration of enemy states.
/// </summary>
public enum EnemyState { WalkingTowardsMiddle, WalkingTowardsFlag, 
                         WalkingTowardsBase, RetrievingEnemyFlag, 
                         RetrievingPlayerFlag, ShootingAtPlayer, Reloading, NULL }

/// <summary>
/// The script for controlling AI through NavMeshes.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    // ============================================= Variables
    [Header("Controls and Animations")]
    private Animator animator;                  // The animator for the controller.
    private AimIKBehaviour aimer;               // The script for controlling aim.
    //private Stack<KeyValuePair<Transform, EnemyState>> previousStateStack;      // A stack of previous command calls
    private EnemyState previousState = EnemyState.NULL;
    private Transform previousNavMeshTarget = null;
    private Transform preReloadTarget = null;
    public EnemyState currentState;             // The current state of this enemy.

    public float speed;

    [Header("Movement and AI")]
    public Transform navMeshTarget;             // The target of the nav mesh agent.
    private NavMeshAgent navMeshAgent;          // The nav mesh agent of this object.
    private Transform player;                   // The transform of the player
    public int behavior = 0;                    // Sets random behavior for the AI.
    [Range(2.0f, 20.0f)]
    public float playerShootDistance = 10.0f;   // The distance at which we should shoot the player from.
    public GunScript gun;                       // The enemy's gun.
    public bool currentlyAiming;                // Is this enemy currently aiming?
    public float difficulty = 1.0f;
    public GameObject nearestPlayer;

    private FlagScript otherTeamFlag;
    private FlagScript myTeamFlag;

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

        // Set the aimer and disable it
        aimer = GetComponentInChildren<AimIKBehaviour>();
        aimer.enabled = false;

        // Get the player transform
        //player = FindObjectOfType<PlayerController>().gameObject.transform;

        // Set the default state and target
        currentState = EnemyState.WalkingTowardsMiddle;
        SetTarget();

        // Add to previous call stack
        //previousStateStack = new Stack<KeyValuePair<Transform, EnemyState>>();
        //previousStateStack.Push(
        //    new KeyValuePair<Transform, EnemyState>(navMeshTarget, currentState));

        // Update the behavior
        behavior = Random.Range(0, 2);

        // Add flags
        otherTeamFlag = gameManager.playerFlag.GetComponent<FlagScript>();
        myTeamFlag = gameManager.enemyFlag.GetComponent<FlagScript>();

        // Set movement speed
        navMeshAgent.speed = difficulty + 1.5f;

        // Set shooting distance
        playerShootDistance = 10.0f + ((difficulty - 1.0f) * 5.0f);

        // Set gun clip size
        gun.clipSize *= Mathf.CeilToInt(difficulty / 2.0f);
        gun.remainingBullets = gun.clipSize;
        gun.damage *= Mathf.Max(1.0f, difficulty / 3.0f);

    }

    // Update is called once per frameenemyFlag
    void Update()
    {
        // Get the nearest player
        nearestPlayer = GetNearestPlayer();

        // PRIORITY #1 - IF PLAYER NEARBY, SHOOT AT
        if (nearestPlayer != null && (transform.position - nearestPlayer.transform.position).magnitude < playerShootDistance &&
                  currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == EnemyState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to shooting
            currentState = EnemyState.ShootingAtPlayer;

            // Set new target to be running away from player
            Vector3 newPosition = transform.position + (transform.position - nearestPlayer.transform.position);
            navMeshTarget = nearestPlayer.transform;
            navMeshAgent.SetDestination(newPosition);
        }

        // PRIORITY #2 - ELSE, IF MY TEAM FLAG IS NEARBY UNCOLLECTED, COLLECT IT
        else if ((transform.position - myTeamFlag.transform.position).magnitude < 25.0f &&
                  currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading &&
                  !myTeamFlag.currentlyCollected && myTeamFlag.hasBeenStolen)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == EnemyState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to retrieving
            currentState = EnemyState.RetrievingEnemyFlag;

            // Set new target to be enemy flag
            navMeshTarget = myTeamFlag.transform;
            navMeshAgent.SetDestination(navMeshTarget.position);
        }

        // PRIORITY #3 - ELSE, IF OTHER TEAM FLAG IS NEARBY UNCOLLECTED, COLLECT IT
        else if ((transform.position - otherTeamFlag.transform.position).magnitude < 25.0f &&
          currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading &&
          currentState != EnemyState.RetrievingEnemyFlag && !otherTeamFlag.currentlyCollected && otherTeamFlag.hasBeenStolen)
        {
            // If previous stuff is null, push to it
            if (previousNavMeshTarget == null && currentState == EnemyState.NULL)
            {
                previousNavMeshTarget = navMeshTarget;
                previousState = currentState;
            }

            // Set the state to retrieving
            currentState = EnemyState.RetrievingPlayerFlag;

            // Set new target to be player flag
            navMeshTarget = otherTeamFlag.transform;
            navMeshAgent.SetDestination(navMeshTarget.position);
        }

        // PRIORITY #4 (NORMAL) - NONE OF THE ABOVE, SO BUSINESS AS NORMAL
        else
        {
            // IF NO PLAYER NEARBY AFTER SHOOTING, REVERT TO PREVIOUS STATE
            if (currentState == EnemyState.ShootingAtPlayer && (nearestPlayer == null ||
                (nearestPlayer.transform.position - nearestPlayer.transform.position).magnitude >= playerShootDistance))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = EnemyState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, IF NO ENEMY FLAG NEARBY, REVERT TO PREVIOUS STATE
            else if (currentState == EnemyState.RetrievingEnemyFlag &&
                ((transform.position - myTeamFlag.transform.position).magnitude >= playerShootDistance ||
                myTeamFlag.currentlyCollected))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = EnemyState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, IF NO ENEMY FLAG NEARBY, REVERT TO PREVIOUS STATE
            else if (currentState == EnemyState.RetrievingPlayerFlag && 
                ((transform.position - otherTeamFlag.transform.position).magnitude >= playerShootDistance ||
                  otherTeamFlag.currentlyCollected))
            {
                // Set back to previous states
                currentState = previousState;
                navMeshTarget = previousNavMeshTarget;

                // Reset previous states
                previousState = EnemyState.NULL;
                previousNavMeshTarget = null;

                // Reset target
                if (navMeshTarget != null)
                    navMeshAgent.SetDestination(navMeshTarget.position);
            }

            // ELSE, DO NORMAL TRANSITIONS
            // Check the distance remaining. If none, change the state then find a new target
            else if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance &&
            navMeshAgent.velocity.sqrMagnitude == 0.0f && currentState != EnemyState.Reloading)
            {
                // If at midpoint...
                if (currentState == EnemyState.WalkingTowardsMiddle)
                {
                    // If we don't have flag, then run towards flag
                    if (!isHoldingFlag)
                        currentState = EnemyState.WalkingTowardsFlag;

                    // Else, we do, so run towards base
                    else
                        currentState = EnemyState.WalkingTowardsBase;
                }
                // Else, if at base for some reason and not holding flag, go back towards middle
                else if (currentState == EnemyState.WalkingTowardsBase && !isHoldingFlag)
                {
                    currentState = EnemyState.WalkingTowardsMiddle;
                }

                // Set the target
                SetTarget();
            }
            // If currently holding flag or a teammate is...
            else if (isHoldingFlag || otherTeamFlag.currentlyCollected)
            {
                // Update the state
                if (currentState == EnemyState.WalkingTowardsFlag)
                {
                    // If our behavior should be to run to the furthest midpoint first, do that
                    if (behavior == 0)
                    {
                        currentState = EnemyState.WalkingTowardsMiddle;
                        navMeshTarget = FindFurthestMidpoint();
                        navMeshAgent.SetDestination(navMeshTarget.position);
                    }
                    // Else, go directly to base
                    else
                    {
                        currentState = EnemyState.WalkingTowardsBase;
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
            if (currentState == EnemyState.ShootingAtPlayer && gun != null)
            {
                speed = -speed * 0.5f;
                transform.LookAt(nearestPlayer.transform);
                if (!gun.currentlyAiming)
                    gun.currentlyAiming = true;
                gun.EnemyShoot();
            }
            else
            {
                if (gun != null && gun.currentlyAiming)
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
        nearestPlayer = GetNearestPlayer();
        
        // If walking towards the middle...
        if (currentState == EnemyState.WalkingTowardsMiddle)
            navMeshTarget = FindClosestMidpoint();

        // Else, if walking towards flag...
        else if (currentState == EnemyState.WalkingTowardsFlag)
            navMeshTarget = gameManager.playerFlag.transform;

        // Else, if walking towards base...
        else if (currentState == EnemyState.WalkingTowardsBase)
            navMeshTarget = gameManager.enemyGoal.transform;

        // Else, if shooting at player...
        else if (nearestPlayer != null && currentState == EnemyState.ShootingAtPlayer && 
                (transform.position - nearestPlayer.transform.position).magnitude < playerShootDistance)
        {
            // Get a new far position
            Vector3 newPosition = transform.position + (transform.position - nearestPlayer.transform.position);
            navMeshTarget = nearestPlayer.transform;
            navMeshAgent.SetDestination(newPosition);

            // Return to prevent an error
            return;
        }

        // Set NavMeshAgent destination
        if (navMeshTarget == null)
        {
            if (isHoldingFlag)
            {
                currentState = EnemyState.WalkingTowardsBase;
                navMeshTarget = gameManager.enemyGoal.transform;
            }
            else
            {
                currentState = EnemyState.WalkingTowardsFlag;
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
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died and dropped your flag!";
            } 
            else
                gameManager.eventText.text = name.Replace("(Clone)", "") + " died!";


            // Play death scream
            AudioSource.PlayClipAtPoint(death, transform.position);

            // Remove from enemies list
            gameManager.enemies.Remove(gameObject);

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
        foreach(GameObject midpoint in GameObject.FindGameObjectsWithTag("Midpoint"))
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
            currentState = EnemyState.ShootingAtPlayer;
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
            currentState = EnemyState.Reloading;
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
    public GameObject GetNearestPlayer()
    {
        // For each player in enemies
        float dist = float.PositiveInfinity;
        GameObject closestPlayer = null;
        foreach (GameObject player in gameManager.players)
        {
            if ((player.transform.position - transform.position).magnitude < dist)
            {
                closestPlayer = player;
                dist = (player.transform.position - transform.position).magnitude;
            }
        }

        // Return the nearest player
        return closestPlayer;
    }
}


//// Check the distance remaining. If none, change the state then find a new target
//        if (!navMeshAgent.pathPending && navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance && 
//            navMeshAgent.velocity.sqrMagnitude == 0.0f && currentState != EnemyState.Reloading)
//        {
//            // If at midpoint...
//            if (currentState == EnemyState.WalkingTowardsMiddle)
//            {
//                // If we don't have flag, then run towards flag
//                if (!isHoldingFlag)
//                    currentState = EnemyState.WalkingTowardsFlag;

//                // Else, we do, so run towards base
//                else
//                    currentState = EnemyState.WalkingTowardsBase;
//            }
                
//            // Set the target
//            SetTarget();
//        }
//        // If there is a player nearby...
//        else if ((transform.position - player.position).magnitude<playerShootDistance && 
//                  currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading)
//        {
//            // Set the state to shooting
//            previousState = currentState;
//            previousTarget = navMeshTarget;
//            currentState = EnemyState.ShootingAtPlayer;

//            // Set new target to be running away from player
//            Vector3 newPosition = transform.position + (transform.position - player.position);
//navMeshTarget = null;
//            navMeshAgent.SetDestination(newPosition);
//        }
//        // If enemy flag is nearby, collect it
//        else if ((transform.position - myTeamFlag.transform.position).magnitude< 25.0f &&
//                  currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading &&
//                  myTeamFlag.currentlyCollected)
//        {
//            // Set the state to shooting
//            previousState = currentState;
//            previousTarget = navMeshTarget;
//            currentState = EnemyState.RetrievingFlag;

//            // Set new target to be running away from player
//            navMeshTarget = myTeamFlag.transform;
//            navMeshAgent.SetDestination(navMeshTarget.position);
//        }
//        // If player flag is nearby, collect it
//        else if ((transform.position - otherTeamFlag.transform.position).magnitude< 25.0f &&
//                  currentState != EnemyState.ShootingAtPlayer && currentState != EnemyState.Reloading &&
//                  currentState != EnemyState.WalkingTowardsFlag && !otherTeamFlag.currentlyCollected)
//        {
//            // Set the state to shooting
//            previousState = currentState;
//            previousTarget = navMeshTarget;
//            currentState = EnemyState.RetrievingFlag;

//            // Set new target to be running away from player
//            navMeshTarget = otherTeamFlag.transform;
//            navMeshAgent.SetDestination(navMeshTarget.position);
//        }
//        // Else, reset to previous state
//        else if ((currentState == EnemyState.ShootingAtPlayer && 
//                (transform.position - player.position).magnitude >= playerShootDistance))
//        {
//            currentState = previousState;
//            navMeshTarget = previousTarget;
//            navMeshAgent.SetDestination(navMeshTarget.position);
//        }
//        // If currently holding flag...
//        else if (isHoldingFlag)
//        {
//            // Update the state
//            if (currentState == EnemyState.WalkingTowardsFlag)
//            {
//                // If our behavior should be to run to the furthest midpoint first, do that
//                if (behavior == 0)
//                {
//                    currentState = EnemyState.WalkingTowardsMiddle;
//                    navMeshTarget = FindFurthestMidpoint();
//navMeshAgent.SetDestination(navMeshTarget.position);
//                }
//                // Else, go directly to base
//                else
//                {
//                    currentState = EnemyState.WalkingTowardsBase;
//                    SetTarget();
//                }
//            }
//        }
