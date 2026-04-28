using UnityEngine;

// PLACEHOLDER UIManager so the project compiles.
// this is a temporary stub.
// Currently just logs events to the Unity Console instead of showing them on screen.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Adds a message to the in-game event log.
    // Stubbed for now — logs to Console until the real UI panel exists.
    public void AddLogEvent(string message)
    {
        Debug.Log($"[GameLog] {message}");
    }
}