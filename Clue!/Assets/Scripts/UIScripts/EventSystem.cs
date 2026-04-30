using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemManager : MonoBehaviour
{
    // eventsystem bug, we have more than one running across scenes when they carry over but can't delete them cus we are testing the scence individually so this lil script will break it if the scenes are running together so we have 1
    private void Awake()
    {
        var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (systems.Length > 1)
        {
            Debug.Log("[EventSystemManager] Duplicate EventSystem blasted from orbit.");
            Destroy(gameObject);
        }
    }
}