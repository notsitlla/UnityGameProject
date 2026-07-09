using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    [Header("Piece Prefabs")]
    public GameObject xPrefab;
    public GameObject oPrefab;
    public GameObject highlightPrefab;

    [Header("Type-Safe Tracking Collections")]
    // 9x9 macro grid cell matrix representation (flattened to satisfy Unity serialization rules)
    public CellState[] globalBoardData = new CellState[81];
    // Tracking array for the 9 mini-boards
    public BoardState[] miniBoardStates = new BoardState[9];
    
    [Header("Dynamic Match Parameters")]
    public Player currentTurn;
    public int activeMiniBoard; // -1 means global wildcard free-play
    public bool isGameOver;

    [Header("Player Visual Theme Colors")]
    public Color32 colorX = new Color32(235, 94, 85, 255);  
    public Color32 colorO = new Color32(87, 166, 201, 255); 

    private Dictionary<int, GameObject> boardHighlightPanels = new Dictionary<int, GameObject>();
    private Dictionary<string, Cell> globalCells = new Dictionary<string, Cell>();

    [Header("AI Configuration Settings")]
    public bool playAgainstAI = true; // Toggle true to test your computer player!
    private bool isAIThinking = false;
    // Add this under your globalCells dictionary:
    private List<GameObject> spawnedPieces = new List<GameObject>();

    private void Awake()
    {
        // Strict scene-local singleton assignment 
        Instance = this;
    }

    private void Start()
    {
        // Explicitly trigger a clean, type-safe data flush on boot
        ResetAndInitializeSystemState();
    }

    public void RegisterCell(int miniBoard, int index, Cell cell)
    {
        string key = $"{miniBoard}_{index}";
        if (!globalCells.ContainsKey(key)) globalCells.Add(key, cell);
    }

    /// <summary>
    /// Flushes all type-safe backend matrices, arrays, tracking loops, and states.
    /// </summary>
    public void ResetAndInitializeSystemState()
    {
        Debug.Log("🧼 Performing a hard reset of all game states and visuals...");

        // 1. Reset backend tracking data (Keep your existing reset arrays)
        currentTurn = Player.X;
        activeMiniBoard = -1; 
        isGameOver = false;
        isAIThinking = false;

        // Initialize flattened board array and mini-board states
        for (int i = 0; i < 81; i++) globalBoardData[i] = CellState.Empty;
        for (int b = 0; b < 9; b++) miniBoardStates[b] = BoardState.Active;

        // 2. 🌟 FOOLPROOF VISUAL WIPE: Find every cell and clear it explicitly
        Cell[] allCells = FindObjectsByType<Cell>();
        foreach (Cell cell in allCells)
        {
                if (cell != null)
                {
                    // Use the cell's public reset method instead of accessing private fields
                    cell.ResetVisual();

                // Loop backwards through children so destroying them doesn't break the loop index
                for (int i = cell.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = cell.transform.GetChild(i);
                    
                    // CRITICAL SAFETY FILTER:
                    // Only destroy the object if it is NOT your cell background or highlight outline!
                    if (child.name.Contains("Piece") || child.name.Contains("Clone") || child.name.Contains("X") || child.name.Contains("O"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }

        // 3. 🚀 ALTERNATIVE DEEP CLEAN (Just in case pieces aren't children of the cells):
        // Find any stray piece clones that somehow spawned out in the open scene hierarchy
        GameObject[] strayPieces = GameObject.FindObjectsByType<GameObject>();
        foreach (GameObject go in strayPieces)
        {
            if (go.name.Contains("X_Piece") || go.name.Contains("O_Piece"))
            {
                Destroy(go);
            }
        }

        // 4. Force boundaries to redraw
        RefreshBoardHighlights();
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
        // Guard Gates
        if (isGameOver) return;
        
        // 🚨 CRITICAL GATEWAY FIX: Only block inputs on Player O's turn IF AI mode is enabled!
        if (currentTurn == Player.O && playAgainstAI) 
        {
            return; 
        }

        int bIdx = clickedCell.miniBoardIndex;
        int lIdx = clickedCell.localIndex;

        // Rule Validation Checks
        if (activeMiniBoard != -1 && activeMiniBoard != bIdx) return;
        if (miniBoardStates[bIdx] != BoardState.Active) return;
        if (GetCellState(bIdx, lIdx) != CellState.Empty) return;

        // Commit Move & Spawn Token Piece
        SetCellState(bIdx, lIdx, (currentTurn == Player.X) ? CellState.X : CellState.O);
        clickedCell.SpawnPiece(currentTurn, currentTurn == Player.X ? colorX : colorO);

        // Evaluate Rules State Win Matrices
        CheckMiniBoardWin(bIdx, currentTurn);
        UpdateNextPlayableBoardConstraints(lIdx);
        CheckMacroGameWin(currentTurn);

        if (isGameOver) return;

        // 🚀 STEP A: Swap Turn Enum State Variable
        currentTurn = (currentTurn == Player.X) ? Player.O : Player.X;

        // 🎯 STEP B: Force the Text Header Canvas String to Refresh immediately!
        if (UIManager.Instance != null)
        {
            bool isX = (currentTurn == Player.X);
            Color32 currentTurnColor = isX ? colorX : colorO;
            UIManager.Instance.UpdateTurnDisplay(isX, currentTurnColor);
        }

        RefreshBoardHighlights();

        // 🤖 STEP C: Run AI Coroutine Processing thread only if validation rules match
        if (currentTurn == Player.O && playAgainstAI)
        {
            if (!isAIThinking) StartCoroutine(TriggerComputerMoveRoutine());
        }
    }

    // Core move executor — used by both human clicks and AI forced moves
    private void ExecuteMove(Cell clickedCell)
    {
        if (isGameOver) return;

        int bIdx = clickedCell.miniBoardIndex;
        int lIdx = clickedCell.localIndex;

        if (activeMiniBoard != -1 && activeMiniBoard != bIdx) return;
        if (miniBoardStates[bIdx] != BoardState.Active) return;
        if (GetCellState(bIdx, lIdx) != CellState.Empty) return; 

        // Apply updated enum values directly to structural storage cells
        SetCellState(bIdx, lIdx, (currentTurn == Player.X) ? CellState.X : CellState.O);

        // Use the cell's SpawnPiece helper for consistent visuals
        clickedCell.SpawnPiece(currentTurn, currentTurn == Player.X ? colorX : colorO);

        // 1. Process board rules and state win conditions first
        CheckMiniBoardWin(bIdx, currentTurn);
        CheckMacroGameWin(currentTurn);

        if (!isGameOver)
        {
            // maintain target zone logic before rotating turn
            UpdateActiveTargetZone(lIdx);

            // 2. NOW alternate the active player state value
            currentTurn = (currentTurn == Player.X) ? Player.O : Player.X;

            // 3. 🌟 RE-POSITION THIS: Always update the screen UI immediately AFTER the turn changes!
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTurnDisplay(currentTurn == Player.X, currentTurn == Player.X ? colorX : colorO);
            }

            // 4. Update the outer active mini-board highlights
            RefreshBoardHighlights();

            // 5. Finally, pass the baton if playing against the computer
            if (!isGameOver && currentTurn == Player.O && playAgainstAI)
            {
                if (!isAIThinking) StartCoroutine(TriggerComputerMoveRoutine());
            }
        }
    }

    public void ResetUltimateMatch()
    {
        Debug.Log("🔄 Resetting match engine data...");

        // 1. Wipe out all physical token game objects on screen
        foreach (GameObject piece in spawnedPieces)
        {
            if (piece != null)
            {
                Destroy(piece);
            }
        }
        spawnedPieces.Clear(); // Empty our lookup list

        // 2. Clear state variables
        globalBoardData = new CellState[81];
        for (int i = 0; i < 81; i++) globalBoardData[i] = CellState.Empty;
        miniBoardStates = new BoardState[9];
        for (int b = 0; b < 9; b++) miniBoardStates[b] = BoardState.Active;
        activeMiniBoard = -1;
        currentTurn = Player.X;
        isGameOver = false;
        isAIThinking = false;

        // 3. Reset cell backplates 
        foreach (var kvp in globalCells)
        {
            kvp.Value.ResetVisual();
        }

        // 4. Update UI displays and outline frames
        RefreshBoardHighlights();
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnDisplay(true, colorX);
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

    // Flattened array helpers
    private int FlatIndex(int board, int index)
    {
        return board * 9 + index;
    }

    public CellState GetCellState(int board, int index)
    {
        return globalBoardData[FlatIndex(board, index)];
    }

    public void SetCellState(int board, int index, CellState value)
    {
        globalBoardData[FlatIndex(board, index)] = value;
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
            if (GetCellState(boardIndex, pattern[0]) == targetState &&
                GetCellState(boardIndex, pattern[1]) == targetState &&
                GetCellState(boardIndex, pattern[2]) == targetState)
            {
                miniBoardStates[boardIndex] = (player == Player.X) ? BoardState.WonByX : BoardState.WonByO;
                Debug.Log($"Sub-Grid {boardIndex} captured by {player}!");
                DimCompletedBoard(boardIndex, player == Player.X ? colorX : colorO);
                return;
            }
        }

        bool isFull = true;
        for (int i = 0; i < 9; i++)
        {
            if (GetCellState(boardIndex, i) == CellState.Empty) { isFull = false; break; }
        }
        if (isFull) miniBoardStates[boardIndex] = BoardState.Tied;
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
        BoardState targetWinState = (player == Player.X) ? BoardState.WonByX : BoardState.WonByO;

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
                
                // 🌟 FIX: Safely trigger the updated UI manager wrapper method
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.DisplayMatchWinner(winnerLabel, player == Player.X ? colorX : colorO);
                }
                
                activeMiniBoard = -2;
                RefreshBoardHighlights();
                return;
            }
        }
    }

    // AI move coroutine: picks a random valid move after a short delay
    private System.Collections.IEnumerator TriggerComputerMoveRoutine()
    {
        isAIThinking = true;
        yield return new WaitForSeconds(0.6f); // Premium tactical thinking delay!

        List<Cell> validMoves = new List<Cell>();

        // Scan for all valid available moves left on the field
        foreach (var kvp in globalCells)
        {
            int bIdx = kvp.Value.miniBoardIndex;
            int lIdx = kvp.Value.localIndex;

            if ((activeMiniBoard == -1 || activeMiniBoard == bIdx) &&
                miniBoardStates[bIdx] == BoardState.Active &&
                GetCellState(bIdx, lIdx) == CellState.Empty)
            {
                validMoves.Add(kvp.Value);
            }
        }

        // Execute random selection choice for initialization test confirmation
        if (validMoves.Count > 0 && !isGameOver)
        {
            Cell randomChoice = validMoves[Random.Range(0, validMoves.Count)];
            // Directly execute the move bypassing the human input guard
            ExecuteMove(randomChoice);
        }

        isAIThinking = false;
    }

    // Spawns a token prefab at the given world position, registers it for cleanup and animates it
    public GameObject SpawnToken(GameObject prefab, Vector3 position)
    {
        GameObject token = Instantiate(prefab, position, Quaternion.identity, transform);
        token.transform.localScale = Vector3.zero;
        spawnedPieces.Add(token);
        StartCoroutine(AnimatePieceSpawn(token.transform));
        return token;
    }

    // wrapper to keep your original method name available
    private void UpdateNextPlayableBoardConstraints(int targetedLocalIndex)
    {
        UpdateActiveTargetZone(targetedLocalIndex);
    }
}

