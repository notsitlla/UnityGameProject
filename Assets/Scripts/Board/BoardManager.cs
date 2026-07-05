using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;

    [Header("Piece Prefabs")]
    public GameObject xPrefab;
    public GameObject oPrefab;
    public GameObject highlightPrefab;

    [Header("Game State Tracking")]
    public int activeMiniBoard = -1; 
    public Player currentTurn = Player.X; // Clean explicit turn selector state tracking
    public bool isGameOver = false;

    // Upgraded datasets utilizing strongly typed Enums!
    private CellState[,] globalBoardData = new CellState[9, 9]; 
    private BoardState[] miniBoardStates = new BoardState[9];     

    [Header("Player Visual Theme Colors")]
    public Color32 colorX = new Color32(255, 80, 90, 255);  
    public Color32 colorO = new Color32(80, 170, 255, 255); 

    private Dictionary<int, GameObject> boardHighlightPanels = new Dictionary<int, GameObject>();
    private Dictionary<string, Cell> globalCells = new Dictionary<string, Cell>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    private void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnDisplay(currentTurn == Player.X, colorX);
        }
        RefreshBoardHighlights();
    }

    public void RegisterCell(int miniBoard, int index, Cell cell)
    {
        string key = $"{miniBoard}_{index}";
        if (!globalCells.ContainsKey(key)) globalCells.Add(key, cell);
    }

    public void SpawnHighlightObject(int miniBoardIndex, Vector3 centerPosition)
    {
        if (!boardHighlightPanels.ContainsKey(miniBoardIndex))
        {
            GameObject hl = Instantiate(highlightPrefab, centerPosition, Quaternion.identity, transform);
            // preserve prefab's configured localScale
            hl.SetActive(false);
            boardHighlightPanels.Add(miniBoardIndex, hl);
        }
    }

    public void OnCellClicked(Cell clickedCell)
    {
        if (isGameOver) return;

        int bIdx = clickedCell.miniBoardIndex;
        int lIdx = clickedCell.localIndex;

        if (activeMiniBoard != -1 && activeMiniBoard != bIdx) return;
        if (miniBoardStates[bIdx] != BoardState.Active) return;
        if (globalBoardData[bIdx, lIdx] != CellState.Empty) return; 

        // Apply updated enum values directly to structural storage cells
        globalBoardData[bIdx, lIdx] = (currentTurn == Player.X) ? CellState.X : CellState.O;

        GameObject tokenPrefab = (currentTurn == Player.X) ? xPrefab : oPrefab;
        GameObject token = Instantiate(tokenPrefab, clickedCell.transform.position, Quaternion.identity, clickedCell.transform);
        token.transform.localPosition = Vector3.zero;
        
        token.transform.localScale = Vector3.zero;
        StartCoroutine(AnimatePieceSpawn(token.transform));
        clickedCell.ClaimCell(Color.clear); 

        CheckMiniBoardWin(bIdx, currentTurn);
        CheckMacroGameWin(currentTurn);

        if (!isGameOver)
        {
            UpdateActiveTargetZone(lIdx);
            currentTurn = (currentTurn == Player.X) ? Player.O : Player.X;
            RefreshBoardHighlights();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTurnDisplay(currentTurn == Player.X, currentTurn == Player.X ? colorX : colorO);
            }
        }
    }

    private System.Collections.IEnumerator AnimatePieceSpawn(Transform target)
    {
        float elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, elapsed / 0.12f);
            target.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private void UpdateActiveTargetZone(int targetedLocalIndex)
    {
        if (miniBoardStates[targetedLocalIndex] != BoardState.Active)
        {
            activeMiniBoard = -1; // Standard open grid play allowance choice rules
        }
        else
        {
            activeMiniBoard = targetedLocalIndex;
        }
    }

    private void RefreshBoardHighlights()
    {
        foreach (var kvp in boardHighlightPanels)
        {
            bool shouldHighlight = (activeMiniBoard == -1 || activeMiniBoard == kvp.Key) && miniBoardStates[kvp.Key] == BoardState.Active;
            kvp.Value.SetActive(shouldHighlight);
        }
    }

    private void CheckMiniBoardWin(int boardIndex, Player player)
    {
        CellState targetState = (player == Player.X) ? CellState.X : CellState.O;

        int[][] winPatterns = new int[][]
        {
            new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8},
            new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8},
            new int[] {0,4,8}, new int[] {2,4,6}
        };

        foreach (int[] pattern in winPatterns)
        {
            if (globalBoardData[boardIndex, pattern[0]] == targetState &&
                globalBoardData[boardIndex, pattern[1]] == targetState &&
                globalBoardData[boardIndex, pattern[2]] == targetState)
            {
                miniBoardStates[boardIndex] = (player == Player.X) ? BoardState.WonX : BoardState.WonO;
                Debug.Log($"Sub-Grid {boardIndex} captured by {player}!");
                DimCompletedBoard(boardIndex, player == Player.X ? colorX : colorO);
                return;
            }
        }

        bool isFull = true;
        for (int i = 0; i < 9; i++)
        {
            if (globalBoardData[boardIndex, i] == CellState.Empty) { isFull = false; break; }
        }
        if (isFull) miniBoardStates[boardIndex] = BoardState.Draw;
    }

    private void DimCompletedBoard(int boardIndex, Color32 winningColor)
    {
        for (int i = 0; i < 9; i++)
        {
            string key = $"{boardIndex}_{i}";
            if (globalCells.TryGetValue(key, out Cell c))
            {
                SpriteRenderer sr = c.GetComponent<SpriteRenderer>();
                sr.color = new Color32((byte)(winningColor.r / 5), (byte)(winningColor.g / 5), (byte)(winningColor.b / 5), 255);
            }
        }
    }

    private void CheckMacroGameWin(Player player)
    {
        BoardState targetWinState = (player == Player.X) ? BoardState.WonX : BoardState.WonO;

        int[][] macroWinPatterns = new int[][]
        {
            new int[] {0,1,2}, new int[] {3,4,5}, new int[] {6,7,8},
            new int[] {0,3,6}, new int[] {1,4,7}, new int[] {2,5,8},
            new int[] {0,4,8}, new int[] {2,4,6}
        };

        foreach (int[] pattern in macroWinPatterns)
        {
            if (miniBoardStates[pattern[0]] == targetWinState &&
                miniBoardStates[pattern[1]] == targetWinState &&
                miniBoardStates[pattern[2]] == targetWinState)
            {
                isGameOver = true;
                string winnerLabel = (player == Player.X) ? "PLAYER X" : "PLAYER O";
                Debug.Log($"🏆 METAGAME MATCH WINNER IS: {winnerLabel}");
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.turnText.text = $"{winnerLabel} WINS THE GAME!";
                }
                
                activeMiniBoard = -2;
                RefreshBoardHighlights();
                return;
            }
        }
    }
}