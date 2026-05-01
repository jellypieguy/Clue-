// DEAD SCRIPT — UIManager.UpdateHandDisplay handles hand rendering now.
// Remove the PlayerHandDisplay component from any scene GameObjects, then delete this file.
using System.Collections.Generic;
using UnityEngine;
public class PlayerHandDisplay : MonoBehaviour
{
    public static PlayerHandDisplay Instance { get; private set; }
    void Awake() { if (Instance == null) Instance = this; else Destroy(gameObject); }
    public void ShowHand(List<CardData> cards) { } // intentional no-op
}
