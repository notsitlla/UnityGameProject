using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Debug.Log("GameManager Initialized");
    }

    private void Start()
    {
        Debug.Log("Welcome to Ultimate TicTacToe");
    }
}