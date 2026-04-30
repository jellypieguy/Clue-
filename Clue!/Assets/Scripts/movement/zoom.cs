using UnityEngine;
using UnityEngine.InputSystem;

// uh huh the class is lowercase I didn't make the scene file bindings lmao
[RequireComponent(typeof(Camera))]
public class zoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Limits")]
    [SerializeField] private float minZoomSize = 5f;
    [SerializeField] private float maxZoomSize = 25f;

    private Camera cam;
    private float targetZoom;
    private float currentVelocity = 0f;
    private bool isInitialized = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;
    }

    private void Update()
    {
        // camera fitters to do their thing before we lock in the zoom
        if (!isInitialized)
        {
            targetZoom = cam.orthographicSize;
            isInitialized = true;
        }

        HandleZoomInput();
        
        cam.orthographicSize = Mathf.SmoothDamp(
            cam.orthographicSize, 
            targetZoom, 
            ref currentVelocity, 
            smoothTime
        );
    }

    private void HandleZoomInput()
    {
        if (Mouse.current == null) return;

        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        if (scrollDelta != 0)
        {
            targetZoom -= (scrollDelta / 120f) * zoomSpeed * 10f;
            targetZoom = Mathf.Clamp(targetZoom, minZoomSize, maxZoomSize);
        }
    }
}