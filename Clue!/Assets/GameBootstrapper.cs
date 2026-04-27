using UnityEngine;
using ClueGame.Data;

/// The entry point for the GameBoard scene.
/// When the scene loads, this script:
///   1. Loads board.json from StreamingAssets/Data/
///   2. Builds the Board object from the JSON data
///   3. Tells BoardRenderer to draw the board on screen

public class GameBootstrapper : MonoBehaviour
{
    [Header("Drag the Board object here")]
    public BoardRenderer boardRenderer;

    void Start()
    {
        // Load the board from StreamingAssets/Data/board.json
        var (board, rawGrid, rooms) = BoardLoader.Load();

        // If loading failed, stop here — check Console for error message
        if (board == null)
        {
            Debug.LogError("[GameBootstrapper] Board failed to load. " +
                           "Check that board.json is in StreamingAssets/Data/");
            return;
        }

        // Draw the board
        boardRenderer.Render(board, rawGrid);

        Debug.Log("[GameBootstrapper] Board rendered successfully.");
    }
}
