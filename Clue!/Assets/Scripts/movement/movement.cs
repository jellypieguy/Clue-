using UnityEngine;
using UnityEngine.InputSystem;

//  camera pan w/ keyboard and r_click and drag
public class movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private float dragSensitivity = 1.0f;

    [Header("Boundaries")]
    [SerializeField] private Vector2 minBounds = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 maxBounds = new Vector2(20f, 20f);

    private Camera _cam;
    private Vector3 _targetPosition;
    private Vector3 _currentVelocity = Vector3.zero;
    private bool _initialized = false;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void Start()
    {
        _targetPosition = transform.position;
    }

    private void Update()
    {
        // cam fit adjusts pos at its start
        if (!_initialized)
        {
            _targetPosition = transform.position;
            _initialized = true;
        }

        HandleInput();
        MoveCamera();
    }

    private void HandleInput()
    {
        HandleKeyboardInput();
        HandleMouseDragInput();

        _targetPosition.x = Mathf.Clamp(_targetPosition.x, minBounds.x, maxBounds.x);
        _targetPosition.y = Mathf.Clamp(_targetPosition.y, minBounds.y, maxBounds.y);
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;

        if (input != Vector2.zero)
            _targetPosition += new Vector3(input.x, input.y, 0).normalized * moveSpeed * Time.deltaTime;
    }

    private void HandleMouseDragInput()
    {
        if (Mouse.current == null || _cam == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta != Vector2.zero)
            {
                float screenFactor = _cam.orthographicSize * 2f / Screen.height;
                // nice drag feel
                _targetPosition -= new Vector3(mouseDelta.x, mouseDelta.y, 0) * screenFactor * dragSensitivity;
            }
        }
    }

    private void MoveCamera()
    {
        Vector3 destination = new Vector3(_targetPosition.x, _targetPosition.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, destination, ref _currentVelocity, smoothTime);
    }
}
