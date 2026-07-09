using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Called by the Player vs Player button
    public void SelectLocalPvPMode()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewMatch(vsComputerAI: false);
        }
    }

    // Called by the Player vs AI button
    public void SelectVsAIMode()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewMatch(vsComputerAI: true);
        }
    }

    // Called by a Quit button (optional store requirement)
    public void QuitGameApplication()
    {
        Debug.Log("🔌 Exiting match client application...");
        Application.Quit();
    }

    /// <summary>
    /// Invoked by the gameplay HUD button to step cleanly back to the splash screen.
    /// </summary>
    public void ReturnToMainMenuScene()
    {
        Debug.Log("🏠 Tearing down match state; returning to Main Menu...");
        SceneManager.LoadScene("MainMenu"); // Must match your menu scene asset file name exactly!
    }
}