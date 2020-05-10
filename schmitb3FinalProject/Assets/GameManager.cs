using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;

/// <summary>
/// The game manager for the game.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    // ============================================= Variables
    [Header("Gameplay Controls")]
    public GameObject player;                       // The main player game object.
    [Range(1.0f, 4.0f)]
    public float difficulty = 2.0f;                      // The difficulty of the game.
    [Range(1, 4)]
    public int numOfEnemies = 4;
    [Range(0, 2)]
    public int numOfPlayerAIs = 4;
    public List<GameObject> enemyPrefabs;
    public List<GameObject> playerPrefabs;
    private GameObject[] enemySpawnLocations;
    private GameObject[] playerSpawnLocations;
    private int currentEnemySpawner = 0;
    private int currentPlayerSpawner = 0;
    public GameObject playerPrefab;                 // A ninja player.
    public bool isGamePaused = false;
    private TitleEventManager titleEventManager;    // The inherited title event manager.
    public bool gameOver = false;

    [Header("Effect Controls")]
    public GoalScript playerGoal;                   // The player goal.
    public GameObject playerFlag;                   // The player's flag.
    public GoalScript enemyGoal;                    // The enemy's goal.
    public GameObject enemyFlag;                    // The enemy's flag.

    [Header("Weapon Spawning")]
    [Range(0, 50)]
    public int numberOfGuns = 15;                   // The number of guns to spawn.
    public List<GameObject> guns;                   // A list of guns that can be spawned.

    [Header("Audio")]
    public AudioClip bGM;
    public AudioClip pause;
    public AudioSource audioSource;

    [Header("Player Management")]
    public List<GameObject> players;
    public List<GameObject> enemies;

    [Header("UI")]
    public Canvas gameUI;
    public GameObject healthBar;
    public GameObject ammoCounter;
    public GameObject pauseScreen;
    public TMP_Text eventText;

    // ============================================= Methods
    // Start is called before the first frame update
    void Start()
    {
        // Spawn a player
        SpawnPlayer();

        // Get the player, if not set
        if (player == null)
            player = FindObjectOfType<PlayerController>().gameObject;

        // Set the audiosource
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Get the title event manager and attempt to get some info
        titleEventManager = FindObjectOfType<TitleEventManager>();
        if (titleEventManager != null)
        {
            difficulty = titleEventManager.difficulty;
        }

        // Set the event text
        if (eventText == null)
            eventText = GameObject.Find("Event Text").GetComponent<TMP_Text>();
        eventText.text = "";

        // Set the BGM
        audioSource.clip = bGM;
        audioSource.loop = true;
        audioSource.volume = 0.35f;
        audioSource.Play();

        // Get the spawn locations and spawn enemies
        enemySpawnLocations = GameObject.FindGameObjectsWithTag("EnemySpawn");
        for (int i = 0; i < numOfEnemies; i++)
            SpawnEnemy();        
        
        // Get the spawn locations and spawn player AIs
        playerSpawnLocations = GameObject.FindGameObjectsWithTag("PlayerSpawn");
        for (int i = 0; i < numOfPlayerAIs; i++)
            SpawnPlayerAI();

        // Set UI elements
        if (gameUI == null)
            gameUI = GameObject.Find("Game UI").GetComponent<Canvas>();

        if (healthBar == null)
            healthBar = gameUI.transform.Find("Health Bar").transform.Find("Health Bar Foreground").gameObject;

        if (ammoCounter == null)
            ammoCounter = gameUI.transform.Find("Bullet Counter").gameObject;
        ammoCounter.SetActive(false);

        // Re-hide the pause screen
        if (pauseScreen == null)
            pauseScreen = GameObject.Find("Pause UI");
        pauseScreen.SetActive(false);

        // Spawn some guns for the player
        SpawnGuns();

        // Set player references
        SetPlayerReferences(player);
    }

    // Update is called once per frame
    void Update()
    {
        // If p key pressed, pause
        if (Input.GetKeyDown(KeyCode.P)) Pause(!isGamePaused);
    }

    /// <summary>
    /// Spawns a random enemy at a given spawn location.
    /// </summary>
    public void SpawnEnemy()
    {
        // Instantiate a random knight at a specified position
        GameObject enemy = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Count)],
                           enemySpawnLocations[currentEnemySpawner % 4].transform.position,
                           enemySpawnLocations[currentEnemySpawner % 4].transform.rotation);

        // Set the difficulty
        enemy.GetComponent<EnemyAI>().difficulty = difficulty;

        // Add enemy to list of enemies
        enemies.Add(enemy);

        // Increment spawn location
        currentEnemySpawner++;
    }

    /// <summary>
    /// Spawns a random player at a given spawn location.
    /// </summary>
    public void SpawnPlayerAI()
    {
        // Instantiate a random knight at a specified position
        GameObject player = Instantiate(playerPrefabs[Random.Range(0, playerPrefabs.Count)],
                           playerSpawnLocations[currentPlayerSpawner % 2].transform.position,
                           playerSpawnLocations[currentPlayerSpawner % 2].transform.rotation);

        // Add player to list of enemies
        players.Add(player);

        // Increment spawn location
        currentPlayerSpawner++;
    }

    /// <summary>
    /// Spawns guns on the map.
    /// </summary>
    private void SpawnGuns()
    {
        // Spawn a n guns throughout the map for the player
        for (int i = 0; i < numberOfGuns; i++)
        {
            // Create a spawn position
            Vector3 spawnPosition = new Vector3(transform.position.x + Mathf.Cos(Random.Range(0.0f, 180.0f)) * 60.0f,
                                                transform.position.y,
                                                transform.position.z + Mathf.Cos(Random.Range(0.0f, 180.0f)) * 60.0f);

            // Debug the spawn position
            //Debug.Log(spawnPosition);

            // Instantiate a gun
            Instantiate(guns[Random.Range(0, guns.Count)], spawnPosition, Quaternion.Euler(Vector3.zero));
        }
    }

    /// <summary>
    /// Spawns a new controllable player in the scene.
    /// </summary>
    public void SpawnPlayer()
    {
        // Create a new player
        GameObject newPlayer = Instantiate(playerPrefab, 
                                        playerGoal.transform.Find("SpawnPoint").transform.position,
                                        playerGoal.transform.Find("SpawnPoint").transform.rotation);

        // Set the player in all other scripts that reference it
        player = newPlayer;

        // Set references
        SetPlayerReferences(newPlayer);

        // Add player to list of enemies
        players.Add(player);
    }

    /// <summary>
    /// Pauses the game.
    /// </summary>
    /// <param name="pause">Should we pause (true) or un-pause (false)?</param>
    public void Pause(bool pause)
    {
        // Set the gamemanager's pause
        isGamePaused = pause;

        // Set time scale
        if (pause)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;

        // Play pause sound
        AudioSource.PlayClipAtPoint(this.pause, Camera.main.transform.position);

        // Pause the music
        if (pause)
            audioSource.Pause();
        else
            audioSource.UnPause();

        // Hide the canvas
        gameUI.enabled = !pause;

        // Show pause UI
        pauseScreen.SetActive(pause);
    }

    /// <summary>
    /// Causes the win screen to show up.
    /// </summary>
    public void Win()
    {
        // Set game to over
        gameOver = true;

        // Load win scene
        SceneManager.LoadScene("Win Screen");
    }

    /// <summary>
    /// Causes the lose screen to show up.
    /// </summary>
    public void Lose()
    {
        // Set game to over
        gameOver = true;

        // Load win scene
        SceneManager.LoadScene("Lose Screen");
    }

    /// <summary>
    /// Sets the player references.
    /// </summary>
    public void SetPlayerReferences(GameObject newPlayer)
    {
        // Enemies
        foreach (EnemyAI enemy in FindObjectsOfType<EnemyAI>())
            enemy.SetPlayer(newPlayer.transform);

        // Player AI
        foreach (PlayerAI playerAI in FindObjectsOfType<PlayerAI>())
            playerAI.SetPlayer(newPlayer.transform);

        // Guns
        foreach (GunScript gun in FindObjectsOfType<GunScript>())
            gun.SetPlayer(newPlayer.GetComponent<PlayerController>());
    }
}
