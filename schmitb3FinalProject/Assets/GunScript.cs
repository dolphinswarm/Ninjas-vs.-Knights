using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// The script for controlling a gun.
/// </summary>
public class GunScript : MonoBehaviour
{
    // ============================================= Variables
    [Header("Gun Behavior")]
    public float damage = 0.0f;
    public float fireRate;
    public int clipSize;
    public int remainingBullets;
    public bool currentlyAiming = false;
    public GameObject bullet;
    public GameObject muzzleFire;
    public bool currentlyReloading = false;

    private bool adjustOnce = false;
    private GameObject shootLocation;
    private float timeUntilNextFire = 0.0f;
    private GameObject indicator;

    [Header("Audio")]
    public AudioClip gunshot;
    public AudioClip gunCock;

    [Header("Object Properties")]
    public GameObject owningGameObject;
    public bool nPCGun = false;                 // Is this gun an NPC gun?
    public GameManager gameManager;

    private Rigidbody rigidBody;
    private Transform player;                   // The transform of the player
    private PlayerController playerController;  // The player controller.
    private Vector3 ninjaHandPosition = new Vector3(0.09485f, 0.23196f, -0.02541f);
    private Vector3 ninjaHandRotation = new Vector3(96.826f, -94.187f, -177.462f);
    private Vector3 ninjaAimPosition = new Vector3(-0.05f, -0.06f, 0.22f);
    private Vector3 ninjaAimRotation = new Vector3(30.737f, 174.02f, 90.14f);
    private bool raycast = true;

    // ============================================= Methods
    /// <summary>
    /// On game start...
    /// </summary>
    void Start()
    {
        // Set the game manager, if not set
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        // Set the rigidbody
        rigidBody = GetComponent<Rigidbody>();

        // Set the current number of rounds to the clip size
        remainingBullets = clipSize;

        // Get the player transform and controller
        //try
        //{
        //    playerController = FindObjectOfType<PlayerController>();
        //    player = playerController.transform;
        //}
        //catch (System.Exception e)
        //{
        //    playerController = null;
        //    player = gameManager.transform
        //}


        // Set the shoot location
        shootLocation = transform.Find("Shoot Location").gameObject;

        // Set the indicator
        indicator = transform.Find("Canvas").gameObject;
        indicator.transform.Find("Label").GetComponent<TMP_Text>().text = name.Replace("(Clone)", "");
        indicator.SetActive(false);
    }

    void Update()
    {
        // Adjust time until next fire
        timeUntilNextFire -= Time.deltaTime;

        // If not an NPC gun...
        if (!nPCGun) {

            // Check distance to player
            float distance = Mathf.Infinity;
            if (player != null)
                distance = (transform.position - player.position).magnitude;

            // If below the floor, destroy it
            if (transform.position.y < -50.0f) Destroy(gameObject);

            // Raycast beneath me to make sure I don't fall through floor :(
            if (raycast)
            {
                if (Physics.Raycast(new Ray(transform.position, Vector3.down), 2.0f) && rigidBody.velocity.magnitude > 1.0f)
                {
                    // Remove velocity
                    rigidBody.velocity = Vector3.zero;
                }
            }

            // If the player has a gun...
            if (playerController.hasGun)
            {
                // If we are the current gun...
                if (playerController.gun == this)
                {
                    // If we want to drop the gun
                    if (Input.GetKeyDown(KeyCode.Q))
                    {
                        // Remove from parent
                        transform.parent = null;
                        owningGameObject = null;

                        // Disable the rigidbody
                        rigidBody.isKinematic = false;
                        rigidBody.useGravity = true;

                        // Turn on indicator
                        indicator.SetActive(true);

                        // Make the player stop aiming
                        currentlyAiming = false;
                        playerController.hasGun = false;
                        playerController.gun = null;
                    }

                    // If we are aiming, readjust position
                    if (currentlyAiming && !adjustOnce)
                    {
                        // Set the transform
                        transform.localPosition = ninjaAimPosition;
                        //if (gameObject.name.Contains("Shotgun"))
                        //    transform.localPosition += Vector3.down * 0.1f;
                        transform.localRotation = Quaternion.Euler(ninjaAimRotation);

                        // Toggle the bool
                        adjustOnce = true;
                    }
                    // Else, reset position
                    else if (!currentlyAiming && adjustOnce)
                    {
                        // Set the transform
                        transform.localPosition = ninjaHandPosition;
                        transform.localRotation = Quaternion.Euler(ninjaHandRotation);

                        // Toggle the bool
                        adjustOnce = false;
                    }

                    // If we are currently aiming and the mouse is clicked, fire bullets
                    if (currentlyAiming && timeUntilNextFire <= 0.0f && Input.GetMouseButton(0) && 
                        playerController.turn < 0.05f && playerController.turn > -0.05f &&
                        remainingBullets > 0)
                    {
                        // If a shotgun...
                        if (gameObject.name.Contains("Shotgun"))
                        {
                            // Instantiate 10 random shots
                            GameObject[] shotgunBullets = new GameObject[10];
                            for (int i = 0; i < 10; i++)
                            {
                                // Generate a random angle
                                Vector3 currentAngles = shootLocation.transform.rotation.eulerAngles;
                                Quaternion randomizedRotation = Quaternion.Euler(new Vector3(currentAngles.x + Random.Range(-10.0f, 10.0f),
                                                                                             currentAngles.y + Random.Range(-10.0f, 10.0f),
                                                                                             currentAngles.z));

                                // Instantiate the bullet
                                shotgunBullets[i] = Instantiate(bullet, shootLocation.transform.position, randomizedRotation);
                                shotgunBullets[i].GetComponent<BulletScript>().damage = damage;
                                shotgunBullets[i].GetComponent<BulletScript>().shooter = "Player";
                                Destroy(shotgunBullets[i], 1.0f);
                            }
                        }
                        // Else, a regular gun
                        else
                        {
                            // Fire a bullet and destroy it a second later
                            GameObject shotBullet = Instantiate(bullet, shootLocation.transform.position, shootLocation.transform.rotation);
                            shotBullet.GetComponent<BulletScript>().damage = damage;
                            shotBullet.GetComponent<BulletScript>().shooter = "Player";
                            Destroy(shotBullet, 1.0f);
                        }

                        // Play the gunshot sound
                        AudioSource.PlayClipAtPoint(gunshot, transform.position);

                        // Instantiate muzzle fire and destroy it 1/10 a second later
                        GameObject shotMuzzleFire = Instantiate(muzzleFire, shootLocation.transform);
                        Destroy(shotMuzzleFire, 0.1f);

                        // Set the time until next fire
                        timeUntilNextFire = fireRate;

                        // Decrease the number of remaining bullets
                        remainingBullets--;

                        // Update the UI
                        gameManager.ammoCounter.GetComponent<TMP_Text>().text = remainingBullets + " / " + clipSize;

                        // Check if we are out of bullets, reload
                        if (remainingBullets <= 0)
                        {
                            Reload();
                        }
                    }
                }
            }
            // Else, if the player doesn't have a gun
            else
            {
                // If player is close enough...
                if (distance < 10.0f)
                {
                    // Turn on indicator
                    indicator.SetActive(true);

                    // If we press down mouse, get the nearest gun
                    if (distance < 3.0f && Input.GetMouseButtonDown(0) && IsClosestGunToPlayer())
                    {
                        // Play the gunshot sound
                        AudioSource.PlayClipAtPoint(gunCock, transform.position);

                        // Set the player's hand to the transform
                        transform.parent = playerController.rightHand.transform;
                        owningGameObject = transform.parent.gameObject;
                        playerController.hasGun = true;
                        playerController.gun = this;

                        // Disable the rigidbody
                        rigidBody.isKinematic = true;
                        rigidBody.useGravity = false;

                        // Adjust the position and rotation
                        transform.localPosition = ninjaHandPosition;
                        transform.localRotation = Quaternion.Euler(ninjaHandRotation);

                        // Turn off indicator
                        indicator.SetActive(false);

                        // Update the UI
                        gameManager.ammoCounter.GetComponent<TMP_Text>().text = remainingBullets + " / " + clipSize;
                    }
                }
                // Else, turn off indicator
                else
                {
                    // Turn off indicator
                    if (indicator.activeSelf)
                        indicator.SetActive(false);
                }
            

            }
        }
    }

    /// <summary>
    /// Checks if this gun is the closes to the player.
    /// </summary>
    /// <returns></returns>
    private bool IsClosestGunToPlayer()
    {
        // Loop through each gun in the scene
        foreach(GameObject gun in GameObject.FindGameObjectsWithTag("Gun"))
        {
            // Get the gun script
            GunScript tmp = gun.GetComponent<GunScript>();

            // Compare distances
            if (DistanceToPlayer() > tmp.DistanceToPlayer()) return false;
        }

        // Return true
        return true;
    }

    /// <summary>
    /// Gets this gun's distance to the player.
    /// </summary>
    /// <returns>This gun's distance from the player.</returns>
    public float DistanceToPlayer()
    {
        return (transform.position - player.position).magnitude;
    }

    /// <summary>
    /// On trigger enter, toggle raycaster.
    /// </summary>
    /// <param name="other"></param>
    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Stage")) raycast = false;
    }

    /// <summary>
    /// Enemy gun shooting.
    /// </summary>
    public void EnemyShoot()
    {
        // If we can shoot the gun...
        if (currentlyAiming && timeUntilNextFire <= 0.0f &&
            !currentlyReloading)
        {
            // If a shotgun...
            if (gameObject.name.Contains("Shotgun"))
            {
                // Instantiate 10 random shots
                GameObject[] shotgunBullets = new GameObject[10];
                for (int i = 0; i < 10; i++)
                {
                    // Generate a random angle
                    Vector3 currentAngles = shootLocation.transform.rotation.eulerAngles;
                    Quaternion randomizedRotation = Quaternion.Euler(new Vector3(currentAngles.x + Random.Range(-10.0f, 10.0f),
                                                                                 currentAngles.y + Random.Range(-10.0f, 10.0f),
                                                                                 currentAngles.z));

                    // Instantiate the bullet
                    shotgunBullets[i] = Instantiate(bullet, shootLocation.transform.position, randomizedRotation);
                    shotgunBullets[i].GetComponent<BulletScript>().damage = damage;
                    shotgunBullets[i].GetComponent<BulletScript>().shooter = "Enemy";
                    Destroy(shotgunBullets[i], 1.0f);
                }
            }
            // Else, a regular gun
            else
            {
                // Fire a bullet and destroy it a second later
                GameObject shotBullet = Instantiate(bullet, shootLocation.transform.position, shootLocation.transform.rotation);
                shotBullet.GetComponent<BulletScript>().damage = damage;
                shotBullet.GetComponent<BulletScript>().shooter = "Enemy";
                Destroy(shotBullet, 1.0f);
            }

            // Play the gunshot sound
            AudioSource.PlayClipAtPoint(gunshot, transform.position);

            // Instantiate muzzle fire and destroy it 1/10 a second later
            GameObject shotMuzzleFire = Instantiate(muzzleFire, shootLocation.transform);
            Destroy(shotMuzzleFire, 0.1f);

            // Set the time until next fire
            timeUntilNextFire = fireRate;

            // Decrease the number of remaining bullets
            remainingBullets--;

            // Check if we are out of bullets, reload
            if (remainingBullets <= 0 && !currentlyReloading)
            {
                Reload();
            }
        }
    }

    /// <summary>
    /// Player AI gun shooting.
    /// </summary>
    public void PlayerAIShoot()
    {
        // If we can shoot the gun...
        if (currentlyAiming && timeUntilNextFire <= 0.0f &&
            !currentlyReloading)
        {
            // If a shotgun...
            if (gameObject.name.Contains("Shotgun"))
            {
                // Instantiate 10 random shots
                GameObject[] shotgunBullets = new GameObject[10];
                for (int i = 0; i < 10; i++)
                {
                    // Generate a random angle
                    Vector3 currentAngles = shootLocation.transform.rotation.eulerAngles;
                    Quaternion randomizedRotation = Quaternion.Euler(new Vector3(currentAngles.x + Random.Range(-10.0f, 10.0f),
                                                                                 currentAngles.y + Random.Range(-10.0f, 10.0f),
                                                                                 currentAngles.z));

                    // Instantiate the bullet
                    shotgunBullets[i] = Instantiate(bullet, shootLocation.transform.position, randomizedRotation);
                    shotgunBullets[i].GetComponent<BulletScript>().damage = damage;
                    shotgunBullets[i].GetComponent<BulletScript>().shooter = "Player";
                    Destroy(shotgunBullets[i], 1.0f);
                }
            }
            // Else, a regular gun
            else
            {
                // Fire a bullet and destroy it a second later
                GameObject shotBullet = Instantiate(bullet, shootLocation.transform.position, shootLocation.transform.rotation);
                shotBullet.GetComponent<BulletScript>().damage = damage;
                shotBullet.GetComponent<BulletScript>().shooter = "Player";
                Destroy(shotBullet, 1.0f);
            }

            // Play the gunshot sound
            AudioSource.PlayClipAtPoint(gunshot, transform.position);

            // Instantiate muzzle fire and destroy it 1/10 a second later
            GameObject shotMuzzleFire = Instantiate(muzzleFire, shootLocation.transform);
            Destroy(shotMuzzleFire, 0.1f);

            // Set the time until next fire
            timeUntilNextFire = fireRate;

            // Decrease the number of remaining bullets
            remainingBullets--;

            // Check if we are out of bullets, reload
            if (remainingBullets <= 0 && !currentlyReloading)
            {
                Reload();
            }
        }
    }

    /// <summary>
    /// The public visible method for reloading
    /// </summary>
    public void Reload()
    {
        ReloadStart();
    }

    /// <summary>
    /// Starts the reload animation.
    /// </summary>
    private void ReloadStart()
    {
        // Toggle currently reloading
        currentlyReloading = true;

        // Get the animation duration
        float duration = 0.0f;
        if (owningGameObject.tag == "EnemyHand")
            duration = transform.GetComponentInParent<EnemyAI>().ToggleReload();
        else if (owningGameObject.tag == "PlayerAIHand")
            duration = transform.GetComponentInParent<PlayerAI>().ToggleReload();
        else
            duration = transform.GetComponentInParent<PlayerController>().ToggleReload();

        // Start the wait coroutine
        StartCoroutine(WaitForAnimation(duration));
    }
    
    /// <summary>
    /// Ends the reload animation.
    /// </summary>
    private void ReloadEnd()
    {
        // Play the gunshot sound
        AudioSource.PlayClipAtPoint(gunCock, transform.position);

        // Reset the bullets
        remainingBullets = clipSize;

        // Update the UI
        gameManager.ammoCounter.GetComponent<TMP_Text>().text = remainingBullets + " / " + clipSize;

        // Un-toggle the reload
        if (owningGameObject.tag == "EnemyHand")
            transform.GetComponentInParent<EnemyAI>().ToggleReload();
        else if (owningGameObject.tag == "PlayerAIHand")
            transform.GetComponentInParent<PlayerAI>().ToggleReload();
        else
            transform.GetComponentInParent<PlayerController>().ToggleReload();

        // Toggle currently reloading
        currentlyReloading = false;
    }

    /// <summary>
    /// A coroutine for waiting to toggle off reload.
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    IEnumerator WaitForAnimation(float seconds)
    {
        // Wait for some seconds
        yield return new WaitForSeconds(seconds);

        // Call reload end
        ReloadEnd();
    }

    /// <summary>
    /// Sets the player for this object.
    /// </summary>
    /// <param name="player"></param>
    public void SetPlayer(PlayerController playerController)
    {
        this.playerController = playerController;
        player = playerController.transform;
    }
}


