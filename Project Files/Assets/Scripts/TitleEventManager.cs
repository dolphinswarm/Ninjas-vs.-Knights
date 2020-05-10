using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages events on the title screen UI.
/// </summary>
public class TitleEventManager : MonoBehaviour
{
    // ====================================================== Properties
    [Header("UI Objects")]
    private TMP_Text difficultySliderLabel;     // The label for the difficulty slider
    private static GameObject instance;         // An instance of this object, for checking.

    [Header("Game Settings")]
    public float difficulty = 1.0f;             // The difficulty of the upcoming game.

    // ====================================================== Methods
    /// <summary>
    /// On game start..
    /// </summary>
    void Start()
    {
        // Checks for duplicate instances
        DontDestroyOnLoad(gameObject);
        if (instance == null)
            instance = gameObject;
        else
            Destroy(gameObject);

        // Get difficulty slider label
        difficultySliderLabel = GameObject.Find("Slider Label").GetComponent<TMP_Text>();
    }

    /// <summary>
    /// On frame update...
    /// </summary>
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Title")
            GetComponentInChildren<Canvas>().enabled = true;
        else
            GetComponentInChildren<Canvas>().enabled = false;

        // If escape key pressed, quit game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    /// <summary>
    /// On difficulty slider move...
    /// </summary>
    public void OnDifficultySliderMove(float value)
    {
        // Do handling based on value
        if (value == 1.0f)
            difficultySliderLabel.text = "Difficulty: Easy";
        else if (value == 2.0f)
            difficultySliderLabel.text = "Difficulty: Medium";
        else if (value == 3.0f)
            difficultySliderLabel.text = "Difficulty: Hard";
        else if (value == 4.0f)
            difficultySliderLabel.text = "Difficulty: IMPOSSIBLE!";
        else
            difficultySliderLabel.text = "Error text!";

        // Set the number of enemies
        difficulty = value;
    }

    /// <summary>
    /// Starts the game.
    /// </summary>
    public void StartGame()
    {
        // Load the game
        SceneManager.LoadScene("Game");

        // Hide the UI
        GetComponentInChildren<Canvas>().enabled = false;
    }
}