using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Global Match Options")]
    public bool playAgainstAI = false;

    private void Awake()
    {
        // Persistent Singleton pattern that survives loading a new scene!
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Invoked by Main Menu UI Buttons to boot up the match arena scene.
    /// </summary>
    public void StartNewMatch(bool vsComputerAI)
    {
        playAgainstAI = vsComputerAI;
        SceneManager.LoadScene("MainScene"); // Loads your primary gameplay layout
    }
}