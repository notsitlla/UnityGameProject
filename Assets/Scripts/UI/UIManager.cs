using UnityEngine;
using TMPro; // Crucial for handling TextMeshPro components

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Element Assignments")]
    public TextMeshProUGUI turnText; // Target our HUD element

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateTurnDisplay(bool isXTurn, Color32 playerColor)
    {
        if (turnText == null) return;

        // Dynamically update context content strings and colors
        string playerLabel = isXTurn ? "PLAYER X" : "PLAYER O";
        turnText.text = $"{playerLabel}'S TURN";
        turnText.color = playerColor;
    }
}