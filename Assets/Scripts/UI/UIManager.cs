using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Element Assignments")]
    public TextMeshProUGUI turnText; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateTurnDisplay(bool isXTurn, Color32 playerColor)
    {
        if (turnText == null) return; // Zero-crash safety guard rails!

        // Custom formatting to show Player 1 (X) or Player 2 (O) explicitly
        string playerLabel = isXTurn ? "PLAYER 1 (X)" : "PLAYER 2 (O)";
        turnText.text = $"{playerLabel}'S TURN";
        turnText.color = playerColor;
    }

    // 🌟 NEW METHOD: Safe end-of-game notification handler
    public void DisplayMatchWinner(string winnerLabel, Color32 winnerColor)
    {
        if (turnText == null) return;

        turnText.text = $"{winnerLabel} WINS THE GAME!";
        turnText.color = winnerColor;
    }
}