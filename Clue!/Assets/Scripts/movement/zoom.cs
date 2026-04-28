using UnityEngine;
using UnityEngine.InputSystem;

// Handles camera zoom m_scrollwheel.
[RequireComponent(typeof(Camera))]
public class zoom : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.5f;
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Limits")]
    [SerializeField] private float minZoomSize = 5f;
    [SerializeField] private float maxZoomSize = 25f;

    private Camera _cam;
    private float _targetZoom;
    private float _currentZoomVelocity = 0f;
    private bool _initialized = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        _targetZoom = _cam.orthographicSize;
    }

    private void Update()
    {
        // sync after the camera fit adjusts size start
        if (!_initialized)
        {
            _targetZoom = _cam.orthographicSize;
            _initialized = true;
        }

        HandleZoomInput();
        ApplyZoom();
    }

    private void HandleZoomInput()
    {
        if (Mouse.current == null) return;

        float scrollInput = Mouse.current.scroll.ReadValue().y;
        if (scrollInput != 0)
        {
            //normalize zoom
            _targetZoom -= (scrollInput / 120f) * zoomSpeed * 10f;
            _targetZoom = Mathf.Clamp(_targetZoom, minZoomSize, maxZoomSize);
        }
    }

    private void ApplyZoom()
    {
        _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, _targetZoom, ref _currentZoomVelocity, smoothTime);
    }
}
